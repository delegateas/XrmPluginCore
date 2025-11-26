using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using XrmPluginCore.SourceGenerator.CodeGeneration;
using XrmPluginCore.SourceGenerator.Helpers;
using XrmPluginCore.SourceGenerator.Models;
using XrmPluginCore.SourceGenerator.Parsers;
using XrmPluginCore.SourceGenerator.Validation;

namespace XrmPluginCore.SourceGenerator.Generators;

/// <summary>
/// Incremental source generator that creates type-safe wrapper classes for plugin images (PreImage/PostImage)
/// </summary>
[Generator]
public class PluginImageGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Create incremental pipeline that processes each plugin class individually
		var pluginMetadata = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: static (node, _) => IsCandidateClass(node),
				transform: static (ctx, ct) => TransformToMetadata(ctx, ct))
			.Where(static m => m is not null)
			.SelectMany(static (list, _) => list); // Flatten multiple registrations per class

		// Register source output per metadata item (incremental - only changed items reprocessed)
		context.RegisterSourceOutput(pluginMetadata, (spc, metadata) => GenerateSourceFromMetadata(metadata, spc));
	}

	/// <summary>
	/// Fast syntax-based predicate to identify candidate classes
	/// </summary>
	private static bool IsCandidateClass(SyntaxNode node)
	{
		// Look for class declarations
		if (node is not ClassDeclarationSyntax classDecl)
			return false;

		// Must have a constructor
		if (!classDecl.Members.OfType<ConstructorDeclarationSyntax>().Any())
			return false;

		return true;
	}

	/// <summary>
	/// Transform phase: Extract all metadata from a plugin class.
	/// This does all the heavy semantic analysis in a cacheable transform phase.
	/// Roslyn will cache results per class and only reprocess changed classes.
	/// </summary>
	private static IEnumerable<PluginStepMetadata> TransformToMetadata(
		GeneratorSyntaxContext context,
		CancellationToken cancellationToken)
	{
		if (context.Node is not ClassDeclarationSyntax classDecl)
			return null;

		// Check cancellation
		if (cancellationToken.IsCancellationRequested)
			return null;

		// Use SemanticModel from context (provided by Roslyn, cached per syntax tree)
		var semanticModel = context.SemanticModel;

		// Check if inherits from Plugin
		if (!SyntaxHelper.InheritsFromPlugin(classDecl, semanticModel))
			return null;

		// Parse registrations (all heavy work here, in cacheable transform)
		var metadataList = RegistrationParser.ParsePluginClass(classDecl, semanticModel);
		if (!metadataList.Any())
			return null;

		// Group metadata by unique registration (EntityType + EventOperation + ExecutionStage)
		var groupedMetadata = metadataList.GroupBy(m => m.UniqueId);

		var results = new List<PluginStepMetadata>();

		foreach (var group in groupedMetadata)
		{
			// Check cancellation
			if (cancellationToken.IsCancellationRequested)
				return null;

			// Merge multiple registrations for the same entity/operation/stage
			var mergedMetadata = WrapperClassGenerator.MergeMetadata(group);

			if (mergedMetadata is null)
				continue;

			// Validate handler method signature
			HandlerMethodValidator.ValidateHandlerMethod(
				mergedMetadata,
				semanticModel.Compilation);

			// Include if:
			// - Has method reference (for ActionWrapper generation)
			// - OR has images with attributes (for image wrapper generation)
			// - OR has diagnostics to report
			if (!string.IsNullOrEmpty(mergedMetadata.HandlerMethodName) ||
				mergedMetadata.Images.Any(i => i.Attributes.Any()) ||
				mergedMetadata.Diagnostics?.Any() == true)
			{
				results.Add(mergedMetadata);
			}
		}

		return results;
	}

	/// <summary>
	/// Generates source code from metadata.
	/// This is called per metadata item, enabling true incrementality.
	/// </summary>
	private void GenerateSourceFromMetadata(
		PluginStepMetadata metadata,
		SourceProductionContext context)
	{
		// Report any collected diagnostics first
		// Note: We use Location.None because Location objects cannot be cached across
		// incremental compilations (they reference SyntaxTrees from the original compilation)
		if (metadata?.Diagnostics != null)
		{
			foreach (var diagnosticInfo in metadata.Diagnostics)
			{
				var diagnostic = Diagnostic.Create(
					diagnosticInfo.Descriptor,
					Location.None,
					diagnosticInfo.MessageArgs);
				context.ReportDiagnostic(diagnostic);
			}
		}

		// Generate code if we have a handler method reference (ActionWrapper always needed)
		if (string.IsNullOrEmpty(metadata?.HandlerMethodName))
			return;

		try
		{
			// Generate the wrapper classes
			var sourceCode = WrapperClassGenerator.GenerateWrapperClasses(metadata);

			if (sourceCode == null)
				return;

			// Generate unique hint name
			var hintName = WrapperClassGenerator.GenerateHintName(metadata);

			// Add the source to the compilation
			// Use SourceText.From() to ensure language-agnostic parsing (Roslyn will use compilation's ParseOptions)
			context.AddSource(hintName, SourceText.From(sourceCode, Encoding.UTF8));
		}
		catch (System.Exception ex)
		{
			// Report diagnostic error
			ReportGenerationError(context, metadata, ex);
		}
	}

	/// <summary>
	/// Reports a diagnostic for successful code generation
	/// </summary>
	private void ReportGenerationSuccess(
		SourceProductionContext context,
		PluginStepMetadata metadata)
	{
		var diagnostic = Diagnostic.Create(
			DiagnosticDescriptors.GenerationSuccess,
			Location.None,
			1, // wrapper class count
			metadata.RegistrationNamespace);

		// Uncomment to see generation info in build output
		context.ReportDiagnostic(diagnostic);
	}

	/// <summary>
	/// Reports a diagnostic error when code generation fails
	/// </summary>
	private void ReportGenerationError(
		SourceProductionContext context,
		PluginStepMetadata metadata,
		System.Exception exception)
	{
		var diagnostic = Diagnostic.Create(
			DiagnosticDescriptors.GenerationError,
			Location.None,
			exception.Message);

		context.ReportDiagnostic(diagnostic);
	}
}
