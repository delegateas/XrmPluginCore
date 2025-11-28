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
/// Code fix provider that converts string literal handler method references to nameof() expressions.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferNameofCodeFixProvider)), Shared]
public class PreferNameofCodeFixProvider : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds =>
		ImmutableArray.Create(DiagnosticDescriptors.PreferNameofOverStringLiteral.Id);

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
		var stringLiteral = root.FindNode(diagnosticSpan);

		// Get service type and method name from diagnostic properties
		if (!diagnostic.Properties.TryGetValue("ServiceType", out var serviceType) ||
			!diagnostic.Properties.TryGetValue("MethodName", out var methodName))
		{
			return;
		}

		context.RegisterCodeFix(
			CodeAction.Create(
				title: $"Use nameof({serviceType}.{methodName})",
				createChangedDocument: c => ConvertToNameofAsync(context.Document, stringLiteral, serviceType, methodName, c),
				equivalenceKey: nameof(PreferNameofCodeFixProvider)),
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

		// Find the actual string literal expression
		var stringLiteral = diagnosticNode.DescendantNodesAndSelf()
			.OfType<LiteralExpressionSyntax>()
			.FirstOrDefault(l => l.IsKind(SyntaxKind.StringLiteralExpression))
			?? diagnosticNode as LiteralExpressionSyntax;

		if (stringLiteral == null)
		{
			return document;
		}

		// Build: nameof(ServiceType.MethodName)
		var nameofExpression = SyntaxFactoryHelper.CreateNameofExpression(serviceType, methodName)
			.WithTriviaFrom(stringLiteral);

		var newRoot = root.ReplaceNode(stringLiteral, nameofExpression);
		return document.WithSyntaxRoot(newRoot);
	}
}
