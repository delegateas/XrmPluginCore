using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using XrmPluginCore.SourceGenerator.Analyzers;
using XrmPluginCore.SourceGenerator.CodeFixes;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.CodeFixTests;

/// <summary>
/// Tests for PreferNameofCodeFixProvider that converts string literals to nameof() expressions.
/// </summary>
public class PreferNameofCodeFixProviderTests
{
	[Fact]
	public async Task Should_Convert_String_Literal_To_Nameof_With_Service_Type()
	{
		// Arrange
		var pluginSource = @"
using XrmPluginCore;
using XrmPluginCore.Enums;
using Microsoft.Extensions.DependencyInjection;
using TestNamespace;

namespace TestNamespace
{
    public class TestPlugin : Plugin
    {
        public TestPlugin()
        {
            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation,
                ""HandleUpdate"")
                .AddFilteredAttributes(x => x.Name);
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<ITestService, TestService>();
        }
    }

    public interface ITestService
    {
        void HandleUpdate();
    }

    public class TestService : ITestService
    {
        public void HandleUpdate() { }
    }
}";

		var source = TestFixtures.GetCompleteSource(TestFixtures.AccountEntity, pluginSource);

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert
		fixedSource.Should().Contain("nameof(ITestService.HandleUpdate)");
		fixedSource.Should().NotContain("\"HandleUpdate\"");
	}

	[Fact]
	public async Task Should_Preserve_Surrounding_Code_Structure()
	{
		// Arrange
		var pluginSource = @"
using XrmPluginCore;
using XrmPluginCore.Enums;
using Microsoft.Extensions.DependencyInjection;
using TestNamespace;

namespace TestNamespace
{
    public class TestPlugin : Plugin
    {
        public TestPlugin()
        {
            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation,
                ""HandleUpdate"")
                .AddFilteredAttributes(x => x.Name);
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<ITestService, TestService>();
        }
    }

    public interface ITestService
    {
        void HandleUpdate();
    }

    public class TestService : ITestService
    {
        public void HandleUpdate() { }
    }
}";

		var source = TestFixtures.GetCompleteSource(TestFixtures.AccountEntity, pluginSource);

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert - Verify structure is preserved
		fixedSource.Should().Contain("RegisterStep<Account, ITestService>");
		fixedSource.Should().Contain("EventOperation.Update");
		fixedSource.Should().Contain("ExecutionStage.PostOperation");
		fixedSource.Should().Contain(".AddFilteredAttributes");
	}

	[Fact]
	public async Task Should_Fix_Multiple_String_Literals_When_FixAll_Applied()
	{
		// Arrange - Two plugins with string literals
		var pluginSource = @"
using XrmPluginCore;
using XrmPluginCore.Enums;
using Microsoft.Extensions.DependencyInjection;
using TestNamespace;

namespace TestNamespace
{
    public class TestPlugin1 : Plugin
    {
        public TestPlugin1()
        {
            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation,
                ""HandleUpdate"")
                .AddFilteredAttributes(x => x.Name);
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<ITestService, TestService>();
        }
    }

    public class TestPlugin2 : Plugin
    {
        public TestPlugin2()
        {
            RegisterStep<Account, ITestService>(EventOperation.Create, ExecutionStage.PreOperation,
                ""HandleCreate"")
                .AddFilteredAttributes(x => x.Name);
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<ITestService, TestService>();
        }
    }

    public interface ITestService
    {
        void HandleUpdate();
        void HandleCreate();
    }

    public class TestService : ITestService
    {
        public void HandleUpdate() { }
        public void HandleCreate() { }
    }
}";

		var source = TestFixtures.GetCompleteSource(TestFixtures.AccountEntity, pluginSource);

		// Act - Apply all fixes
		var fixedSource = await ApplyAllCodeFixesAsync(source);

		// Assert
		fixedSource.Should().Contain("nameof(ITestService.HandleUpdate)");
		fixedSource.Should().Contain("nameof(ITestService.HandleCreate)");
		fixedSource.Should().NotContain("\"HandleUpdate\"");
		fixedSource.Should().NotContain("\"HandleCreate\"");
	}

	[Fact]
	public async Task CodeFix_Should_Have_Correct_Title()
	{
		// Arrange
		var pluginSource = @"
using XrmPluginCore;
using XrmPluginCore.Enums;
using Microsoft.Extensions.DependencyInjection;
using TestNamespace;

namespace TestNamespace
{
    public class TestPlugin : Plugin
    {
        public TestPlugin()
        {
            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation,
                ""HandleUpdate"")
                .AddFilteredAttributes(x => x.Name);
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<ITestService, TestService>();
        }
    }

    public interface ITestService
    {
        void HandleUpdate();
    }

    public class TestService : ITestService
    {
        public void HandleUpdate() { }
    }
}";

		var source = TestFixtures.GetCompleteSource(TestFixtures.AccountEntity, pluginSource);

		// Act
		var codeActions = await GetCodeActionsAsync(source);

		// Assert
		codeActions.Should().ContainSingle();
		codeActions[0].Title.Should().Be("Use nameof(ITestService.HandleUpdate)");
	}

	private static async Task<string> ApplyCodeFixAsync(string source)
	{
		var compilation = CompilationHelper.CreateCompilation(source);
		var analyzer = new PreferNameofAnalyzer();
		var codeFixProvider = new PreferNameofCodeFixProvider();

		var compilationWithAnalyzers = compilation.WithAnalyzers(
			ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

		var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
		var diagnostic = diagnostics.FirstOrDefault(d => d.Id == "XPC3001");

		if (diagnostic == null)
		{
			return source;
		}

		var document = CreateDocument(source);
		var actions = new List<CodeAction>();

		var context = new CodeFixContext(
			document,
			diagnostic,
			(action, _) => actions.Add(action),
			CancellationToken.None);

		await codeFixProvider.RegisterCodeFixesAsync(context);

		if (actions.Count == 0)
		{
			return source;
		}

		var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
		var changedSolution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
		var changedDocument = changedSolution.GetDocument(document.Id);
		var newText = await changedDocument!.GetTextAsync();

		return newText.ToString();
	}

	private static async Task<string> ApplyAllCodeFixesAsync(string source)
	{
		var currentSource = source;
		var previousSource = string.Empty;

		// Keep applying fixes until no more changes
		while (currentSource != previousSource)
		{
			previousSource = currentSource;
			currentSource = await ApplyCodeFixAsync(currentSource);
		}

		return currentSource;
	}

	private static async Task<List<CodeAction>> GetCodeActionsAsync(string source)
	{
		var compilation = CompilationHelper.CreateCompilation(source);
		var analyzer = new PreferNameofAnalyzer();
		var codeFixProvider = new PreferNameofCodeFixProvider();

		var compilationWithAnalyzers = compilation.WithAnalyzers(
			ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

		var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
		var diagnostic = diagnostics.FirstOrDefault(d => d.Id == "XPC3001");

		if (diagnostic == null)
		{
			return new List<CodeAction>();
		}

		var document = CreateDocument(source);
		var actions = new List<CodeAction>();

		var context = new CodeFixContext(
			document,
			diagnostic,
			(action, _) => actions.Add(action),
			CancellationToken.None);

		await codeFixProvider.RegisterCodeFixesAsync(context);

		return actions;
	}

	private static Document CreateDocument(string source)
	{
		var projectId = ProjectId.CreateNewId();
		var documentId = DocumentId.CreateNewId(projectId);

		var references = new[]
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Plugin).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(XrmPluginCore.Enums.EventOperation).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Microsoft.Xrm.Sdk.Entity).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions).Assembly.Location),
			MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
			MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
		};

		var solution = new AdhocWorkspace().CurrentSolution
			.AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
			.WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
			.WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.CSharp11))
			.AddMetadataReferences(projectId, references)
			.AddDocument(documentId, "Test.cs", source);

		return solution.GetDocument(documentId)!;
	}
}
