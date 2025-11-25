# XrmPluginCore.SourceGenerator.Tests

This project contains unit and integration tests for the XrmPluginCore.SourceGenerator, which generates type-safe wrapper classes for plugin images (PreImage/PostImage).

## Overview

The source generator analyzes plugin classes that inherit from `Plugin` and generates strongly-typed wrapper classes for images registered via `WithPreImage()` and `WithPostImage()` methods. These tests verify that the generator correctly parses plugin registrations, generates valid code, and that the generated code compiles and runs as expected.

## Testing Approach

This project uses a **hybrid testing approach** combining two complementary strategies:

### 1. Compiled Execution Testing (Primary)
Tests functional correctness by:
- Running the generator on test source code
- Compiling the generated code
- Loading the compiled assembly in an isolated `AssemblyLoadContext`
- Verifying runtime behavior via reflection

**Benefits:**
- Tests what actually matters: does the generated code work?
- Resilient to implementation changes (refactoring-friendly)
- Validates generated code compiles and runs correctly
- Can test actual runtime behavior

### 2. Snapshot Testing (Structural Verification)
Tests generated code structure by:
- Capturing the exact generated source code
- Verifying presence of expected patterns and elements
- Ensuring consistent code generation

**Benefits:**
- Fast execution
- Catches unintended changes in code generation
- Ensures consistent code patterns

## Test Organization

### Helpers/
Reusable test infrastructure:

- **CompilationHelper.cs** - Creates `CSharpCompilation` instances with proper references to Dataverse SDK and XrmPluginCore assemblies
- **GeneratorTestHelper.cs** - Runs the generator, compiles output, loads assemblies in isolated contexts
- **TestFixtures.cs** - Provides sample entity classes (Account, Contact) and common plugin registration patterns

### ParsingTests/
Tests for metadata extraction from plugin source code:

- `RegisterStepParsingTests.cs` - Tests parsing of `RegisterStep` invocations with various image configurations
  - WithPreImage only
  - WithPostImage only
  - Both images
  - Old AddImage API (backward compatibility)
  - Lambda, nameof, and string literal attribute syntax
  - Multiple attributes per image

### GenerationTests/
Tests for code generation structure and content:

- `WrapperClassGenerationTests.cs` - Verifies generated wrapper class structure
  - PreImage/PostImage class structure
  - Property generation with correct types
  - ToEntity<T>() method
  - GetUnderlyingEntity() method
  - IEntityImageWrapper interface implementation

### IntegrationTests/
End-to-end tests that verify generated code compiles and runs:

- `CompilationTests.cs` - Tests complete generation → compilation → execution flow
  - Compilation success
  - Assembly loading and instantiation
  - Property access and value verification
  - Null handling
  - Namespace isolation

### DiagnosticTests/
Tests for source generator diagnostic reporting:

- `DiagnosticReportingTests.cs` - Verifies diagnostic codes are reported correctly
  - XPC1000: Generation success (Info)
  - XPC4001: Property not found (Warning)
  - XPC4002: No parameterless constructor (Warning)
  - XPC5000: Generation error handling

### SnapshotTests/
Tests for exact code structure verification:

- `GeneratedCodeSnapshotTests.cs` - Verifies generated code follows expected patterns
  - Class structure elements
  - XML documentation
  - Namespace patterns
  - [CompilerGenerated] attribute

## Running Tests

### Run All Tests
```bash
dotnet test --configuration Release
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~CompilationTests"
```

### Run Single Test
```bash
dotnet test --filter "FullyQualifiedName~Should_Compile_Generated_Code_Without_Errors"
```

### Run with Detailed Output
```bash
dotnet test --configuration Release --verbosity normal
```

## Adding New Tests

### Adding a Parsing Test
1. Create test source code (or use `TestFixtures` helpers)
2. Run the generator via `GeneratorTestHelper.RunGenerator()`
3. Assert on `result.GeneratedTrees` content

```csharp
[Fact]
public void Should_Parse_New_Pattern()
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
    generatedSource.Should().Contain("expected pattern");
}
```

### Adding a Compilation Test
1. Create test source code
2. Run generator and compile via `GeneratorTestHelper.RunGeneratorAndCompile()`
3. Load assembly via `GeneratorTestHelper.LoadAssembly()`
4. Test via reflection

