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
/// Code fix provider that creates a missing handler method on a service interface.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CreateHandlerMethodCodeFixProvider)), Shared]
public class CreateHandlerMethodCodeFixProvider : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds =>
		ImmutableArray.Create("XPC4002");

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
		if (!diagnostic.Properties.TryGetValue("ServiceType", out var serviceType) ||
			!diagnostic.Properties.TryGetValue("MethodName", out var methodName))
		{
			return;
		}

		diagnostic.Properties.TryGetValue("HasPreImage", out var hasPreImageStr);
		diagnostic.Properties.TryGetValue("HasPostImage", out var hasPostImageStr);

		var hasPreImage = bool.TryParse(hasPreImageStr, out var pre) && pre;
		var hasPostImage = bool.TryParse(hasPostImageStr, out var post) && post;

		// Build the title showing expected signature
		var signatureDescription = SyntaxFactoryHelper.BuildSignatureDescription(hasPreImage, hasPostImage);
		var title = $"Create method '{methodName}({signatureDescription})'";

		context.RegisterCodeFix(
			CodeAction.Create(
				title: title,
				createChangedSolution: c => CreateMethodAsync(context.Document, diagnostic, serviceType!, methodName!, hasPreImage, hasPostImage, c),
				equivalenceKey: nameof(CreateHandlerMethodCodeFixProvider)),
			diagnostic);
	}

	private static async Task<Solution> CreateMethodAsync(
		Document document,
		Diagnostic diagnostic,
		string serviceTypeName,
		string methodName,
		bool hasPreImage,
		bool hasPostImage,
		CancellationToken cancellationToken)
	{
		var solution = document.Project.Solution;

		// Get semantic model to find the service type declaration
		var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		if (semanticModel == null)
		{
			return solution;
		}

		// Find the RegisterStep call to get the service type
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

		// Find the interface declaration in the solution
		var interfaceDeclaration = await FindInterfaceDeclarationAsync(solution, serviceTypeSymbol, cancellationToken);
		if (interfaceDeclaration == null)
		{
			return solution;
		}

		// Create the method declaration
		var methodDeclaration = CreateMethodDeclaration(methodName, hasPreImage, hasPostImage);

		// Add the method to the interface
		var interfaceDocument = solution.GetDocument(interfaceDeclaration.SyntaxTree);
		if (interfaceDocument == null)
		{
			return solution;
		}

		var interfaceRoot = await interfaceDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (interfaceRoot == null)
		{
			return solution;
		}

		var newInterface = interfaceDeclaration.AddMembers(methodDeclaration);
		var newRoot = interfaceRoot.ReplaceNode(interfaceDeclaration, newInterface);

		return solution.WithDocumentSyntaxRoot(interfaceDocument.Id, newRoot);
	}

	private static MethodDeclarationSyntax CreateMethodDeclaration(string methodName, bool hasPreImage, bool hasPostImage)
	{
		return SyntaxFactory.MethodDeclaration(
				SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
				SyntaxFactory.Identifier(methodName))
			.WithParameterList(SyntaxFactoryHelper.CreateImageParameterList(hasPreImage, hasPostImage))
			.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
			.WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed, SyntaxFactory.ElasticTab)
			.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);
	}

	private static async Task<InterfaceDeclarationSyntax> FindInterfaceDeclarationAsync(
		Solution solution,
		INamedTypeSymbol typeSymbol,
		CancellationToken cancellationToken)
	{
		foreach (var location in typeSymbol.Locations)
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

			var document = solution.GetDocument(tree);
			if (document == null)
			{
				continue;
			}

			var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
			var node = root.FindNode(location.SourceSpan);

			var interfaceDeclaration = node.AncestorsAndSelf()
				.OfType<InterfaceDeclarationSyntax>()
				.FirstOrDefault();

			if (interfaceDeclaration != null)
			{
				return interfaceDeclaration;
			}
		}

		return null;
	}
}
