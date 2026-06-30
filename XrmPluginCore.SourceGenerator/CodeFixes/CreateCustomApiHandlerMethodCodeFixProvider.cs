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
/// Code fix that creates a missing Custom API handler method on the service type with the signature
/// matching the declared request parameters and response properties.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CreateCustomApiHandlerMethodCodeFixProvider)), Shared]
public class CreateCustomApiHandlerMethodCodeFixProvider : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds =>
		ImmutableArray.Create(DiagnosticDescriptors.CustomApiHandlerMethodNotFound.Id);

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

		var title = $"Create method '{CustomApiHandlerSyntaxHelper.BuildSignatureTitle(methodName!, hasRequest, hasResponse, requestTypeName, responseTypeName)}'";

		context.RegisterCodeFix(
			CodeAction.Create(
				title: title,
				createChangedSolution: c => CreateMethodAsync(context.Document, diagnostic, methodName!, hasRequest, hasResponse, requestTypeName, responseTypeName, c),
				equivalenceKey: nameof(CreateCustomApiHandlerMethodCodeFixProvider)),
			diagnostic);
	}

	private static async Task<Solution> CreateMethodAsync(
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

		var typeDeclaration = await FindTypeDeclarationAsync(solution, serviceType, cancellationToken);
		if (typeDeclaration == null)
		{
			return solution;
		}

		var typeDocument = solution.GetDocument(typeDeclaration.SyntaxTree);
		if (typeDocument == null)
		{
			return solution;
		}

		var typeRoot = await typeDeclaration.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

		var isInterface = typeDeclaration is InterfaceDeclarationSyntax;
		var methodDeclaration = CreateMethodDeclaration(methodName, hasRequest, hasResponse, requestTypeName, responseTypeName, isInterface);

		var newRoot = typeRoot.ReplaceNode(typeDeclaration, typeDeclaration.AddMembers(methodDeclaration));
		return solution.WithDocumentSyntaxRoot(typeDocument.Id, newRoot);
	}

	private static MethodDeclarationSyntax CreateMethodDeclaration(
		string methodName,
		bool hasRequest,
		bool hasResponse,
		string requestTypeName,
		string responseTypeName,
		bool isInterface)
	{
		var method = SyntaxFactory.MethodDeclaration(
				CustomApiHandlerSyntaxHelper.CreateReturnType(hasResponse, responseTypeName),
				SyntaxFactory.Identifier(methodName))
			.WithParameterList(CustomApiHandlerSyntaxHelper.CreateParameterList(hasRequest, requestTypeName));

		if (isInterface)
		{
			method = method
				.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
		}
		else
		{
			method = method
				.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
				.WithBody(SyntaxFactory.Block(
					SyntaxFactory.ThrowStatement(
						SyntaxFactory.ObjectCreationExpression(
							SyntaxFactory.ParseTypeName("System.NotImplementedException"))
						.WithArgumentList(SyntaxFactory.ArgumentList()))));
		}

		return method
			.WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed, SyntaxFactory.ElasticTab)
			.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);
	}

	private static async Task<TypeDeclarationSyntax> FindTypeDeclarationAsync(
		Solution solution,
		INamedTypeSymbol typeSymbol,
		CancellationToken cancellationToken)
	{
		foreach (var location in typeSymbol.Locations)
		{
			if (!location.IsInSource || location.SourceTree == null)
			{
				continue;
			}

			var root = await location.SourceTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
			var node = root.FindNode(location.SourceSpan);

			var typeDeclaration = node.AncestorsAndSelf()
				.OfType<TypeDeclarationSyntax>()
				.FirstOrDefault(t => t is ClassDeclarationSyntax || t is InterfaceDeclarationSyntax);

			if (typeDeclaration != null)
			{
				return typeDeclaration;
			}
		}

		return null;
	}
}
