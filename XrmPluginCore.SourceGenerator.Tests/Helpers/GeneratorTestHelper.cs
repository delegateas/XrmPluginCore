using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Runtime.Loader;
using XrmPluginCore.SourceGenerator.Generators;

namespace XrmPluginCore.SourceGenerator.Tests.Helpers;

/// <summary>
/// Helper for running source generators and testing their output.
/// </summary>
public static class GeneratorTestHelper
{
    /// <summary>
    /// Runs the PluginImageGenerator on the provided compilation and returns the updated compilation.
    /// </summary>
    public static GeneratorRunResult RunGenerator(CSharpCompilation compilation)
    {
        var generator = new PluginImageGenerator();
        // Pass the compilation's parse options to the driver so generated syntax trees use the same language version
        var driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            parseOptions: (CSharpParseOptions?)compilation.SyntaxTrees.FirstOrDefault()?.Options);

        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        var runResult = driver.GetRunResult();

        // Get generated trees from the output compilation (they have consistent parse options)
        // instead of from runResult.GeneratedTrees (which may have inconsistent options)
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(tree => !compilation.SyntaxTrees.Contains(tree))
            .ToArray();

        return new GeneratorRunResult
        {
            OutputCompilation = (CSharpCompilation)outputCompilation,
            Diagnostics = diagnostics.ToArray(),
            GeneratedTrees = generatedTrees,
            GeneratorDiagnostics = runResult.Results[0].Diagnostics.ToArray()
        };
    }

    /// <summary>
    /// Runs the generator and compiles the output to an in-memory assembly.
    /// </summary>
    public static CompiledGeneratorResult RunGeneratorAndCompile(string source)
    {
        var compilation = CompilationHelper.CreateCompilation(source);
        var result = RunGenerator(compilation);

        using var ms = new MemoryStream();
        var emitResult = result.OutputCompilation.Emit(ms);

        if (!emitResult.Success)
        {
            var errors = emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => $"{d.Id}: {d.GetMessage()}")
                .ToArray();

            return new CompiledGeneratorResult
            {
                Success = false,
                Errors = errors,
                GeneratorResult = result
            };
        }

        ms.Seek(0, SeekOrigin.Begin);

        return new CompiledGeneratorResult
        {
            Success = true,
            AssemblyBytes = ms.ToArray(),
            GeneratorResult = result
        };
    }

    /// <summary>
    /// Loads a compiled assembly in an isolated AssemblyLoadContext for testing.
    /// </summary>
    public static LoadedAssemblyContext LoadAssembly(byte[] assemblyBytes, string contextName = "TestContext")
    {
        var context = new AssemblyLoadContext(contextName, isCollectible: true);
        using var ms = new MemoryStream(assemblyBytes);
        var assembly = context.LoadFromStream(ms);

        return new LoadedAssemblyContext
        {
            Context = context,
            Assembly = assembly
        };
    }
}

/// <summary>
/// Result from running the source generator.
/// </summary>
public class GeneratorRunResult
{
    public required CSharpCompilation OutputCompilation { get; init; }
    public required Diagnostic[] Diagnostics { get; init; }
    public required SyntaxTree[] GeneratedTrees { get; init; }
    public required Diagnostic[] GeneratorDiagnostics { get; init; }
}

/// <summary>
/// Result from compiling generated code.
/// </summary>
public class CompiledGeneratorResult
{
    public required bool Success { get; init; }
    public byte[]? AssemblyBytes { get; init; }
    public string[]? Errors { get; init; }
    public required GeneratorRunResult GeneratorResult { get; init; }
}

/// <summary>
/// A loaded assembly in an isolated context that can be unloaded.
/// </summary>
public class LoadedAssemblyContext : IDisposable
{
    public required AssemblyLoadContext Context { get; init; }
    public required Assembly Assembly { get; init; }

    public void Dispose()
    {
        Context.Unload();
        GC.SuppressFinalize(this);
    }
}
