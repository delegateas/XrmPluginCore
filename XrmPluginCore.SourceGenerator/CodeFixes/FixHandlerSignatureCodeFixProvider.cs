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
		solution = await FixMethodDeclarationsAsync(solution, serviceTypeSymbol, methodName, hasPreImage, hasPostImage, imageNamespace, cancellationToken);

		return solution;
	}

	private static async Task<Solution> FixMethodDeclarationsAsync(
		Solution solution,
		INamedTypeSymbol serviceType,
		string methodName,
		bool hasPreImage,
		bool hasPostImage,
		string imageNamespace,
		CancellationToken cancellationToken)
	{
		// Find all method declarations with this name on the service type
		var methods = TypeHelper.GetAllMethodsIncludingInherited(serviceType, methodName);

		foreach (var method in methods)
		{
			foreach (var location in method.Locations)
			{
				if (!location.IsInSource)
				{
					continue;
				}

				var tree = location.SourceTree;
				if (tree == null)
				{
					continue;
				}

				var methodDocument = solution.GetDocument(tree);
				if (methodDocument == null)
				{
					continue;
				}

				var methodRoot = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
				var node = methodRoot.FindNode(location.SourceSpan);

				var methodDeclaration = node.AncestorsAndSelf()
					.OfType<MethodDeclarationSyntax>()
					.FirstOrDefault();

				if (methodDeclaration == null)
				{
					continue;
				}

				// Create new parameter list
				var newParameters = SyntaxFactoryHelper.CreateImageParameterList(hasPreImage, hasPostImage);
				var newMethodDeclaration = methodDeclaration.WithParameterList(newParameters);

				var newRoot = methodRoot.ReplaceNode(methodDeclaration, newMethodDeclaration);
				newRoot = SyntaxFactoryHelper.AddUsingDirectiveIfMissing(newRoot, imageNamespace);
				solution = solution.WithDocumentSyntaxRoot(methodDocument.Id, newRoot);

				// Re-fetch the tree since we modified the solution
				break; // Only fix the first declaration, we'll fix others on subsequent runs
			}
		}

		return solution;
	}
}
