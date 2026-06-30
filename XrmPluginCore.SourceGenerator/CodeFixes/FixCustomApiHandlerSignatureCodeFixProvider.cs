using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XrmPluginCore.SourceGenerator.Helpers;

namespace XrmPluginCore.SourceGenerator.CodeFixes;

/// <summary>
/// Code fix that rewrites a Custom API handler method signature to match the declared request
/// parameters and response properties.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FixCustomApiHandlerSignatureCodeFixProvider)), Shared]
public class FixCustomApiHandlerSignatureCodeFixProvider : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds =>
		ImmutableArray.Create(
			DiagnosticDescriptors.CustomApiHandlerSignatureMismatch.Id,
			DiagnosticDescriptors.CustomApiHandlerSignatureMismatchError.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root == null)
		{
			return;
		}

		var diagnostic = context.Diagnostics.First();

		if (!diagnostic.Properties.TryGetValue(Constants.PropertyMethodName, out var methodName))
		{
			return;
		}

		diagnostic.Properties.TryGetValue(Constants.PropertyHasRequest, out var hasRequestStr);
		diagnostic.Properties.TryGetValue(Constants.PropertyHasResponse, out var hasResponseStr);
		diagnostic.Properties.TryGetValue(Constants.PropertyRequestTypeName, out var requestTypeName);
		diagnostic.Properties.TryGetValue(Constants.PropertyResponseTypeName, out var responseTypeName);

		var hasRequest = bool.TryParse(hasRequestStr, out var req) && req;
		var hasResponse = bool.TryParse(hasResponseStr, out var resp) && resp;

		var title = $"Fix signature to '{CustomApiHandlerSyntaxHelper.BuildSignatureTitle(methodName!, hasRequest, hasResponse, requestTypeName, responseTypeName)}'";

		context.RegisterCodeFix(
			CodeAction.Create(
				title: title,
				createChangedSolution: c => FixSignatureAsync(context.Document, diagnostic, methodName!, hasRequest, hasResponse, requestTypeName, responseTypeName, c),
				equivalenceKey: nameof(FixCustomApiHandlerSignatureCodeFixProvider)),
			diagnostic);
	}

	private static async Task<Solution> FixSignatureAsync(
		Document document,
		Diagnostic diagnostic,
		string methodName,
		bool hasRequest,
		bool hasResponse,
		string requestTypeName,
		string responseTypeName,
		CancellationToken cancellationToken)
	{
		var solution = document.Project.Solution;

		var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (semanticModel == null || root == null)
		{
			return solution;
		}

		var diagnosticNode = root.FindNode(diagnostic.Location.SourceSpan);
		var registerApiInvocation = diagnosticNode.AncestorsAndSelf()
			.OfType<InvocationExpressionSyntax>()
			.FirstOrDefault(i => RegisterApiHelper.IsRegisterApiCall(i, out _));
		if (registerApiInvocation == null)
		{
			return solution;
		}

		var genericName = RegisterApiHelper.GetGenericName(registerApiInvocation);
		var serviceType = RegisterApiHelper.GetServiceType(genericName, semanticModel) as INamedTypeSymbol;
		if (serviceType == null)
		{
			return solution;
		}

		var allMethods = new List<IMethodSymbol>(TypeHelper.GetAllMethodsIncludingInherited(serviceType, methodName));
		if (serviceType.TypeKind == TypeKind.Interface)
		{
			allMethods.AddRange(TypeHelper.FindImplementingMethods(semanticModel.Compilation, serviceType, methodName));
		}

		var newReturnType = CustomApiHandlerSyntaxHelper.CreateReturnType(hasResponse, responseTypeName);
		var newParameters = CustomApiHandlerSyntaxHelper.CreateParameterList(hasRequest, requestTypeName);

		// Group method locations by document.
		var locationsByTree = new Dictionary<SyntaxTree, List<Location>>();
		foreach (var method in allMethods)
		{
			foreach (var location in method.Locations)
			{
				if (!location.IsInSource || location.SourceTree == null)
				{
					continue;
				}

				if (!locationsByTree.TryGetValue(location.SourceTree, out var list))
				{
					list = new List<Location>();
					locationsByTree[location.SourceTree] = list;
				}

				list.Add(location);
			}
		}

		foreach (var kvp in locationsByTree)
		{
			var methodDocument = solution.GetDocument(kvp.Key);
			if (methodDocument == null)
			{
				continue;
			}

			var methodRoot = await kvp.Key.GetRootAsync(cancellationToken).ConfigureAwait(false);

			var targets = new HashSet<(string typeName, string method)>();
			foreach (var location in kvp.Value)
			{
				var node = methodRoot.FindNode(location.SourceSpan);
				var methodDecl = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
				var containingType = methodDecl?.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
				if (methodDecl != null && containingType != null)
				{
					targets.Add((containingType.Identifier.Text, methodDecl.Identifier.Text));
				}
			}

			var newRoot = methodRoot;
			foreach (var target in targets)
			{
				var current = newRoot.DescendantNodes().OfType<MethodDeclarationSyntax>()
					.FirstOrDefault(m => m.Identifier.Text == target.method &&
						m.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text == target.typeName);
				if (current != null)
				{
					var updated = current
						.WithParameterList(newParameters)
						.WithReturnType(newReturnType.WithTrailingTrivia(SyntaxFactory.Space));
					newRoot = newRoot.ReplaceNode(current, updated);
				}
			}

			solution = solution.WithDocumentSyntaxRoot(methodDocument.Id, newRoot);
		}

		return solution;
	}
}