```csharp
[Fact]
public void Should_Test_Runtime_Behavior()
{
    // Arrange
    var source = TestFixtures.GetCompleteSource(...);
    var result = GeneratorTestHelper.RunGeneratorAndCompile(source);
    result.Success.Should().BeTrue();

    // Act
    using var loadedAssembly = GeneratorTestHelper.LoadAssembly(result.AssemblyBytes!);
    var type = loadedAssembly.Assembly.GetType("Namespace.PreImage");
    var instance = Activator.CreateInstance(type, testEntity);

    // Assert
    var property = type.GetProperty("PropertyName");
    property!.GetValue(instance).Should().Be("expected value");
}
```

### Adding a Diagnostic Test
1. Create source code that should trigger a diagnostic
2. Run the generator
3. Assert on `result.GeneratorDiagnostics`

```csharp
[Fact]
public void Should_Report_Diagnostic_Code()
{
    // Arrange
    var source = "code that triggers diagnostic";

    // Act
    var result = GeneratorTestHelper.RunGenerator(
        CompilationHelper.CreateCompilation(source));

    // Assert
    var diagnostics = result.GeneratorDiagnostics
        .Where(d => d.Id == "XPC####")
        .ToArray();

    diagnostics.Should().NotBeEmpty();
}
```

## Test Coverage

Current test coverage includes:

**Parsing (~8 tests)**
- ✅ WithPreImage registration
- ✅ WithPostImage registration
- ✅ Both PreImage and PostImage
- ✅ Old AddImage API
- ✅ Lambda syntax
- ✅ Multiple entities
- ✅ Namespace generation
- ✅ Multiple attributes

**Generation (~7 tests)**
- ✅ PreImage class structure
- ✅ PostImage class structure
- ✅ Both classes in same namespace
- ✅ Property types (string, Money, OptionSetValue, EntityReference)
- ✅ ToEntity<T>() method
- ✅ GetUnderlyingEntity() method
- ✅ IEntityImageWrapper interface

**Integration (~6 tests)**
- ✅ Compilation success
- ✅ PreImage instantiation
- ✅ Property access
- ✅ Both images
- ✅ Null handling
- ✅ Namespace isolation

**Diagnostics (~4 tests)**
- ✅ XPC1000 success diagnostic
- ✅ XPC4001 property not found
- ✅ XPC4002 no parameterless constructor
- ✅ XPC5000 error handling

**Snapshots (~5 tests)**
- ✅ PreImage structure
- ✅ PostImage structure
- ✅ XML documentation
- ✅ Namespace pattern
- ✅ [CompilerGenerated] attribute

**Total: ~30 tests** with standard coverage of core scenarios and common patterns.

## Common Issues and Solutions

### Issue: "Type or namespace could not be found"
**Solution:** Ensure `CompilationHelper.CreateCompilation()` includes all necessary references. The helper automatically includes Dataverse SDK and XrmPluginCore references.

### Issue: "AssemblyLoadContext cannot be unloaded"
**Solution:** Always use `using` statements with `LoadedAssemblyContext` to ensure proper cleanup:
```csharp
using var loadedAssembly = GeneratorTestHelper.LoadAssembly(bytes);
// ... test code ...
// Automatically unloaded when scope exits
```

### Issue: Tests pass locally but fail in CI
**Solution:** Verify all required NuGet packages are restored. Run `dotnet restore` before `dotnet test`.

## Dependencies

- **xUnit** - Test framework
- **FluentAssertions** - Fluent assertion library
- **Microsoft.CodeAnalysis.CSharp** - For creating compilations and running generators
- **Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit** - For snapshot testing support
- **Microsoft.PowerPlatform.Dataverse.Client** - Dataverse SDK for test compilations

## References

- [Roslyn Source Generators Documentation](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.md)
- [Testing Source Generators - Thinktecture](https://www.thinktecture.com/en/net/roslyn-source-generators-analyzers-code-fixes-testing/)
- [How to Test Source Generators - Meziantou](https://www.meziantou.net/how-to-test-roslyn-source-generators.htm)
