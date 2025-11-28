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

namespace XrmPluginCore.SourceGenerator.CodeFixes;

/// <summary>
/// Code fix provider that adds a parameterless constructor to plugin classes.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddParameterlessConstructorCodeFixProvider)), Shared]
public class AddParameterlessConstructorCodeFixProvider : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds =>
		ImmutableArray.Create(DiagnosticDescriptors.NoParameterlessConstructor.Id);

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
		var classDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
			.OfType<ClassDeclarationSyntax>()
			.FirstOrDefault();

		if (classDeclaration == null)
		{
			return;
		}

		context.RegisterCodeFix(
			CodeAction.Create(
				title: "Add parameterless constructor",
				createChangedDocument: c => AddParameterlessConstructorAsync(context.Document, classDeclaration, c),
				equivalenceKey: nameof(AddParameterlessConstructorCodeFixProvider)),
			diagnostic);
	}

	private static async Task<Document> AddParameterlessConstructorAsync(
		Document document,
		ClassDeclarationSyntax classDeclaration,
		CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root == null)
		{
			return document;
		}

		// Find the last constructor to insert after
		var constructors = classDeclaration.Members
			.OfType<ConstructorDeclarationSyntax>()
			.ToList();

		// Create: public ClassName() { }
		var newConstructor = SyntaxFactory.ConstructorDeclaration(classDeclaration.Identifier)
			.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
			.WithBody(SyntaxFactory.Block())
			.WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed, SyntaxFactory.ElasticTab)
			.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

		// Find where to insert: after the last constructor, or at the start of members
		ClassDeclarationSyntax newClassDeclaration;
		if (constructors.Count > 0)
		{
			var lastConstructor = constructors.Last();
			var insertIndex = classDeclaration.Members.IndexOf(lastConstructor) + 1;
			newClassDeclaration = classDeclaration.WithMembers(
				classDeclaration.Members.Insert(insertIndex, newConstructor));
		}
		else
		{
			// Insert at the beginning of members
			newClassDeclaration = classDeclaration.WithMembers(
				classDeclaration.Members.Insert(0, newConstructor));
		}

		var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
		return document.WithSyntaxRoot(newRoot);
	}
}
