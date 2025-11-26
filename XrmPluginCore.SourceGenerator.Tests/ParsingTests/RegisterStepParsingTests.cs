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
    public void Should_Not_Generate_Code_For_Old_AddImage_Api_Without_Method_Reference()
    {
        // Arrange - Old API uses service => service.Process() which is a method invocation,
        // not a method reference. The new generator requires a method reference.
        var source = TestFixtures.GetCompleteSource(
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithOldImageApi());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert - No code should be generated for old API without method reference
        result.GeneratedTrees.Should().BeEmpty();
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

        // Namespace pattern: {Namespace}.PluginRegistrations.{PluginClassName}.{Entity}{Operation}{Stage}
        generatedSource.Should().Contain("namespace TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation");
    }

    [Fact]
    public void Should_Handle_Multiple_Attributes_In_Same_Image()
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
                .AddImage(ImageType.PreImage, x => x.Name, x => x.AccountNumber, x => x.Revenue, x => x.IndustryCode);
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

    [Fact]
    public void Should_Parse_Handler_Method_Name()
    {
        // Arrange - plugin with new API to verify method reference parsing
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
                service => service.HandleAccountUpdate)
                .AddImage(ImageType.PreImage, x => x.Name);
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<ITestService, TestService>();
        }
    }

    public interface ITestService
    {
        void HandleAccountUpdate(PreImage preImage);
    }

    public class TestService : ITestService
    {
        public void HandleAccountUpdate(PreImage preImage) { }
    }
}";

        var source = TestFixtures.GetCompleteSource(TestFixtures.AccountEntity, pluginSource);

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert - should generate ActionWrapper that calls HandleAccountUpdate
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();
        generatedSource.Should().Contain("service.HandleAccountUpdate(preImage)");
    }

    [Fact]
    public void Should_Parse_Parameterless_Method_Reference()
    {
        // Arrange - plugin with a parameterless handler method (no images)
        // This tests the Expression<Func<TService, Action>> overload for parameterless methods
        var pluginSource = @"
using XrmPluginCore;
using XrmPluginCore.Abstractions;
using XrmPluginCore.Enums;
using Microsoft.Extensions.DependencyInjection;
using TestNamespace;
using TestNamespace.PluginRegistrations.TestPlugin.AccountCreatePostOperation;

namespace TestNamespace
{
    public class TestPlugin : Plugin
    {
        public TestPlugin()
        {
            RegisterStep<Account, ITestService>(EventOperation.Create, ExecutionStage.PostOperation,
                service => service.HandleCreate)
                .AddFilteredAttributes(x => x.Name);
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<ITestService, TestService>();
        }
    }

    public interface ITestService
    {
        void HandleCreate();
    }

    public class TestService : ITestService
    {
        public void HandleCreate() { }
    }
}";

        var source = TestFixtures.GetCompleteSource(TestFixtures.AccountEntity, pluginSource);

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert - should generate ActionWrapper that calls HandleCreate with no parameters
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Verify the method name was extracted correctly
        generatedSource.Should().Contain("service.HandleCreate()");

        // Verify ActionWrapper is generated
        generatedSource.Should().Contain("internal sealed class ActionWrapper : IActionWrapper");

        // Verify correct namespace is used
        generatedSource.Should().Contain("namespace TestNamespace.PluginRegistrations.TestPlugin.AccountCreatePostOperation");

        // Verify NO image classes are generated since it's a parameterless method
        generatedSource.Should().NotContain("public class PreImage");
        generatedSource.Should().NotContain("public class PostImage");
    }

    [Fact]
    public void Should_Parse_Parameterless_Method_Reference_With_Custom_Method_Name()
    {
        // Arrange - plugin with a parameterless handler method with a unique name
        // This ensures the method name extraction works for various naming conventions
        var pluginSource = @"
using XrmPluginCore;
using XrmPluginCore.Abstractions;
using XrmPluginCore.Enums;
using Microsoft.Extensions.DependencyInjection;
using TestNamespace;
using TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePreOperation;

namespace TestNamespace
{
    public class TestPlugin : Plugin
    {
        public TestPlugin()
        {
            RegisterStep<Account, ITestService>(EventOperation.Delete, ExecutionStage.PreOperation,
                service => service.OnAccountDeleting)
                .AddFilteredAttributes(x => x.Name);
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<ITestService, TestService>();
        }
    }

    public interface ITestService
    {
        void OnAccountDeleting();
    }

    public class TestService : ITestService
    {
        public void OnAccountDeleting() { }
    }
}";

        var source = TestFixtures.GetCompleteSource(TestFixtures.AccountEntity, pluginSource);

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert - should generate ActionWrapper that calls OnAccountDeleting
        result.GeneratedTrees.Should().NotBeEmpty();
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Verify the custom method name was extracted correctly
        generatedSource.Should().Contain("service.OnAccountDeleting()");

        // Verify correct namespace with Delete operation and PreOperation stage
        generatedSource.Should().Contain("namespace TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePreOperation");
    }
}
