using FluentAssertions;
using Microsoft.Xrm.Sdk;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.IntegrationTests;

/// <summary>
/// Integration tests that verify generated code compiles and runs correctly.
/// </summary>
public class CompilationTests
{
	[Fact]
	public void Should_Compile_Generated_Code_Without_Errors()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithPreImage());

		// Act
		var result = GeneratorTestHelper.RunGeneratorAndCompile(source);

		// Assert
		result.Success.Should().BeTrue(
			because: $"compilation should succeed. Errors: {string.Join(", ", result.Errors ?? [])}");
		result.AssemblyBytes.Should().NotBeNull();
	}

	[Fact]
	public void Should_Instantiate_Generated_PreImage_Class_Via_Reflection()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithPreImage());

		var result = GeneratorTestHelper.RunGeneratorAndCompile(source);
		result.Success.Should().BeTrue();

		// Create test entity
		var entity = new Entity("account")
		{
			["name"] = "Test Account",
			["revenue"] = new Money(100000)
		};

		// Act
		using var loadedAssembly = GeneratorTestHelper.LoadAssembly(result.AssemblyBytes!);
		var preImageType = loadedAssembly.Assembly.GetType(
			"TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation.PreImage");

		preImageType.Should().NotBeNull("PreImage class should be generated");

		var preImageInstance = Activator.CreateInstance(preImageType!, entity);

		// Assert
		preImageInstance.Should().NotBeNull();

		var nameProperty = preImageType!.GetProperty("Name");
		nameProperty.Should().NotBeNull();
		var nameValue = nameProperty!.GetValue(preImageInstance);
		nameValue.Should().Be("Test Account");

		var revenueProperty = preImageType.GetProperty("Revenue");
		revenueProperty.Should().NotBeNull();
		var revenueValue = revenueProperty!.GetValue(preImageInstance) as decimal?;
		revenueValue!.Should().Be(100000);
	}

	[Fact]
	public void Should_Access_Properties_And_Verify_Values()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.PluginWithPostImage);

		var result = GeneratorTestHelper.RunGeneratorAndCompile(source);
		result.Success.Should().BeTrue();

		var entity = new Entity("account")
		{
			["name"] = "Test Account",
			["accountnumber"] = "ACC-12345"
		};

		// Act
		using var loadedAssembly = GeneratorTestHelper.LoadAssembly(result.AssemblyBytes!);
		var postImageType = loadedAssembly.Assembly.GetType(
			"TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation.PostImage");

		var postImageInstance = Activator.CreateInstance(postImageType!, entity);

		// Assert
		var nameProperty = postImageType!.GetProperty("Name");
		nameProperty!.GetValue(postImageInstance).Should().Be("Test Account");

		var accountNumberProperty = postImageType.GetProperty("AccountNumber");
		accountNumberProperty!.GetValue(postImageInstance).Should().Be("ACC-12345");
	}

	[Fact]
	public void Should_Work_With_Both_PreImage_And_PostImage()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.PluginWithBothImages);

		var result = GeneratorTestHelper.RunGeneratorAndCompile(source);
		result.Success.Should().BeTrue();

		var preEntity = new Entity("account")
		{
			["name"] = "Old Name",
			["revenue"] = new Money(50000)
		};

		var postEntity = new Entity("account")
		{
			["name"] = "New Name",
			["accountnumber"] = "ACC-12345"
		};

		// Act
		using var loadedAssembly = GeneratorTestHelper.LoadAssembly(result.AssemblyBytes!);
		const string baseNamespace = "TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation";

		var preImageType = loadedAssembly.Assembly.GetType($"{baseNamespace}.PreImage");
		var postImageType = loadedAssembly.Assembly.GetType($"{baseNamespace}.PostImage");

		var preImageInstance = Activator.CreateInstance(preImageType!, preEntity);
		var postImageInstance = Activator.CreateInstance(postImageType!, postEntity);

		// Assert - PreImage
		preImageType!.GetProperty("Name")!.GetValue(preImageInstance).Should().Be("Old Name");
		var preRevenue = preImageType.GetProperty("Revenue")!.GetValue(preImageInstance) as decimal?;
		preRevenue!.Should().Be(50000);

		// Assert - PostImage
		postImageType!.GetProperty("Name")!.GetValue(postImageInstance).Should().Be("New Name");
		postImageType.GetProperty("AccountNumber")!.GetValue(postImageInstance).Should().Be("ACC-12345");
	}

	[Fact]
	public void Should_Handle_Null_Attribute_Values_Gracefully()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithPreImage());

		var result = GeneratorTestHelper.RunGeneratorAndCompile(source);
		result.Success.Should().BeTrue();

		// Entity with missing attributes
		var entity = new Entity("account");

		// Act
		using var loadedAssembly = GeneratorTestHelper.LoadAssembly(result.AssemblyBytes!);
		var preImageType = loadedAssembly.Assembly.GetType(
			"TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation.PreImage");

		var preImageInstance = Activator.CreateInstance(preImageType!, entity);

		// Assert - should return null for missing attributes, not throw
		var nameProperty = preImageType!.GetProperty("Name");
		var nameValue = nameProperty!.GetValue(preImageInstance);
		nameValue.Should().BeNull();

		var revenueProperty = preImageType.GetProperty("Revenue");
		var revenueValue = revenueProperty!.GetValue(preImageInstance);
		revenueValue.Should().BeNull();
	}

	[Fact]
	public void Should_Verify_Namespace_Isolation_Per_Registration()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithPreImage());

		var result = GeneratorTestHelper.RunGeneratorAndCompile(source);
		result.Success.Should().BeTrue();

		// Act
		using var loadedAssembly = GeneratorTestHelper.LoadAssembly(result.AssemblyBytes!);

		// Assert - namespace should follow pattern: {Namespace}.PluginRegistrations.{Plugin}.{Entity}{Operation}{Stage}
		const string expectedNamespace = "TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation";
		var preImageType = loadedAssembly.Assembly.GetType($"{expectedNamespace}.PreImage");

		preImageType.Should().NotBeNull("PreImage should be in the expected namespace");
		preImageType!.Namespace.Should().Be(expectedNamespace);
	}

	[Fact]
	public void Should_Generate_ActionWrapper_Class()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithPreImage());

		var result = GeneratorTestHelper.RunGeneratorAndCompile(source);
		result.Success.Should().BeTrue();

		// Act
		using var loadedAssembly = GeneratorTestHelper.LoadAssembly(result.AssemblyBytes!);
		const string expectedNamespace = "TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation";
		var actionWrapperType = loadedAssembly.Assembly.GetType($"{expectedNamespace}.ActionWrapper");

		// Assert
		actionWrapperType.Should().NotBeNull("ActionWrapper should be generated");

		// Verify ActionWrapper implements IActionWrapper interface
		var iactionWrapperInterface = actionWrapperType!.GetInterface("IActionWrapper");
		iactionWrapperInterface.Should().NotBeNull("ActionWrapper should implement IActionWrapper interface");

		// Verify CreateAction method exists (now instance method, not static)
		var createActionMethod = actionWrapperType.GetMethod("CreateAction");
		createActionMethod.Should().NotBeNull("CreateAction method should exist");
		createActionMethod!.IsStatic.Should().BeFalse("CreateAction should be an instance method since ActionWrapper implements IActionWrapper");
	}
}
