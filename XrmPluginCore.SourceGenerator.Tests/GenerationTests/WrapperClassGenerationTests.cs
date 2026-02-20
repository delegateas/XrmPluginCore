using FluentAssertions;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.GenerationTests;

/// <summary>
/// Tests for verifying wrapper class code generation structure and content.
/// </summary>
public partial class WrapperClassGenerationTests
{
	private const string ContextNamespace = "XrmPluginCore.Tests.Context.BusinessDomain";

	[Fact]
	public void Should_Generate_PreImage_Class_With_Properties()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithPreImage());

		// Act
		var result = GeneratorTestHelper.RunGenerator(
			CompilationHelper.CreateCompilation(source));

		// Assert
		result.GeneratedTrees.Should().NotBeEmpty();
		var generatedSource = result.GeneratedTrees[0].GetText().ToString();

		// Verify class structure
		generatedSource.Should().Contain($"public sealed class PreImage : IEntityImageWrapper<{ContextNamespace}.Account>");
		generatedSource.Should().Contain($"public {ContextNamespace}.Account Entity {{ get; }}");
		generatedSource.Should().Contain("public PreImage(Entity entity)");
		generatedSource.Should().Contain($"Entity = entity.ToEntity<{ContextNamespace}.Account>();");

		// Verify NO PostImage class is generated
		generatedSource.Should().NotContain("public sealed class PostImage");

		// Verify NO PostEntityImages retrieval
		generatedSource.Should().NotContain("PostEntityImages");
	}

	[Fact]
	public void Should_Generate_PostImage_Class_With_Properties()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.PluginWithPostImage);

		// Act
		var result = GeneratorTestHelper.RunGenerator(
			CompilationHelper.CreateCompilation(source));

		// Assert
		result.GeneratedTrees.Should().NotBeEmpty();
		var generatedSource = result.GeneratedTrees[0].GetText().ToString();

		// Verify class structure
		generatedSource.Should().Contain($"public sealed class PostImage : IEntityImageWrapper<{ContextNamespace}.Account>");
		generatedSource.Should().Contain($"public {ContextNamespace}.Account Entity {{ get; }}");
		generatedSource.Should().Contain("public PostImage(Entity entity)");
		generatedSource.Should().Contain($"Entity = entity.ToEntity<{ContextNamespace}.Account>();");

		// Verify properties forward to Entity
		generatedSource.Should().Contain("public string? Name => Entity.Name;");

		HasAccountNumberSummary().Matches(generatedSource).Count.Should().Be(1, $"AccountNumber property should have correct XML summary. Generated source:\n{generatedSource}");

		// Verify NO PreImage class is generated
		generatedSource.Should().NotContain("public sealed class PreImage");

		// Verify NO PreEntityImages retrieval
		generatedSource.Should().NotContain("PreEntityImages");
	}

	[Fact]
	public void Should_Generate_Both_Image_Classes_In_Same_Namespace()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.PluginWithBothImages);

		// Act
		var result = GeneratorTestHelper.RunGenerator(
			CompilationHelper.CreateCompilation(source));

		// Assert
		result.GeneratedTrees.Should().NotBeEmpty();
		var generatedSource = result.GeneratedTrees[0].GetText().ToString();

		// All classes should be in the same namespace
		var namespaceCount = IsAccountUpdatePostOperationNamespace().Matches(generatedSource).Count;

		namespaceCount.Should().Be(1, "all classes should be in the same namespace");

		// All classes should exist
		generatedSource.Should().Contain($"public sealed class PreImage : IEntityImageWrapper<{ContextNamespace}.Account>");
		generatedSource.Should().Contain($"public sealed class PostImage : IEntityImageWrapper<{ContextNamespace}.Account>");
		generatedSource.Should().Contain("internal sealed class ActionWrapper : IActionWrapper");
	}

	[Theory]
	[InlineData("Account")]
	[InlineData("Contact")]
	public void Should_Generate_Properties_With_Correct_Types(string entityType)
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(TestFixtures.GetPluginWithPreImage(entityType));

		// Act
		var result = GeneratorTestHelper.RunGenerator(
			CompilationHelper.CreateCompilation(source));

		// Assert
		var generatedSource = result.GeneratedTrees[0].GetText().ToString();

		// Verify properties forward to Entity with correct types
		if (entityType == "Account")
		{
			generatedSource.Should().Contain("public string? Name => Entity.Name;");
			generatedSource.Should().Contain("public decimal? Revenue => Entity.Revenue;");
			generatedSource.Should().Contain("public XrmPluginCore.Tests.Context.BusinessDomain.account_industrycode? IndustryCode => Entity.IndustryCode;");
		}
		else
		{
			generatedSource.Should().Contain("public string? FirstName => Entity.FirstName;");
			generatedSource.Should().Contain("public Microsoft.Xrm.Sdk.EntityReference? AccountId => Entity.AccountId;");
			generatedSource.Should().Contain("public string? EMailAddress1 => Entity.EMailAddress1;");
		}
	}

	[Fact]
	public void Should_Implement_IEntityWrapper_interface()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithPreImage());

		// Act
		var result = GeneratorTestHelper.RunGenerator(
			CompilationHelper.CreateCompilation(source));

		// Assert
		var generatedSource = result.GeneratedTrees[0].GetText().ToString();

		// Entity property should be public and of the early-bound type
		generatedSource.Should().Contain($": IEntityImageWrapper<{ContextNamespace}.Account>");
		generatedSource.Should().Contain($"public {ContextNamespace}.Account Entity {{ get; }}");
		generatedSource.Should().Contain($"Entity = entity.ToEntity<{ContextNamespace}.Account>();");
	}

	[Fact]
	public void Should_Generate_ActionWrapper_Class()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithPreImage());

		// Act
		var result = GeneratorTestHelper.RunGenerator(
			CompilationHelper.CreateCompilation(source));

		// Assert
		var generatedSource = result.GeneratedTrees[0].GetText().ToString();

		// Verify ActionWrapper class structure (now implements IActionWrapper interface)
		generatedSource.Should().Contain("internal sealed class ActionWrapper : IActionWrapper");
		generatedSource.Should().Contain("public Action<IExtendedServiceProvider> CreateAction()");
		generatedSource.Should().Contain("var service = serviceProvider.GetRequiredService<TestNamespace.ITestService>();");

		// Should have using statement
		generatedSource.Should().Contain("using System.Runtime.CompilerServices;");

		// Count occurrences - should be at least 3 (PreImage, PostImage, and ActionWrapper)
		var matches = IsCompilerGenerated().Matches(generatedSource);
		matches.Count.Should().BeGreaterOrEqualTo(2, "PreImage, and ActionWrapper classes should be marked");
	}

	[Fact]
	public void Should_Generate_ActionWrapper_With_PreImage_Call()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithPreImage());

		// Act
		var result = GeneratorTestHelper.RunGenerator(
			CompilationHelper.CreateCompilation(source));

		// Assert
		var generatedSource = result.GeneratedTrees[0].GetText().ToString();

		// Verify PreImage handling (now inline instead of using PluginImageHelper)
		generatedSource.Should().Contain("var preImageEntity = context?.PreEntityImages?.Values?.FirstOrDefault();");
		generatedSource.Should().Contain("var preImage = preImageEntity != null ? new PreImage(preImageEntity) : null;");
		generatedSource.Should().Contain("service.HandleAccountUpdate(preImage)");

		// Should have using statement
		generatedSource.Should().Contain("using System.Runtime.CompilerServices;");

		// Count occurrences - should be at least 3 (PreImage, PostImage, and ActionWrapper)
		var matches = IsCompilerGenerated().Matches(generatedSource);
		matches.Count.Should().BeGreaterOrEqualTo(2, "PreImage, and ActionWrapper classes should be marked");
	}

	[Fact]
	public void Should_Generate_ActionWrapper_With_PostImage_Call()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.PluginWithPostImage);

		// Act
		var result = GeneratorTestHelper.RunGenerator(
			CompilationHelper.CreateCompilation(source));

		// Assert
		var generatedSource = result.GeneratedTrees[0].GetText().ToString();

		// Verify PostImage handling (now inline instead of using PluginImageHelper)
		generatedSource.Should().Contain("var postImageEntity = context?.PostEntityImages?.Values?.FirstOrDefault();");
		generatedSource.Should().Contain("var postImage = postImageEntity != null ? new PostImage(postImageEntity) : null;");
		generatedSource.Should().Contain("service.HandleAccountUpdate(postImage)");

		// Should have using statement
		generatedSource.Should().Contain("using System.Runtime.CompilerServices;");

		// Count occurrences - should be at least 3 (PreImage, PostImage, and ActionWrapper)
		var matches = IsCompilerGenerated().Matches(generatedSource);
		matches.Count.Should().BeGreaterOrEqualTo(2, "PostImage, and ActionWrapper classes should be marked");
	}

	[Fact]
	public void Should_Generate_ActionWrapper_With_Both_Images()
	{
		// Arrange
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.PluginWithBothImages);

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
		generatedSource.Should().Contain("service.HandleAccountUpdate(preImage, postImage)");

		// Should have using statement
		generatedSource.Should().Contain("using System.Runtime.CompilerServices;");

		// Count occurrences - should be at least 3 (PreImage, PostImage, and ActionWrapper)
		var matches = IsCompilerGenerated().Matches(generatedSource);
		matches.Count.Should().BeGreaterOrEqualTo(3, "PreImage, PostImage, and ActionWrapper classes should be marked");
	}

	[Fact]
	public void Should_Generate_ActionWrapper_For_Handler_Without_Images()
	{
		// Arrange - Plugin with method reference syntax but NO images
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithoutImages());

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

		// Should have using statement
		generatedSource.Should().Contain("using System.Runtime.CompilerServices;");

		// Count occurrences - should be at least 3 (PreImage, PostImage, and ActionWrapper)
		var matches = IsCompilerGenerated().Matches(generatedSource);
		matches.Count.Should().BeGreaterOrEqualTo(1, "ActionWrapper classes should be marked");
	}

	[Fact]
	public void Should_Not_Generate_Code_For_Old_AddImage_Api_Without_Method_Reference()
	{
		// Arrange - Old API uses service => service.Process() which is a method invocation,
		// not a method reference. The new generator requires a method reference.
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.PluginWithLegacyAddImage);

		// Act
		var result = GeneratorTestHelper.RunGenerator(
			CompilationHelper.CreateCompilation(source));

		// Assert - No code should be generated for old API without method reference
		result.GeneratedTrees.Should().BeEmpty();
	}

	[Fact]
	public void Should_Parse_Parameterless_Method_Reference()
	{
		var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithoutImages("service => service.HandleUpdate"));

		// Act
		var result = GeneratorTestHelper.RunGenerator(
			CompilationHelper.CreateCompilation(source));

		// Assert - should generate ActionWrapper that calls HandleUpdate
		result.GeneratedTrees.Should().NotBeEmpty();
		var generatedSource = result.GeneratedTrees[0].GetText().ToString();

		// Verify the custom method name was extracted correctly
		generatedSource.Should().Contain("service.HandleUpdate()");
	}

	[System.Text.RegularExpressions.GeneratedRegex(@"namespace\s+TestNamespace\.PluginRegistrations\.TestPlugin\.AccountUpdatePostOperation")]
	private static partial System.Text.RegularExpressions.Regex IsAccountUpdatePostOperationNamespace();

	[System.Text.RegularExpressions.GeneratedRegex(@"\[CompilerGenerated\]")]
	private static partial System.Text.RegularExpressions.Regex IsCompilerGenerated();

	[System.Text.RegularExpressions.GeneratedRegex(@"/// <summary>\s+/// <para>Type an ID number or code for the account to quickly search and identify the account in system views\.<\/para>\s+/// <para>Display Name: Account Number<\/para>\s+/// <\/summary>")]
	private static partial System.Text.RegularExpressions.Regex HasAccountNumberSummary();
}
