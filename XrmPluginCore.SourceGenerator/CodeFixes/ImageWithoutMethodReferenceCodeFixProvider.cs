using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XrmPluginCore.SourceGenerator.Helpers;

namespace XrmPluginCore.SourceGenerator.CodeFixes;

/// <summary>
/// Code fix provider that converts lambda invocation syntax (s => s.Method()) to nameof() expressions
/// when images are registered.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImageWithoutMethodReferenceCodeFixProvider)), Shared]
public class ImageWithoutMethodReferenceCodeFixProvider : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds =>
		ImmutableArray.Create("XPC4004");

	public sealed override FixAllProvider GetFixAllProvider() =>
		WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root == null)
		{
			return;
		}

		var diagnostic = context.Diagnostics.First();
		var diagnosticSpan = diagnostic.Location.SourceSpan;
		var lambdaNode = root.FindNode(diagnosticSpan);

		// Get service type and method name from diagnostic properties
		if (!diagnostic.Properties.TryGetValue("ServiceType", out var serviceType) ||
			!diagnostic.Properties.TryGetValue("MethodName", out var methodName))
		{
			return;
		}

		context.RegisterCodeFix(
			CodeAction.Create(
				title: $"Use nameof({serviceType}.{methodName})",
				createChangedDocument: c => ConvertToNameofAsync(context.Document, lambdaNode, serviceType, methodName, c),
				equivalenceKey: nameof(ImageWithoutMethodReferenceCodeFixProvider)),
			diagnostic);
	}

	private static async Task<Document> ConvertToNameofAsync(
		Document document,
		SyntaxNode diagnosticNode,
		string serviceType,
		string methodName,
		CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root == null)
		{
			return document;
		}

		// Find the lambda expression (either SimpleLambda or ParenthesizedLambda)
		var lambda = diagnosticNode.DescendantNodesAndSelf()
			.FirstOrDefault(n => n is SimpleLambdaExpressionSyntax || n is ParenthesizedLambdaExpressionSyntax)
			?? diagnosticNode;

		if (lambda is not (SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax))
		{
			return document;
		}

		// Build: nameof(ServiceType.MethodName)
		var nameofExpression = SyntaxFactoryHelper.CreateNameofExpression(serviceType, methodName)
			.WithTriviaFrom(lambda);

		var newRoot = root.ReplaceNode(lambda, nameofExpression);
		return document.WithSyntaxRoot(newRoot);
	}
}
