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

        // All classes should be in the same namespace
        var namespaceCount = System.Text.RegularExpressions.Regex.Matches(
            generatedSource,
            @"namespace\s+TestNamespace\.PluginRegistrations\.TestPlugin\.AccountUpdatePostOperation").Count;

        namespaceCount.Should().Be(1, "all classes should be in the same namespace");

        // All classes should exist
        generatedSource.Should().Contain("public class PreImage");
        generatedSource.Should().Contain("public class PostImage");
        generatedSource.Should().Contain("internal sealed class ActionWrapper : IActionWrapper");
    }

    [Fact]
    public void Should_Generate_Properties_With_Correct_Types()
    {
        // Arrange
        var pluginSource = @"
using XrmPluginCore;
using XrmPluginCore.Abstractions;
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
                .AddImage(ImageType.PreImage, x => x.Name, x => x.Revenue, x => x.IndustryCode, x => x.PrimaryContactId);
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<ITestService, TestService>();
        }
    }

    public interface ITestService { void Process(PreImage preImage); }
    public class TestService : ITestService { public void Process(PreImage preImage) { } }
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

    [Fact]
    public void Should_Generate_ActionWrapper_Class_For_New_Api()
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

        // Verify ActionWrapper class structure (now implements IActionWrapper interface)
        generatedSource.Should().Contain("internal sealed class ActionWrapper : IActionWrapper");
        generatedSource.Should().Contain("public Action<IExtendedServiceProvider> CreateAction()");
    }

    [Fact]
    public void Should_Generate_ActionWrapper_With_PreImage_Call()
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

        // Verify PreImage handling (now inline instead of using PluginImageHelper)
        generatedSource.Should().Contain("var preImageEntity = context?.PreEntityImages?.Values?.FirstOrDefault();");
        generatedSource.Should().Contain("var preImage = preImageEntity != null ? new PreImage(preImageEntity) : null;");
        generatedSource.Should().Contain("service.Process(preImage)");
    }

    [Fact]
    public void Should_Generate_ActionWrapper_With_PostImage_Call()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithPostImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Verify PostImage handling (now inline instead of using PluginImageHelper)
        generatedSource.Should().Contain("var postImageEntity = context?.PostEntityImages?.Values?.FirstOrDefault();");
        generatedSource.Should().Contain("var postImage = postImageEntity != null ? new PostImage(postImageEntity) : null;");
        generatedSource.Should().Contain("service.Process(postImage)");
    }

    [Fact]
    public void Should_Generate_ActionWrapper_With_Both_Images()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithBothImages());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Verify both images are handled (now inline instead of using PluginImageHelper)
        generatedSource.Should().Contain("var preImageEntity = context?.PreEntityImages?.Values?.FirstOrDefault();");
        generatedSource.Should().Contain("var preImage = preImageEntity != null ? new PreImage(preImageEntity) : null;");
        generatedSource.Should().Contain("var postImageEntity = context?.PostEntityImages?.Values?.FirstOrDefault();");
        generatedSource.Should().Contain("var postImage = postImageEntity != null ? new PostImage(postImageEntity) : null;");
        generatedSource.Should().Contain("service.Process(preImage, postImage)");
    }

    [Fact]
    public void Should_Generate_ActionWrapper_With_Service_Resolution()
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

        // Verify service is resolved from service provider
        generatedSource.Should().Contain("var service = serviceProvider.GetRequiredService<");
        generatedSource.Should().Contain("ITestService");
    }

    [Fact]
    public void Should_Generate_ActionWrapper_For_Handler_Without_Images()
    {
        // Arrange - Plugin with method reference syntax but NO images
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithHandlerNoImages());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Verify ActionWrapper class structure is generated
        generatedSource.Should().Contain("internal sealed class ActionWrapper : IActionWrapper");
        generatedSource.Should().Contain("public Action<IExtendedServiceProvider> CreateAction()");

        // Verify service method is called WITHOUT any image parameters
        generatedSource.Should().Contain("service.HandleUpdate()");

        // Verify NO PreImage or PostImage classes are generated
        generatedSource.Should().NotContain("public class PreImage");
        generatedSource.Should().NotContain("public class PostImage");

        // Verify NO image entity retrieval is generated
        generatedSource.Should().NotContain("PreEntityImages");
        generatedSource.Should().NotContain("PostEntityImages");
    }

    [Fact]
    public void Should_Generate_ActionWrapper_With_PreImage_Only()
    {
        // Arrange - Plugin with PreImage only (no PostImage)
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithPreImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Verify ActionWrapper calls service with preImage parameter
        generatedSource.Should().Contain("var preImageEntity = context?.PreEntityImages?.Values?.FirstOrDefault();");
        generatedSource.Should().Contain("var preImage = preImageEntity != null ? new PreImage(preImageEntity) : null;");
        generatedSource.Should().Contain("service.Process(preImage)");

        // Verify PreImage class IS generated
        generatedSource.Should().Contain("public class PreImage");

        // Verify NO PostImage class is generated
        generatedSource.Should().NotContain("public class PostImage");

        // Verify NO PostEntityImages retrieval
        generatedSource.Should().NotContain("PostEntityImages");
    }

    [Fact]
    public void Should_Generate_ActionWrapper_With_PostImage_Only()
    {
        // Arrange - Plugin with PostImage only (no PreImage)
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithPostImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Verify ActionWrapper calls service with postImage parameter
        generatedSource.Should().Contain("var postImageEntity = context?.PostEntityImages?.Values?.FirstOrDefault();");
        generatedSource.Should().Contain("var postImage = postImageEntity != null ? new PostImage(postImageEntity) : null;");
        generatedSource.Should().Contain("service.Process(postImage)");

        // Verify PostImage class IS generated
        generatedSource.Should().Contain("public class PostImage");

        // Verify NO PreImage class is generated
        generatedSource.Should().NotContain("public class PreImage");

        // Verify NO PreEntityImages retrieval
        generatedSource.Should().NotContain("PreEntityImages");
    }

    [Fact]
    public void Should_Generate_ActionWrapper_With_Both_PreImage_And_PostImage()
    {
        // Arrange - Plugin with both PreImage and PostImage
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithBothImages());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Verify ActionWrapper calls service with both image parameters
        generatedSource.Should().Contain("var preImageEntity = context?.PreEntityImages?.Values?.FirstOrDefault();");
        generatedSource.Should().Contain("var preImage = preImageEntity != null ? new PreImage(preImageEntity) : null;");
        generatedSource.Should().Contain("var postImageEntity = context?.PostEntityImages?.Values?.FirstOrDefault();");
        generatedSource.Should().Contain("var postImage = postImageEntity != null ? new PostImage(postImageEntity) : null;");
        generatedSource.Should().Contain("service.Process(preImage, postImage)");

        // Verify both PreImage and PostImage classes ARE generated
        generatedSource.Should().Contain("public class PreImage");
        generatedSource.Should().Contain("public class PostImage");
    }
}
