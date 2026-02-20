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
/// Code fix provider that fixes handler method signatures to match registered images.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FixHandlerSignatureCodeFixProvider)), Shared]
public class FixHandlerSignatureCodeFixProvider : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds =>
		ImmutableArray.Create(
			DiagnosticDescriptors.HandlerSignatureMismatch.Id,
			DiagnosticDescriptors.HandlerSignatureMismatchError.Id);

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

		// Get properties from diagnostic
		if (!diagnostic.Properties.TryGetValue(Constants.PropertyServiceType, out var serviceType) ||
			!diagnostic.Properties.TryGetValue(Constants.PropertyMethodName, out var methodName))
		{
			return;
		}

		diagnostic.Properties.TryGetValue(Constants.PropertyHasPreImage, out var hasPreImageStr);
		diagnostic.Properties.TryGetValue(Constants.PropertyHasPostImage, out var hasPostImageStr);
		diagnostic.Properties.TryGetValue(Constants.PropertyImageNamespace, out var imageNamespace);

		var hasPreImage = bool.TryParse(hasPreImageStr, out var pre) && pre;
		var hasPostImage = bool.TryParse(hasPostImageStr, out var post) && post;

		// Build the title showing expected signature
		var signatureDescription = SyntaxFactoryHelper.BuildSignatureDescription(hasPreImage, hasPostImage, includeParameterNames: true);
		var title = $"Fix signature to '{methodName}({signatureDescription})'";

		context.RegisterCodeFix(
			CodeAction.Create(
				title: title,
				createChangedSolution: c => FixSignatureAsync(context.Document, diagnostic, serviceType!, methodName!, hasPreImage, hasPostImage, imageNamespace, c),
				equivalenceKey: nameof(FixHandlerSignatureCodeFixProvider)),
			diagnostic);
	}

	private static async Task<Solution> FixSignatureAsync(
		Document document,
		Diagnostic diagnostic,
		string serviceTypeName,
		string methodName,
		bool hasPreImage,
		bool hasPostImage,
		string imageNamespace,
		CancellationToken cancellationToken)
	{
		var solution = document.Project.Solution;

		// Get semantic model to find the service type
		var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		if (semanticModel == null)
		{
			return solution;
		}

		// Find the RegisterStep call to get the service type symbol
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root == null)
		{
			return solution;
		}

		var diagnosticNode = root.FindNode(diagnostic.Location.SourceSpan);
		var registerStepInvocation = diagnosticNode.AncestorsAndSelf()
			.OfType<InvocationExpressionSyntax>()
			.FirstOrDefault(i => RegisterStepHelper.IsRegisterStepCall(i, out _));

		if (registerStepInvocation == null)
		{
			return solution;
		}

		// Get service type from generic arguments
		var genericName = RegisterStepHelper.GetGenericName(registerStepInvocation);
		if (genericName == null || genericName.TypeArgumentList.Arguments.Count < 2)
		{
			return solution;
		}

		var serviceTypeSyntax = genericName.TypeArgumentList.Arguments[1];
		var typeInfo = semanticModel.GetTypeInfo(serviceTypeSyntax, cancellationToken);
		var serviceTypeSymbol = typeInfo.Type as INamedTypeSymbol;

		if (serviceTypeSymbol == null)
		{
			return solution;
		}

		// Find the method declarations to fix (in interface and implementations)
		solution = await FixMethodDeclarationsAsync(solution, semanticModel.Compilation, serviceTypeSymbol, methodName, hasPreImage, hasPostImage, imageNamespace, cancellationToken);

		return solution;
	}

	private static async Task<Solution> FixMethodDeclarationsAsync(
		Solution solution,
		Compilation compilation,
		INamedTypeSymbol serviceType,
		string methodName,
		bool hasPreImage,
		bool hasPostImage,
		string imageNamespace,
		CancellationToken cancellationToken)
	{
		// Collect ALL methods to fix (interface + implementations)
		var allMethods = new List<IMethodSymbol>(TypeHelper.GetAllMethodsIncludingInherited(serviceType, methodName));

		if (serviceType.TypeKind == TypeKind.Interface)
		{
			var implMethods = TypeHelper.FindImplementingMethods(compilation, serviceType, methodName);
			allMethods.AddRange(implMethods);
		}

		// Group method locations by document (syntax tree)
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

		// Process each document
		foreach (var kvp in locationsByTree)
		{
			var tree = kvp.Key;
			var locations = kvp.Value;

			var methodDocument = solution.GetDocument(tree);
			if (methodDocument == null)
			{
				continue;
			}

			var methodRoot = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

			// Find all method declarations in this document
			var methodDeclarations = new List<MethodDeclarationSyntax>();
			foreach (var location in locations)
			{
				var node = methodRoot.FindNode(location.SourceSpan);
				var methodDecl = node.AncestorsAndSelf()
					.OfType<MethodDeclarationSyntax>()
					.FirstOrDefault();

				if (methodDecl != null && !methodDeclarations.Contains(methodDecl))
				{
					methodDeclarations.Add(methodDecl);
				}
			}

			if (methodDeclarations.Count == 0)
			{
				continue;
			}

			// Detect ambiguity
			var ambiguity = SyntaxFactoryHelper.DetectImageAmbiguity(methodRoot, imageNamespace);
			var newParameters = SyntaxFactoryHelper.CreateImageParameterList(hasPreImage, hasPostImage, ambiguity.needsAlias ? ambiguity.alias : null);

			// Build a set of (containingTypeName, methodName) pairs to replace
			var targets = new HashSet<(string typeName, string method)>();
			foreach (var methodDecl in methodDeclarations)
			{
				var containingType = methodDecl.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
				var typeName = containingType?.Identifier.Text;
				if (typeName != null)
				{
					targets.Add((typeName, methodDecl.Identifier.Text));
				}
			}

			// Replace all matching method declarations one at a time, re-finding after each
			SyntaxNode newRoot = methodRoot;
			foreach (var target in targets)
			{
				var current = newRoot.DescendantNodes().OfType<MethodDeclarationSyntax>()
					.FirstOrDefault(m => m.Identifier.Text == target.method &&
						m.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text == target.typeName);
				if (current != null)
				{
					newRoot = newRoot.ReplaceNode(current, current.WithParameterList(newParameters));
				}
			}

			// Handle usings
			if (ambiguity.needsAlias)
			{
				newRoot = SyntaxFactoryHelper.ConvertToAliasedUsingsAndQualifyRefs(newRoot, imageNamespace);
			}
			else
			{
				newRoot = SyntaxFactoryHelper.AddUsingDirectiveIfMissing(newRoot, imageNamespace);
			}

			solution = solution.WithDocumentSyntaxRoot(methodDocument.Id, newRoot);
		}

		return solution;
	}
}
