using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using XrmPluginCore.Enums;

namespace XrmPluginCore.SourceGenerator.Tests.Helpers;

/// <summary>
/// Helper for creating test compilations with proper references for testing source generators.
/// </summary>
public static class CompilationHelper
{
    /// <summary>
    /// Creates a CSharpCompilation with the necessary references for Dataverse plugin development.
    /// </summary>
    /// <param name="source">The C# source code to compile</param>
    /// <param name="assemblyName">Optional assembly name (defaults to random GUID)</param>
    /// <returns>A configured CSharpCompilation</returns>
    public static CSharpCompilation CreateCompilation(string source, string? assemblyName = null)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp11));

        var references = GetMetadataReferences();

        return CSharpCompilation.Create(
            assemblyName ?? $"TestAssembly_{Guid.NewGuid():N}",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    /// <summary>
    /// Gets all necessary metadata references for plugin compilation.
    /// </summary>
    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        // Basic .NET references
        yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(Console).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location); // System.Linq.Expressions
        yield return MetadataReference.CreateFromFile(typeof(System.ComponentModel.DescriptionAttribute).Assembly.Location); // System.ComponentModel
        yield return MetadataReference.CreateFromFile(typeof(IServiceProvider).Assembly.Location); // IServiceProvider
        yield return MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location);
        yield return MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location);
        yield return MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location);

        // Dataverse SDK references
        yield return MetadataReference.CreateFromFile(typeof(Microsoft.Xrm.Sdk.IPlugin).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(Microsoft.Xrm.Sdk.Entity).Assembly.Location);

        // XrmPluginCore references
        yield return MetadataReference.CreateFromFile(typeof(Plugin).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(EventOperation).Assembly.Location);

        // Microsoft.Extensions.DependencyInjection (required by Plugin base class)
        yield return MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions).Assembly.Location);
    }
}
