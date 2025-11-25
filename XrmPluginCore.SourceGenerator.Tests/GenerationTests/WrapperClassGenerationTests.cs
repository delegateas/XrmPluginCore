using FluentAssertions;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.GenerationTests;

/// <summary>
/// Tests for verifying wrapper class code generation structure and content.
/// </summary>
public class WrapperClassGenerationTests
{
    [Fact]
    public void Should_Generate_PreImage_Class_With_Properties()
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

        // Verify class structure
        generatedSource.Should().Contain("public class PreImage");
        generatedSource.Should().Contain("private readonly Entity entity;");
        generatedSource.Should().Contain("public PreImage(Entity entity)");

        // Verify properties
        generatedSource.Should().Contain("public string Name");
        generatedSource.Should().Contain("entity.GetAttributeValue<string>(\"name\")");

        generatedSource.Should().Contain("public Microsoft.Xrm.Sdk.Money Revenue");
        generatedSource.Should().Contain("entity.GetAttributeValue<Microsoft.Xrm.Sdk.Money>(\"revenue\")");
    }

    [Fact]
    public void Should_Generate_PostImage_Class_With_Properties()
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

        // Verify class structure
        generatedSource.Should().Contain("public class PostImage");
        generatedSource.Should().Contain("private readonly Entity entity;");
        generatedSource.Should().Contain("public PostImage(Entity entity)");

        // Verify properties
        generatedSource.Should().Contain("public string Name");
        generatedSource.Should().Contain("public string AccountNumber");
    }

    [Fact]
    public void Should_Generate_Both_Image_Classes_In_Same_Namespace()
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

        // Both classes should be in the same namespace
        var namespaceCount = System.Text.RegularExpressions.Regex.Matches(
            generatedSource,
            @"namespace\s+TestNamespace\.PluginImages\.TestPlugin\.AccountUpdatePostOperation").Count;

        namespaceCount.Should().Be(1, "both classes should be in the same namespace");

        // Both classes should exist
        generatedSource.Should().Contain("public class PreImage");
        generatedSource.Should().Contain("public class PostImage");
    }

    [Fact]
    public void Should_Generate_Properties_With_Correct_Types()
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
                .WithPreImage(x => x.Name, x => x.Revenue, x => x.IndustryCode, x => x.PrimaryContactId)
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

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Verify different types
        generatedSource.Should().Contain("public string Name");
        generatedSource.Should().Contain("entity.GetAttributeValue<string>");

        generatedSource.Should().Contain("public Microsoft.Xrm.Sdk.Money Revenue");
        generatedSource.Should().Contain("entity.GetAttributeValue<Microsoft.Xrm.Sdk.Money>");

        generatedSource.Should().Contain("public Microsoft.Xrm.Sdk.OptionSetValue IndustryCode");
        generatedSource.Should().Contain("entity.GetAttributeValue<Microsoft.Xrm.Sdk.OptionSetValue>");

        generatedSource.Should().Contain("public Microsoft.Xrm.Sdk.EntityReference PrimaryContactId");
        generatedSource.Should().Contain("entity.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>");
    }

    [Fact]
    public void Should_Include_ToEntity_Method()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithPreImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        generatedSource.Should().Contain("public T ToEntity<T>() where T : Entity");
        generatedSource.Should().Contain("=> entity.ToEntity<T>();");
    }

    [Fact]
    public void Should_Include_GetUnderlyingEntity_Method()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithPreImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        generatedSource.Should().Contain("public Entity GetUnderlyingEntity()");
        generatedSource.Should().Contain("=> entity;");
    }

    [Fact]
    public void Should_Implement_IEntityImageWrapper_Interface()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithPreImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        generatedSource.Should().Contain(": IEntityImageWrapper");
    }
}
