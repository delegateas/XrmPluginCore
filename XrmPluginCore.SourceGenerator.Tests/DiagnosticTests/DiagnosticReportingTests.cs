using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using XrmPluginCore.SourceGenerator.Analyzers;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.DiagnosticTests;

/// <summary>
/// Tests for verifying diagnostic reporting from the source generator and analyzers.
/// </summary>
public class DiagnosticReportingTests
{
    [Fact]
    public void Should_Not_Report_XPC1000_Success_Diagnostic_On_Successful_Generation()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithPreImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert - XPC1000 is no longer reported to avoid spamming the user
        var successDiagnostics = result.GeneratorDiagnostics
            .Where(d => d.Id == "XPC1000")
            .ToArray();

        successDiagnostics.Should().BeEmpty("XPC1000 success diagnostic should not be reported to avoid spam");
    }

    [Fact]
    public async Task Should_Report_XPC4001_When_Plugin_Has_No_Parameterless_Constructor()
    {
		// Arrange - plugin class with only a parameterized constructor (no parameterless)
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
			        // Only has a constructor WITH parameters - no parameterless constructor
			        public TestPlugin(string config)
			        {
			            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation,
			                service => service.Process)
			                .AddImage(ImageType.PreImage, x => x.Name);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			        {
			            return services.AddScoped<ITestService, TestService>();
			        }
			    }

			    public interface ITestService { void Process(PreImage preImage); }
			    public class TestService : ITestService { public void Process(PreImage preImage) { } }
			}
			""";

        var source = TestFixtures.GetCompleteSource(pluginSource);

        // Act - Run analyzer instead of generator
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source, new NoParameterlessConstructorAnalyzer());

        // Assert - should report XPC4001
        var errorDiagnostics = diagnostics
            .Where(d => d.Id == "XPC4001")
            .ToArray();

        errorDiagnostics.Should().NotBeEmpty("XPC4001 should be reported when plugin class has no parameterless constructor");
        errorDiagnostics.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Should_Handle_XPC5000_Generation_Error_Gracefully()
    {
        // This test verifies that the generator doesn't crash on unexpected errors
        // We can't easily force an XPC5000 error, but we verify the compilation doesn't fail

        // Arrange - complex but valid source
        var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithBothImages());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert - should not have critical errors
        var criticalErrors = result.GeneratorDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        criticalErrors.Should().BeEmpty("generator should not produce critical errors for valid source");

        // Verify generation succeeded
        result.GeneratedTrees.Should().NotBeEmpty("code should be generated");
    }

    [Fact]
    public async Task Should_Report_XPC4002_When_Handler_Method_Not_Found()
    {
		// Arrange - method reference points to NonExistentMethod but service has Process
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
			                service => service.NonExistentMethod)
			                .WithPreImage(x => x.Name);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			        {
			            return services.AddScoped<ITestService, TestService>();
			        }
			    }

			    public interface ITestService
			    {
			        void Process();
			    }

			    public class TestService : ITestService
			    {
			        public void Process() { }
			    }
			}
			""";

        var source = TestFixtures.GetCompleteSource(pluginSource);

        // Act - Run analyzer instead of generator
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source, new HandlerMethodNotFoundAnalyzer());

        // Assert
        var errorDiagnostics = diagnostics
            .Where(d => d.Id == "XPC4002")
            .ToArray();

        errorDiagnostics.Should().NotBeEmpty("XPC4002 should be reported when handler method is not found");
        errorDiagnostics.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Should_Report_XPC4003_When_Handler_Missing_PreImage_Parameter()
    {
		// Arrange - WithPreImage is registered but handler takes no parameters
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
			                service => service.Process)
			                .WithPreImage(x => x.Name);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			        {
			            return services.AddScoped<ITestService, TestService>();
			        }
			    }

			    public interface ITestService
			    {
			        void Process();  // No PreImage parameter!
			    }

			    public class TestService : ITestService
			    {
			        public void Process() { }
			    }
			}
			""";

        var source = TestFixtures.GetCompleteSource(pluginSource);

        // Act - Run analyzer instead of generator
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source, new HandlerSignatureMismatchAnalyzer());

        // Assert
        var errorDiagnostics = diagnostics
            .Where(d => d.Id == "XPC4003")
            .ToArray();

        errorDiagnostics.Should().NotBeEmpty("XPC4003 should be reported when handler is missing PreImage parameter");
        errorDiagnostics.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Should_Report_XPC4003_When_Handler_Missing_PostImage_Parameter()
    {
		// Arrange - WithPostImage is registered but handler takes no parameters
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
			                service => service.Process)
			                .WithPostImage(x => x.Name);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			        {
			            return services.AddScoped<ITestService, TestService>();
			        }
			    }

			    public interface ITestService
			    {
			        void Process();  // No PostImage parameter!
			    }

			    public class TestService : ITestService
			    {
			        public void Process() { }
			    }
			}
			""";

        var source = TestFixtures.GetCompleteSource(pluginSource);

        // Act - Run analyzer instead of generator
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source, new HandlerSignatureMismatchAnalyzer());

        // Assert
        var errorDiagnostics = diagnostics
            .Where(d => d.Id == "XPC4003")
            .ToArray();

        errorDiagnostics.Should().NotBeEmpty("XPC4003 should be reported when handler is missing PostImage parameter");
        errorDiagnostics.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Should_Report_XPC4003_When_Handler_Missing_Both_Image_Parameters()
    {
		// Arrange - Both WithPreImage and WithPostImage but handler takes no parameters
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
			                service => service.Process)
			                .WithPreImage(x => x.Name)
			                .WithPostImage(x => x.AccountNumber);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			        {
			            return services.AddScoped<ITestService, TestService>();
			        }
			    }

			    public interface ITestService
			    {
			        void Process();  // No parameters!
			    }

			    public class TestService : ITestService
			    {
			        public void Process() { }
			    }
			}
			""";

        var source = TestFixtures.GetCompleteSource(pluginSource);

        // Act - Run analyzer instead of generator
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source, new HandlerSignatureMismatchAnalyzer());

        // Assert
        var errorDiagnostics = diagnostics
            .Where(d => d.Id == "XPC4003")
            .ToArray();

        errorDiagnostics.Should().NotBeEmpty("XPC4003 should be reported when handler is missing both image parameters");
        errorDiagnostics.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Should_Report_XPC4003_When_Handler_Has_Wrong_Parameter_Order()
    {
		// Arrange - WithPreImage and WithPostImage but handler has parameters in wrong order
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
			                service => service.Process)
			                .WithPreImage(x => x.Name)
			                .WithPostImage(x => x.AccountNumber);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			        {
			            return services.AddScoped<ITestService, TestService>();
			        }
			    }

			    public interface ITestService
			    {
			        void Process(PostImage post, PreImage pre);  // Wrong order! Should be PreImage, PostImage
			    }

			    public class TestService : ITestService
			    {
			        public void Process(PostImage post, PreImage pre) { }
			    }
			}
			""";

        var source = TestFixtures.GetCompleteSource(pluginSource);

        // Act - Run analyzer instead of generator
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source, new HandlerSignatureMismatchAnalyzer());

        // Assert
        var errorDiagnostics = diagnostics
            .Where(d => d.Id == "XPC4003")
            .ToArray();

        errorDiagnostics.Should().NotBeEmpty("XPC4003 should be reported when handler has wrong parameter order");
        errorDiagnostics.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Should_Report_XPC4004_When_WithPreImage_Used_With_Invocation_Syntax()
    {
		// Arrange - WithPreImage used with s => s.DoSomething() (invocation) instead of s => s.DoSomething (method reference)
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
			                s => s.DoSomething())  // Invocation syntax - NOT method reference
			                .WithPreImage(x => x.Name);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			        {
			            return services.AddScoped<ITestService, TestService>();
			        }
			    }

			    public interface ITestService
			    {
			        void DoSomething();
			    }

			    public class TestService : ITestService
			    {
			        public void DoSomething() { }
			    }
			}
			""";

        var source = TestFixtures.GetCompleteSource(pluginSource);

        // Act - Run analyzer instead of generator
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source, new ImageWithoutMethodReferenceAnalyzer());

        // Assert
        var warningDiagnostics = diagnostics
            .Where(d => d.Id == "XPC4004")
            .ToArray();

        warningDiagnostics.Should().NotBeEmpty("XPC4004 should be reported when WithPreImage is used with invocation syntax");
        warningDiagnostics.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task Should_Report_XPC4004_When_WithPostImage_Used_With_Invocation_Syntax()
    {
		// Arrange - WithPostImage used with s => s.DoSomething() (invocation) instead of s => s.DoSomething (method reference)
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
			                s => s.DoSomething())  // Invocation syntax - NOT method reference
			                .WithPostImage(x => x.Name);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			        {
			            return services.AddScoped<ITestService, TestService>();
			        }
			    }

			    public interface ITestService
			    {
			        void DoSomething();
			    }

			    public class TestService : ITestService
			    {
			        public void DoSomething() { }
			    }
			}
			""";

        var source = TestFixtures.GetCompleteSource(pluginSource);

        // Act - Run analyzer instead of generator
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source, new ImageWithoutMethodReferenceAnalyzer());

        // Assert
        var warningDiagnostics = diagnostics
            .Where(d => d.Id == "XPC4004")
            .ToArray();

        warningDiagnostics.Should().NotBeEmpty("XPC4004 should be reported when WithPostImage is used with invocation syntax");
        warningDiagnostics.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task Should_Not_Report_XPC4004_When_Using_Method_Reference_Syntax()
    {
		// Arrange - Method reference syntax (correct usage)
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
			                s => s.HandleUpdate)  // Method reference - correct syntax
			                .WithPreImage(x => x.Name);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			        {
			            return services.AddScoped<ITestService, TestService>();
			        }
			    }

			    public interface ITestService
			    {
			        void HandleUpdate(PreImage preImage);
			    }

			    public class TestService : ITestService
			    {
			        public void HandleUpdate(PreImage preImage) { }
			    }
			}
			""";

        var source = TestFixtures.GetCompleteSource(pluginSource);

        // Act - Run analyzer instead of generator
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source, new ImageWithoutMethodReferenceAnalyzer());

        // Assert
        var warningDiagnostics = diagnostics
            .Where(d => d.Id == "XPC4004")
            .ToArray();

        warningDiagnostics.Should().BeEmpty("XPC4004 should NOT be reported when using method reference syntax");
    }

    [Fact]
    public async Task Should_Not_Report_XPC4004_When_Old_Api_Used_Without_Images()
    {
		// Arrange - Invocation syntax but without WithPreImage/WithPostImage (no images registered)
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
			                s => s.DoSomething())  // Invocation syntax - but no images
			                .AddFilteredAttributes(x => x.Name);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			        {
			            return services.AddScoped<ITestService, TestService>();
			        }
			    }

			    public interface ITestService
			    {
			        void DoSomething();
			    }

			    public class TestService : ITestService
			    {
			        public void DoSomething() { }
			    }
			}
			""";

        var source = TestFixtures.GetCompleteSource(pluginSource);

        // Act - Run analyzer instead of generator
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source, new ImageWithoutMethodReferenceAnalyzer());

        // Assert
        var warningDiagnostics = diagnostics
            .Where(d => d.Id == "XPC4004")
            .ToArray();

        warningDiagnostics.Should().BeEmpty("XPC4004 should NOT be reported when old API is used without images");
    }

    private static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(string source, DiagnosticAnalyzer analyzer)
    {
        var compilation = CompilationHelper.CreateCompilation(source);

        var compilationWithAnalyzers = compilation.WithAnalyzers(
			[analyzer]);

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}
