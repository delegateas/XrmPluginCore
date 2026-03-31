using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using XrmPluginCore.SourceGenerator.Analyzers;
using XrmPluginCore.SourceGenerator.CodeFixes;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.DiagnosticTests;

/// <summary>
/// Tests for LocalPluginContextAsServiceAnalyzer that errors when LocalPluginContext is used as TService in RegisterStep.
/// </summary>
public class LocalPluginContextAsServiceAnalyzerTests : CodeFixTestBase
{
	[Fact]
	public async Task Should_Report_XPC3004_When_LocalPluginContext_Explicitly_Specified()
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
            RegisterStep<Contact, LocalPluginContext>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute);
        }

        private void Execute(LocalPluginContext context) { }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
            => services;
    }
}
""";

		var source = TestFixtures.GetCompleteSource(pluginSource);
		var diagnostics = await GetDiagnosticsAsync(source);

		// Assert
		diagnostics.Should().ContainSingle(d => d.Id == "XPC3004");
		var diagnostic = diagnostics.Single(d => d.Id == "XPC3004");
		diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
		diagnostic.GetMessage().Should().Contain("Contact");
		diagnostic.GetMessage().Should().Contain("LocalPluginContext");
	}

	[Fact]
	public async Task Should_Report_XPC3004_When_LocalPluginContext_Used_As_TService_With_Lambda()
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
            RegisterStep<Contact, LocalPluginContext>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ctx => ctx.TracingService.Trace("hello"));
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
            => services;
    }
}
""";

		var source = TestFixtures.GetCompleteSource(pluginSource);
		var diagnostics = await GetDiagnosticsAsync(source);

		// Assert
		diagnostics.Should().ContainSingle(d => d.Id == "XPC3004");
	}

	[Fact]
	public async Task Should_Not_Report_XPC3004_When_DI_Service_Used()
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
            RegisterStep<Contact, ITestService>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                nameof(ITestService.HandleUpdate));
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
            => services.AddScoped<ITestService, TestService>();
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
		var diagnostics = await GetDiagnosticsAsync(source);

		// Assert
		diagnostics.Should().NotContain(d => d.Id == "XPC3004");
	}

	[Fact]
	public async Task Should_Not_Report_XPC3004_For_SingleTypeParam_Overload()
	{
		// Arrange — RegisterStep<T> with a single type arg uses Action<IExtendedServiceProvider>
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
            RegisterStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                sp => sp.GetRequiredService<object>());
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
            => services;
    }
}
""";

		var source = TestFixtures.GetCompleteSource(pluginSource);
		var diagnostics = await GetDiagnosticsAsync(source);

		// Assert
		diagnostics.Should().NotContain(d => d.Id == "XPC3004");
	}

	[Fact]
	public async Task CodeFix_Should_Rewrite_To_RegisterPluginStep()
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
            RegisterStep<Contact, LocalPluginContext>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute);
        }

        private void Execute(LocalPluginContext context) { }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
            => services;
    }
}
""";

		var source = TestFixtures.GetCompleteSource(pluginSource);

		// Act
		var fixedSource = await ApplyCodeFixAsync(
			source,
			new LocalPluginContextAsServiceAnalyzer(),
			new LocalPluginContextAsServiceCodeFixProvider(),
			DiagnosticDescriptors.LocalPluginContextAsService.Id);

		// Assert
		fixedSource.Should().Contain("RegisterPluginStep<Contact>");
		fixedSource.Should().NotContain("RegisterStep<Contact, LocalPluginContext>");
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
            RegisterStep<Contact, LocalPluginContext>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute);
        }

        private void Execute(LocalPluginContext context) { }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
            => services;
    }
}
""";

		var source = TestFixtures.GetCompleteSource(pluginSource);

		// Act
		var codeActions = await GetCodeActionsAsync(
			source,
			new LocalPluginContextAsServiceAnalyzer(),
			new LocalPluginContextAsServiceCodeFixProvider(),
			DiagnosticDescriptors.LocalPluginContextAsService.Id);

		// Assert
		codeActions.Should().ContainSingle();
		codeActions[0].Title.Should().Be("Use RegisterPluginStep<Contact> instead");
	}

	private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
	{
		var compilation = CompilationHelper.CreateCompilation(source);
		var analyzer = new LocalPluginContextAsServiceAnalyzer();

		var compilationWithAnalyzers = compilation.WithAnalyzers(
			[analyzer]);

		return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
	}
}
