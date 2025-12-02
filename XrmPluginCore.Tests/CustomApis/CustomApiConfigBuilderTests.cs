using FluentAssertions;
using Xunit;
using XrmPluginCore.CustomApis;
using XrmPluginCore.Enums;
using XrmPluginCore.Tests.Context.BusinessDomain;

namespace XrmPluginCore.Tests.CustomApis;

public class CustomApiConfigBuilderTests
{
    [Fact]
    public void WithExecutePrivilegeName_SetsExecutePrivilegeNameOnConfig()
    {
        // Arrange
        var builder = new CustomApiConfigBuilder("test_api");
        const string privilegeName = "prvCustomPrivilege";

        // Act
        builder.WithExecutePrivilegeName(privilegeName);
        var config = builder.Build();

        // Assert
        config.ExecutePrivilegeName.Should().Be(privilegeName);
    }

    [Fact]
    public void WithExecutePrivilegeName_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new CustomApiConfigBuilder("test_api");

        // Act
        var result = builder.WithExecutePrivilegeName("prvTest");

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void Build_WithNoExecutePrivilegeName_ReturnsNullPrivilegeName()
    {
        // Arrange
        var builder = new CustomApiConfigBuilder("test_api");

        // Act
        var config = builder.Build();

        // Assert
        config.ExecutePrivilegeName.Should().BeNull();
    }

    [Fact]
    public void WithExecutePrivilegeName_CanBeChainedWithOtherMethods()
    {
        // Arrange
        var builder = new CustomApiConfigBuilder("test_api");
        const string privilegeName = "prvCustomAction";
        const string description = "Test API description";

        // Act
        var config = builder
            .WithExecutePrivilegeName(privilegeName)
            .SetDescription(description)
            .MakePrivate()
            .EnableForWorkFlow()
            .Build();

        // Assert
        config.ExecutePrivilegeName.Should().Be(privilegeName);
        config.Description.Should().Be(description);
        config.IsPrivate.Should().BeTrue();
        config.EnabledForWorkflow.Should().BeTrue();
    }

    [Fact]
    public void WithExecutePrivilegeName_WithEmptyString_SetsEmptyString()
    {
        // Arrange
        var builder = new CustomApiConfigBuilder("test_api");

        // Act
        builder.WithExecutePrivilegeName("");
        var config = builder.Build();

        // Assert
        config.ExecutePrivilegeName.Should().BeEmpty();
    }

    [Fact]
    public void WithExecutePrivilegeName_CalledMultipleTimes_UsesLastValue()
    {
        // Arrange
        var builder = new CustomApiConfigBuilder("test_api");

        // Act
        builder.WithExecutePrivilegeName("prvFirst");
        builder.WithExecutePrivilegeName("prvSecond");
        builder.WithExecutePrivilegeName("prvFinal");
        var config = builder.Build();

        // Assert
        config.ExecutePrivilegeName.Should().Be("prvFinal");
    }

    [Fact]
    public void Build_PreservesAllConfigurationIncludingExecutePrivilege()
    {
        // Arrange
        var builder = new CustomApiConfigBuilder("test_CompleteApi");
        const string privilegeName = "prvExecuteComplete";

        // Act
        var config = builder
            .WithExecutePrivilegeName(privilegeName)
            .SetDescription("Complete test")
            .Bind<Account>(BindingType.Entity)
            .MakeFunction()
            .EnableCustomization()
            .AddRequestParameter("input", CustomApiParameterType.String, isOptional: false)
            .AddResponseProperty("output", CustomApiParameterType.Integer)
            .Build();

        // Assert
        config.ExecutePrivilegeName.Should().Be(privilegeName);
        config.Name.Should().Be("test_CompleteApi");
        config.Description.Should().Be("Complete test");
        config.BindingType.Should().Be(BindingType.Entity);
        config.BoundEntityLogicalName.Should().Be("account");
        config.IsFunction.Should().BeTrue();
        config.IsCustomizable.Should().BeTrue();
    }
}
