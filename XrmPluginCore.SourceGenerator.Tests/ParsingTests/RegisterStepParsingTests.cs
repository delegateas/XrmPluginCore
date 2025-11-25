using FluentAssertions;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.ParsingTests;

/// <summary>
/// Tests for parsing RegisterStep invocations and extracting metadata.
/// </summary>
public class RegisterStepParsingTests
{
    [Fact]
    public void Should_Parse_WithPreImage_Registration()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithPreImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();
        generatedSource.Should().Contain("class PreImage");
        generatedSource.Should().Contain("public string Name");
        generatedSource.Should().Contain("public Microsoft.Xrm.Sdk.Money Revenue");
    }

    [Fact]
    public void Should_Parse_WithPostImage_Registration()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithPostImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();
        generatedSource.Should().Contain("class PostImage");
        generatedSource.Should().Contain("public string Name");
        generatedSource.Should().Contain("public string AccountNumber");
    }

    [Fact]
    public void Should_Parse_Both_PreImage_And_PostImage()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithBothImages());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        generatedSource.Should().Contain("class PreImage");
        generatedSource.Should().Contain("class PostImage");

        // PreImage should have Name and Revenue
        generatedSource.Should().Match("*PreImage*Name*");
        generatedSource.Should().Match("*PreImage*Revenue*");

        // PostImage should have Name and AccountNumber
        generatedSource.Should().Match("*PostImage*Name*");
        generatedSource.Should().Match("*PostImage*AccountNumber*");
    }

    [Fact]
    public void Should_Parse_Old_AddImage_Api_For_Backward_Compatibility()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithOldImageApi());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();
        generatedSource.Should().Contain("class PreImage");
        generatedSource.Should().Contain("public string Name");
        generatedSource.Should().Contain("public Microsoft.Xrm.Sdk.Money Revenue");
    }

    [Fact]
    public void Should_Parse_Lambda_Syntax_For_Attributes()
    {
        // Arrange - GetPluginWithPreImage uses lambda syntax: x => x.Name
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithPreImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();
        generatedSource.Should().Contain("public string Name");
    }

    [Fact]
    public void Should_Handle_Contact_Entity()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.ContactEntity,
            TestFixtures.GetPluginWithPreImage("Contact"));

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();
        generatedSource.Should().Contain("class PreImage");
        generatedSource.Should().Contain("public string FirstName");
        generatedSource.Should().Contain("public string EmailAddress");
    }

    [Fact]
    public void Should_Generate_Correct_Namespace_For_Registration()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithPreImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Namespace pattern: {Namespace}.PluginImages.{PluginClassName}.{Entity}{Operation}{Stage}
        generatedSource.Should().Contain("namespace TestNamespace.PluginImages.TestPlugin.AccountUpdatePostOperation");
    }

    [Fact]
    public void Should_Handle_Multiple_Attributes_In_Same_Image()
    {
        // Arrange
        var pluginSource = @"
using XrmPluginCore;
using XrmPluginCore.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using TestNamespace;

namespace TestNamespace
{
    public class TestPlugin : Plugin
    {
        public TestPlugin()
        {
            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation)
                .WithPreImage(x => x.Name, x => x.AccountNumber, x => x.Revenue, x => x.IndustryCode)
                .Execute<PreImage>((service, preImage) => service.Process(preImage));
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<ITestService, TestService>();
        }
    }

    public interface ITestService
    {
        void Process(object image);
    }

    public class TestService : ITestService
    {
        public void Process(object image) { }
    }
}";

        var source = TestFixtures.GetCompleteSource(TestFixtures.AccountEntity, pluginSource);

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        generatedSource.Should().Contain("public string Name");
        generatedSource.Should().Contain("public string AccountNumber");
        generatedSource.Should().Contain("public Microsoft.Xrm.Sdk.Money Revenue");
        generatedSource.Should().Contain("public Microsoft.Xrm.Sdk.OptionSetValue IndustryCode");
    }
}
