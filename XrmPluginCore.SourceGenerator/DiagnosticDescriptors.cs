using Microsoft.CodeAnalysis;

namespace XrmPluginCore.SourceGenerator;

/// <summary>
/// Diagnostic descriptors for the source generator.
/// Categories:
/// - XPC1xxx: Informational messages (generation status)
/// - XPC2xxx: Plugin class structure issues (constructor, inheritance)
/// - XPC3xxx: Code style and best practices (nameof, modern API)
/// - XPC4xxx: Handler method issues (not found, signature mismatch)
/// - XPC5xxx: Internal errors (generation failures, symbol resolution)
/// </summary>
public static class DiagnosticDescriptors
{
	private const string Category = "XrmPluginCore.SourceGenerator";
	private const string HelpLinkBaseUri = "https://github.com/delegateas/XrmPluginCore/blob/main/XrmPluginCore.SourceGenerator/rules";

	public static readonly DiagnosticDescriptor GenerationSuccess = new(
		id: "XPC1001",
		title: "Generated type-safe wrapper classes",
		messageFormat: "Generated {0} wrapper class(es) for {1}",
		category: Category,
		DiagnosticSeverity.Info,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor NoParameterlessConstructor = new(
		id: "XPC2001",
		title: "No parameterless constructor found",
		messageFormat: "Plugin class '{0}' has no parameterless constructor. Image wrappers will not be generated for this plugin.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: $"{HelpLinkBaseUri}/XPC2001.md");

	public static readonly DiagnosticDescriptor PreferNameofOverStringLiteral = new(
		id: "XPC3001",
		title: "Prefer nameof over string literal for handler method",
		messageFormat: "Use 'nameof({0}.{1})' instead of string literal \"{1}\" for compile-time safety",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Using nameof() provides compile-time verification that the method exists and enables refactoring support.",
		helpLinkUri: $"{HelpLinkBaseUri}/XPC3001.md");

	public static readonly DiagnosticDescriptor LegacyImageRegistration = new(
		id: "XPC3002",
		title: "Consider using modern image registration API",
		messageFormat: "Consider using WithPreImage/WithPostImage with nameof() instead of AddImage for type-safe image wrappers",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: $"{HelpLinkBaseUri}/XPC3002.md");

	public static readonly DiagnosticDescriptor ImageWithoutMethodReference = new(
		id: "XPC3003",
		title: "Image registration without method reference",
		messageFormat: "WithPreImage/WithPostImage requires method reference syntax (e.g., 'service => service.HandleUpdate'). Using method invocation (e.g., 's => s.HandleUpdate()') will not generate type-safe wrappers.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: $"{HelpLinkBaseUri}/XPC3003.md");

	public static readonly DiagnosticDescriptor HandlerMethodNotFound = new(
		id: "XPC4001",
		title: "Handler method not found",
		messageFormat: "Method '{0}' not found on service type '{1}'",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: $"{HelpLinkBaseUri}/XPC4001.md");

	public static readonly DiagnosticDescriptor HandlerSignatureMismatch = new(
		id: "XPC4002",
		title: "Handler signature does not match registered images",
		messageFormat: "Handler method '{0}' does not have expected signature. Expected parameters in order: {1}. PreImage must be the first parameter, followed by PostImage if both are used.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: $"{HelpLinkBaseUri}/XPC4002.md");

	public static readonly DiagnosticDescriptor HandlerSignatureMismatchError = new(
		id: "XPC4003",
		title: "Handler signature does not match registered images",
		messageFormat: "Handler method '{0}' does not have expected signature. Expected parameters in order: {1}. PreImage must be the first parameter, followed by PostImage if both are used.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: $"{HelpLinkBaseUri}/XPC4003.md");

	public static readonly DiagnosticDescriptor SymbolResolutionFailed = new(
		id: "XPC5001",
		title: "Failed to resolve symbol",
		messageFormat: "Could not resolve RegisterStep method symbol in {0}. Image wrappers will not be generated for this registration.",
		category: Category,
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor GenerationError = new(
		id: "XPC5002",
		title: "Failed to generate wrapper classes",
		messageFormat: "Exception during generation: {0}",
		category: Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

}
