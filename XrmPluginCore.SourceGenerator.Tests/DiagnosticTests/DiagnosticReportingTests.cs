using FluentAssertions;
using Microsoft.CodeAnalysis;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.DiagnosticTests;

/// <summary>
/// Tests for verifying diagnostic reporting from the source generator.
/// </summary>
public class DiagnosticReportingTests
{
    [Fact]
    public void Should_Report_XPC1000_Success_Diagnostic_On_Successful_Generation()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithPreImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        var successDiagnostics = result.GeneratorDiagnostics
            .Where(d => d.Id == "XPC1000")
            .ToArray();

        successDiagnostics.Should().NotBeEmpty("XPC1000 should be reported on successful generation");
        successDiagnostics.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Info);
    }

    [Fact]
    public void Should_Report_XPC4001_When_Plugin_Has_No_Parameterless_Constructor()
    {
        // Arrange - plugin class with only a parameterized constructor (no parameterless)
        var pluginSource = @"
using XrmPluginCore;
using XrmPluginCore.Enums;
using Microsoft.Extensions.DependencyInjection;
using TestNamespace;

namespace TestNamespace
{
    public class TestPlugin : Plugin
    {
        // Only has a constructor WITH parameters - no parameterless constructor
        public TestPlugin(string config)
        {
            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation)
                .WithPreImage(x => x.Name)
                .Execute<PreImage>((service, preImage) => service.Process(preImage));
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<ITestService, TestService>();
        }
    }

    public interface ITestService { void Process(object image); }
    public class TestService : ITestService { public void Process(object image) { } }
}";

        var source = TestFixtures.GetCompleteSource(TestFixtures.AccountEntity, pluginSource);

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert - should report XPC4001 (was XPC4002, now renamed)
        var errorDiagnostics = result.GeneratorDiagnostics
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
            TestFixtures.AccountEntity,
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
}
