using FluentAssertions;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.SnapshotTests;

/// <summary>
/// Snapshot tests that verify the exact structure of generated code.
/// These tests ensure consistency in code generation patterns.
/// </summary>
public partial class GeneratedCodeSnapshotTests
{
    [Fact]
    public void Should_Generate_PreImage_Class_With_Expected_Structure()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithPreImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Verify essential structure elements
        var expectedElements = new[]
        {
            "namespace TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation",
            "public class PreImage : IEntityImageWrapper",
            "private readonly Entity entity;",
            "public PreImage(Entity entity)",
            "this.entity = entity ?? throw new ArgumentNullException(nameof(entity));",
            "public Entity GetUnderlyingEntity()",
            "=> entity;",
            "public T ToEntity<T>() where T : Entity",
            "=> entity.ToEntity<T>();",
            "[CompilerGenerated]"
        };

        foreach (var element in expectedElements)
        {
            generatedSource.Should().Contain(element, $"generated code should contain: {element}");
        }
    }

    [Fact]
    public void Should_Generate_PostImage_Class_With_Expected_Structure()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithPostImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Verify essential structure elements
        var expectedElements = new[]
        {
            "namespace TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation",
            "public class PostImage : IEntityImageWrapper",
            "private readonly Entity entity;",
            "public PostImage(Entity entity)",
            "this.entity = entity ?? throw new ArgumentNullException(nameof(entity));",
            "public Entity GetUnderlyingEntity()",
            "public T ToEntity<T>() where T : Entity",
            "[CompilerGenerated]"
        };

        foreach (var element in expectedElements)
        {
            generatedSource.Should().Contain(element, $"generated code should contain: {element}");
        }
    }

    [Fact]
    public void Should_Include_XML_Documentation_Comments()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithPreImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Check for XML documentation
        generatedSource.Should().Contain("/// <summary>");
        generatedSource.Should().Contain("/// </summary>");
    }

    [Fact]
    public void Should_Follow_Namespace_Pattern()
    {
        // Arrange - test different entity/operation combinations
        var testCases = new[]
        {
            new
            {
                Source = TestFixtures.GetCompleteSource(
					TestFixtures.GetPluginWithPreImage()),
                ExpectedNamespace = "TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation"
            },
            new
            {
                Source = TestFixtures.GetCompleteSource(
					TestFixtures.GetPluginWithPreImage("Contact")),
                ExpectedNamespace = "TestNamespace.PluginRegistrations.TestPlugin.ContactUpdatePostOperation"
            }
        };

        foreach (var testCase in testCases)
        {
            // Act
            var result = GeneratorTestHelper.RunGenerator(
                CompilationHelper.CreateCompilation(testCase.Source));

            // Assert
            var generatedSource = result.GeneratedTrees[0].GetText().ToString();
            generatedSource.Should().Contain($"namespace {testCase.ExpectedNamespace}",
                "namespace should follow pattern: {Namespace}.PluginRegistrations.{Plugin}.{Entity}{Operation}{Stage}");
        }
    }

    [Fact]
    public void Should_Mark_Classes_With_CompilerGenerated_Attribute()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithBothImages());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Should have using statement
        generatedSource.Should().Contain("using System.Runtime.CompilerServices;");

        // Both classes should be marked
        generatedSource.Should().Contain("[CompilerGenerated]");

        // Count occurrences - should be at least 3 (PreImage, PostImage, and ActionWrapper)
        var matches = IsCompilerGenerated().Matches(generatedSource);
        matches.Count.Should().BeGreaterOrEqualTo(3, "PreImage, PostImage, and ActionWrapper classes should be marked");
    }

    [Fact]
    public void Should_Generate_ActionWrapper_Class_For_New_Api()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithPreImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Verify ActionWrapper structure (now implements IActionWrapper interface with inline image construction)
        var expectedElements = new[]
        {
            "internal sealed class ActionWrapper : IActionWrapper",
            "public Action<IExtendedServiceProvider> CreateAction()",
            "serviceProvider =>",
            "var service = serviceProvider.GetRequiredService<",
            "var preImageEntity = context?.PreEntityImages?.Values?.FirstOrDefault();",
            "var preImage = preImageEntity != null ? new PreImage(preImageEntity) : null;",
            "service.Process(preImage)"
        };

        foreach (var element in expectedElements)
        {
            generatedSource.Should().Contain(element, $"generated code should contain: {element}");
        }
    }

    [Fact]
    public void Should_Generate_ActionWrapper_With_Both_Images()
    {
        // Arrange
        var source = TestFixtures.GetCompleteSource(
			TestFixtures.GetPluginWithBothImages());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Verify ActionWrapper handles both images (now inline instead of using PluginImageHelper)
        generatedSource.Should().Contain("var preImageEntity = context?.PreEntityImages?.Values?.FirstOrDefault();");
        generatedSource.Should().Contain("var preImage = preImageEntity != null ? new PreImage(preImageEntity) : null;");
        generatedSource.Should().Contain("var postImageEntity = context?.PostEntityImages?.Values?.FirstOrDefault();");
        generatedSource.Should().Contain("var postImage = postImageEntity != null ? new PostImage(postImageEntity) : null;");
        generatedSource.Should().Contain("service.Process(preImage, postImage)");
    }

	[System.Text.RegularExpressions.GeneratedRegex(@"\[CompilerGenerated\]")]
	private static partial System.Text.RegularExpressions.Regex IsCompilerGenerated();
}
