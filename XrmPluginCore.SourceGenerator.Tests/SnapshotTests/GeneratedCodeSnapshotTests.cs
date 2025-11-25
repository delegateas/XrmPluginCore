using FluentAssertions;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.SnapshotTests;

/// <summary>
/// Snapshot tests that verify the exact structure of generated code.
/// These tests ensure consistency in code generation patterns.
/// </summary>
public class GeneratedCodeSnapshotTests
{
    [Fact]
    public void Should_Generate_PreImage_Class_With_Expected_Structure()
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

        // Verify essential structure elements
        var expectedElements = new[]
        {
            "namespace TestNamespace.PluginImages.TestPlugin.AccountUpdatePostOperation",
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
            TestFixtures.AccountEntity,
            TestFixtures.GetPluginWithPostImage());

        // Act
        var result = GeneratorTestHelper.RunGenerator(
            CompilationHelper.CreateCompilation(source));

        // Assert
        var generatedSource = result.GeneratedTrees[0].GetText().ToString();

        // Verify essential structure elements
        var expectedElements = new[]
        {
            "namespace TestNamespace.PluginImages.TestPlugin.AccountUpdatePostOperation",
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
            TestFixtures.AccountEntity,
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
                    TestFixtures.AccountEntity,
                    TestFixtures.GetPluginWithPreImage()),
                ExpectedNamespace = "TestNamespace.PluginImages.TestPlugin.AccountUpdatePostOperation"
            },
            new
            {
                Source = TestFixtures.GetCompleteSource(
                    TestFixtures.ContactEntity,
                    TestFixtures.GetPluginWithPreImage("Contact")),
                ExpectedNamespace = "TestNamespace.PluginImages.TestPlugin.ContactUpdatePostOperation"
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
                $"namespace should follow pattern: {{Namespace}}.PluginImages.{{Plugin}}.{{Entity}}{{Operation}}{{Stage}}");
        }
    }

    [Fact]
    public void Should_Mark_Classes_With_CompilerGenerated_Attribute()
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

        // Should have using statement
        generatedSource.Should().Contain("using System.Runtime.CompilerServices;");

        // Both classes should be marked
        generatedSource.Should().Contain("[CompilerGenerated]");

        // Count occurrences - should be at least 2 (one for each class)
        var matches = System.Text.RegularExpressions.Regex.Matches(generatedSource, @"\[CompilerGenerated\]");
        matches.Count.Should().BeGreaterOrEqualTo(2, "both PreImage and PostImage classes should be marked");
    }
}
