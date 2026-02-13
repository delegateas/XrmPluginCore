using FluentAssertions;
using Microsoft.CodeAnalysis.CodeActions;
using XrmPluginCore.SourceGenerator.Analyzers;
using XrmPluginCore.SourceGenerator.CodeFixes;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.DiagnosticTests;

/// <summary>
/// Tests for PreferNameofCodeFixProvider that converts string literals to nameof() expressions.
/// </summary>
public class PreferNameofCodeFixProviderTests : CodeFixTestBase
{
	[Fact]
	public async Task Should_Convert_String_Literal_To_Nameof_With_Service_Type()
	{
		// Arrange
		const string pluginSource = """
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
                "HandleUpdate")
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
}
""";

		var source = TestFixtures.GetCompleteSource(pluginSource);

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert
		fixedSource.Should().Contain("nameof(ITestService.HandleUpdate)");
		fixedSource.Should().NotContain("\"HandleUpdate\"");

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
		const string pluginSource = """
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
                "HandleUpdate")
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
                "HandleCreate")
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
}
""";

		var source = TestFixtures.GetCompleteSource(pluginSource);

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
		const string pluginSource = """
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
                "HandleUpdate")
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
}
""";

		var source = TestFixtures.GetCompleteSource(pluginSource);

		// Act
		var codeActions = await GetCodeActionsAsync(source);

		// Assert
		codeActions.Should().ContainSingle();
		codeActions[0].Title.Should().Be("Use nameof(ITestService.HandleUpdate)");
	}

	private static Task<string> ApplyCodeFixAsync(string source)
		=> ApplyCodeFixAsync(source, new PreferNameofAnalyzer(), new PreferNameofCodeFixProvider(),
			DiagnosticDescriptors.PreferNameofOverStringLiteral.Id);

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

	private static Task<List<CodeAction>> GetCodeActionsAsync(string source)
		=> GetCodeActionsAsync(source, new PreferNameofAnalyzer(), new PreferNameofCodeFixProvider(),
			DiagnosticDescriptors.PreferNameofOverStringLiteral.Id);
}
