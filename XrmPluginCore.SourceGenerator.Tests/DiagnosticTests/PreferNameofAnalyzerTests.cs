using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using XrmPluginCore.SourceGenerator.Analyzers;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.DiagnosticTests;

/// <summary>
/// Tests for PreferNameofAnalyzer that warns when string literals are used for handler methods.
/// </summary>
public class PreferNameofAnalyzerTests
{
	[Fact]
	public async Task Should_Report_XPC3001_When_String_Literal_Used_For_Handler_Method()
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
		var diagnostics = await GetDiagnosticsAsync(source);

		// Assert
		diagnostics.Should().ContainSingle(d => d.Id == "XPC3001");
		var diagnostic = diagnostics.Single(d => d.Id == "XPC3001");
		diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
		diagnostic.GetMessage().Should().Contain("nameof(ITestService.HandleUpdate)");
		diagnostic.GetMessage().Should().Contain("\"HandleUpdate\"");

		diagnostic.Properties.Should().ContainKey("ServiceType");
		diagnostic.Properties.Should().ContainKey("MethodName");
		diagnostic.Properties["ServiceType"].Should().Be("ITestService");
		diagnostic.Properties["MethodName"].Should().Be("HandleUpdate");
	}

	[Fact]
	public async Task Should_Not_Report_XPC3001_When_Nameof_Used_For_Handler_Method()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithoutImages());

		var diagnostics = await GetDiagnosticsAsync(source);

		// Assert
		diagnostics.Should().NotContain(d => d.Id == "XPC3001");
	}

	[Fact]
	public async Task Should_Not_Report_XPC3001_When_Lambda_Used_For_Handler_Method()
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
			                s => s.HandleUpdate)
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
		var diagnostics = await GetDiagnosticsAsync(source);

		// Assert
		diagnostics.Should().NotContain(d => d.Id == "XPC3001");
	}

	[Fact]
	public async Task Should_Report_XPC3001_For_String_Literal_With_Images()
	{
		// Arrange
		const string pluginSource = """

using XrmPluginCore;
using XrmPluginCore.Enums;
using Microsoft.Extensions.DependencyInjection;
using TestNamespace;
using TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;

namespace TestNamespace
{
    public class TestPlugin : Plugin
    {
        public TestPlugin()
        {
            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation,
                "Process")
                .AddFilteredAttributes(x => x.Name)
                .WithPreImage(x => x.Name, x => x.Revenue);
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<ITestService, TestService>();
        }
    }

    public interface ITestService
    {
        void Process(PreImage preImage);
    }

    public class TestService : ITestService
    {
        public void Process(PreImage preImage) { }
    }
}
""";

		var source = TestFixtures.GetCompleteSource(pluginSource);
		var diagnostics = await GetDiagnosticsAsync(source);

		// Assert
		diagnostics.Should().ContainSingle(d => d.Id == "XPC3001");
	}

	[Fact]
	public async Task Should_Not_Report_For_Non_RegisterStep_Methods()
	{
		// Arrange - Source with a generic method call that is not RegisterStep
		const string source = """

using System;

namespace TestNamespace
{
    public class SomeClass
    {
        public void DoSomething()
        {
            SomeMethod<string, int>("value");
        }

        public void SomeMethod<T1, T2>(string arg) { }
    }
}
""";

		var diagnostics = await GetDiagnosticsAsync(source);

		// Assert
		diagnostics.Should().NotContain(d => d.Id == "XPC3001");
	}

	private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
	{
		var compilation = CompilationHelper.CreateCompilation(source);
		var analyzer = new PreferNameofAnalyzer();

		var compilationWithAnalyzers = compilation.WithAnalyzers(
			[analyzer]);

		return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
	}
}
