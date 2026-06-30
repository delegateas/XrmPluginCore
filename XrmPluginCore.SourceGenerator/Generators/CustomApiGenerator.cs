using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;
using System.Threading;
using XrmPluginCore.SourceGenerator.CodeGeneration;
using XrmPluginCore.SourceGenerator.Helpers;
using XrmPluginCore.SourceGenerator.Models;
using XrmPluginCore.SourceGenerator.Parsers;

namespace XrmPluginCore.SourceGenerator.Generators;

/// <summary>
/// Incremental source generator that creates type-safe Request/Response/ActionWrapper classes for
/// Custom API registrations declared with <c>RegisterAPI&lt;TService&gt;(name, handlerMethodName)</c>.
/// </summary>
[Generator]
public class CustomApiGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var metadata = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: static (node, _) => IsCandidateClass(node),
				transform: static (ctx, ct) => TransformToMetadata(ctx, ct))
			.Where(static m => m is not null);

		context.RegisterSourceOutput(metadata, (spc, m) => GenerateSourceFromMetadata(m, spc));
	}

	private static bool IsCandidateClass(SyntaxNode node)
		=> node is ClassDeclarationSyntax classDecl &&
			classDecl.Members.OfType<ConstructorDeclarationSyntax>().Any();

	private static CustomApiMetadata TransformToMetadata(GeneratorSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.Node is not ClassDeclarationSyntax classDecl)
		{
			return null;
		}

		if (cancellationToken.IsCancellationRequested)
		{
			return null;
		}

		var semanticModel = context.SemanticModel;
		if (!SyntaxHelper.InheritsFromPlugin(classDecl, semanticModel))
		{
			return null;
		}

		var metadata = CustomApiRegistrationParser.ParsePluginClass(classDecl, semanticModel);
		if (metadata == null || string.IsNullOrEmpty(metadata.HandlerMethodName))
		{
			return null;
		}

		metadata.NullableAnnotationsEnabled = NullableHelper.AnnotationsEnabled(semanticModel.Compilation);
		return metadata;
	}

	private void GenerateSourceFromMetadata(CustomApiMetadata metadata, SourceProductionContext context)
	{
		if (metadata?.Diagnostics != null)
		{
			foreach (var diagnosticInfo in metadata.Diagnostics)
			{
				context.ReportDiagnostic(Diagnostic.Create(diagnosticInfo.Descriptor, Location.None, diagnosticInfo.MessageArgs));
			}
		}

		if (string.IsNullOrEmpty(metadata?.HandlerMethodName))
		{
			return;
		}

		try
		{
			var sourceCode = CustomApiClassGenerator.GenerateCustomApiClasses(metadata);
			if (sourceCode == null)
			{
				return;
			}

			var hintName = CustomApiClassGenerator.GenerateHintName(metadata);
			context.AddSource(hintName, SourceText.From(sourceCode, Encoding.UTF8));
		}
		catch (System.Exception ex)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				DiagnosticDescriptors.GenerationError,
				Location.None,
				$"{ex.Message} | StackTrace: {ex.StackTrace}"));
		}
	}
}
