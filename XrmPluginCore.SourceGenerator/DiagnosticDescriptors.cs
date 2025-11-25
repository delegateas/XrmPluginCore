using Microsoft.CodeAnalysis;

namespace XrmPluginCore.SourceGenerator;

/// <summary>
/// Diagnostic descriptors for the source generator
/// </summary>
internal static class DiagnosticDescriptors
{
	private const string Category = "XrmPluginCore.SourceGenerator";

	public static readonly DiagnosticDescriptor GenerationSuccess = new(
		id: "XPC1000",
		title: "Generated type-safe wrapper classes",
		messageFormat: "Generated {0} wrapper class(es) for {1}",
		category: Category,
		DiagnosticSeverity.Info,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor GenerationError = new(
		id: "XPC5000",
		title: "Failed to generate wrapper classes",
		messageFormat: "Exception during generation: {0}",
		category: Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor SymbolResolutionFailed = new(
		id: "XPC4000",
		title: "Failed to resolve symbol",
		messageFormat: "Could not resolve RegisterStep method symbol in {0}. Image wrappers will not be generated for this registration.",
		category: Category,
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor NoParameterlessConstructor = new(
		id: "XPC4001",
		title: "No parameterless constructor found",
		messageFormat: "Plugin class '{0}' has no parameterless constructor. Image wrappers will not be generated for this plugin.",
		category: Category,
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true);
}
