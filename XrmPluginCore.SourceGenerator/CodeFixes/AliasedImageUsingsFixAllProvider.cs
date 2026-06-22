using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XrmPluginCore.SourceGenerator.Helpers;

namespace XrmPluginCore.SourceGenerator.CodeFixes;

/// <summary>
/// Shared <see cref="FixAllProvider"/> for the image handler code fixes. Unlike
/// <see cref="WellKnownFixAllProviders.BatchFixer"/> — which computes each diagnostic's fix
/// independently against the original document and merges text changes — this provider consolidates
/// every fixable diagnostic in a document into a single pass: it applies each target signature/method
/// edit with its own alias-qualified parameter list, then runs <em>one</em> always-aliased using
/// rewrite per affected document (adding every distinct aliased image using and requalifying every
/// reference once). The result is order-independent and convergent: fixing N registrations via FixAll
/// produces the same unambiguous output as fixing them one-by-one.
/// </summary>
internal sealed class AliasedImageUsingsFixAllProvider : FixAllProvider
{
	public static readonly AliasedImageUsingsFixAllProvider Instance = new();

	// User-facing label shown in the FixAll UI. The equivalence key is a stable identity token and is
	// not suitable as a title.
	private const string Title = "Fix all image handler signatures";

	private AliasedImageUsingsFixAllProvider()
	{
	}

	public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() => new[]
	{
		FixAllScope.Document,
		FixAllScope.Project,
		FixAllScope.Solution,
	};

	public override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
	{
		var diagnostics = await GetDiagnosticsAsync(fixAllContext).ConfigureAwait(false);
		if (diagnostics.IsDefaultOrEmpty)
		{
			return null;
		}

		var solution = fixAllContext.Solution;

		return CodeAction.Create(
			Title,
			c => FixAllAsync(solution, diagnostics, c),
			equivalenceKey: fixAllContext.CodeActionEquivalenceKey);
	}

	private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(FixAllContext context)
	{
		switch (context.Scope)
		{
			case FixAllScope.Document when context.Document != null:
				return await context.GetDocumentDiagnosticsAsync(context.Document).ConfigureAwait(false);

			case FixAllScope.Project:
				return await context.GetAllDiagnosticsAsync(context.Project).ConfigureAwait(false);

			case FixAllScope.Solution:
				var all = ImmutableArray.CreateBuilder<Diagnostic>();
				foreach (var project in context.Solution.Projects)
				{
					all.AddRange(await context.GetAllDiagnosticsAsync(project).ConfigureAwait(false));
				}

				return all.ToImmutable();

			default:
				return ImmutableArray<Diagnostic>.Empty;
		}
	}

	private static async Task<Solution> FixAllAsync(
		Solution solution,
		ImmutableArray<Diagnostic> diagnostics,
		CancellationToken cancellationToken)
	{
		// Accumulate every edit by the document it lands in, so each affected document gets a single
		// consolidated rewrite regardless of how many diagnostics target it.
		var batches = new Dictionary<DocumentId, DocumentBatch>();

		foreach (var diagnostic in diagnostics)
		{
			await AccumulateDiagnosticAsync(solution, diagnostic, batches, cancellationToken).ConfigureAwait(false);
		}

		foreach (var kvp in batches)
		{
			solution = await ApplyBatchAsync(solution, kvp.Key, kvp.Value, cancellationToken).ConfigureAwait(false);
		}

		return solution;
	}

	private static async Task AccumulateDiagnosticAsync(
		Solution solution,
		Diagnostic diagnostic,
		Dictionary<DocumentId, DocumentBatch> batches,
		CancellationToken cancellationToken)
	{
		var diagnosticTree = diagnostic.Location.SourceTree;
		if (diagnosticTree == null)
		{
			return;
		}

		var diagnosticDocument = solution.GetDocument(diagnosticTree);
		if (diagnosticDocument == null)
		{
			return;
		}

		if (!diagnostic.Properties.TryGetValue(Constants.PropertyMethodName, out var methodName) || methodName == null)
		{
			return;
		}

		diagnostic.Properties.TryGetValue(Constants.PropertyHasPreImage, out var hasPreImageStr);
		diagnostic.Properties.TryGetValue(Constants.PropertyHasPostImage, out var hasPostImageStr);
		diagnostic.Properties.TryGetValue(Constants.PropertyImageNamespace, out var imageNamespace);

		var hasPreImage = bool.TryParse(hasPreImageStr, out var pre) && pre;
		var hasPostImage = bool.TryParse(hasPostImageStr, out var post) && post;

		var semanticModel = await diagnosticDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		var diagnosticRoot = await diagnosticTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
		if (semanticModel == null || diagnosticRoot == null)
		{
			return;
		}

		var serviceType = RegisterStepHelper.ResolveServiceType(diagnosticRoot, semanticModel, diagnostic.Location.SourceSpan, cancellationToken);
		if (serviceType == null)
		{
			return;
		}

		if (diagnostic.Id == DiagnosticDescriptors.HandlerMethodNotFound.Id)
		{
			await AccumulateCreateMethodAsync(solution, serviceType, methodName, hasPreImage, hasPostImage, imageNamespace, batches, cancellationToken).ConfigureAwait(false);
		}
		else
		{
			await AccumulateSignatureFixAsync(diagnosticDocument, serviceType, methodName, hasPreImage, hasPostImage, imageNamespace, batches, cancellationToken).ConfigureAwait(false);
		}
	}

	private static async Task AccumulateCreateMethodAsync(
		Solution solution,
		INamedTypeSymbol serviceType,
		string methodName,
		bool hasPreImage,
		bool hasPostImage,
		string imageNamespace,
		Dictionary<DocumentId, DocumentBatch> batches,
		CancellationToken cancellationToken)
	{
		var interfaceDeclaration = await CreateHandlerMethodCodeFixProvider
			.FindInterfaceDeclarationAsync(solution, serviceType, cancellationToken)
			.ConfigureAwait(false);
		if (interfaceDeclaration == null)
		{
			return;
		}

		var interfaceDocument = solution.GetDocument(interfaceDeclaration.SyntaxTree);
		if (interfaceDocument == null)
		{
			return;
		}

		var batch = GetBatch(batches, interfaceDocument.Id);
		batch.Creations.Add(new MethodEdit(interfaceDeclaration.Identifier.Text, methodName, hasPreImage, hasPostImage, imageNamespace));
		batch.AddNamespace(imageNamespace);
	}

	private static async Task AccumulateSignatureFixAsync(
		Document diagnosticDocument,
		INamedTypeSymbol serviceType,
		string methodName,
		bool hasPreImage,
		bool hasPostImage,
		string imageNamespace,
		Dictionary<DocumentId, DocumentBatch> batches,
		CancellationToken cancellationToken)
	{
		var solution = diagnosticDocument.Project.Solution;

		var methods = new List<IMethodSymbol>(TypeHelper.GetAllMethodsIncludingInherited(serviceType, methodName));
		if (serviceType.TypeKind == TypeKind.Interface)
		{
			var compilation = await diagnosticDocument.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
			if (compilation != null)
			{
				methods.AddRange(TypeHelper.FindImplementingMethods(compilation, serviceType, methodName));
			}
		}

		foreach (var method in methods)
		{
			foreach (var location in method.Locations)
			{
				if (!location.IsInSource || location.SourceTree == null)
				{
					continue;
				}

				var document = solution.GetDocument(location.SourceTree);
				if (document == null)
				{
					continue;
				}

				var batch = GetBatch(batches, document.Id);
				batch.SignatureEdits.Add(new MethodEdit(method.ContainingType.Name, methodName, hasPreImage, hasPostImage, imageNamespace));
				batch.AddNamespace(imageNamespace);
			}
		}
	}

	private static async Task<Solution> ApplyBatchAsync(
		Solution solution,
		DocumentId documentId,
		DocumentBatch batch,
		CancellationToken cancellationToken)
	{
		var document = solution.GetDocument(documentId);
		if (document == null)
		{
			return solution;
		}

		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		if (root == null)
		{
			return solution;
		}

		// One consolidated always-aliased using rewrite FIRST, on the original tree, so the rewriter
		// resolves bare references against a semantic model whose nodes still match. All structural
		// edits below produce already alias-qualified parameters, so requalification skips them.
		var newRoot = SyntaxFactoryHelper.ApplyAliasedImageUsings(root, batch.Namespaces, semanticModel);

		// Fix existing handler signatures (interface + implementations) to their alias-qualified form.
		foreach (var edit in DistinctEdits(batch.SignatureEdits))
		{
			var newParameters = SyntaxFactoryHelper.CreateImageParameterList(edit.HasPreImage, edit.HasPostImage, GetAlias(edit.ImageNamespace));
			var current = newRoot.DescendantNodes().OfType<MethodDeclarationSyntax>()
				.FirstOrDefault(m => m.Identifier.Text == edit.MethodName &&
					m.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text == edit.TypeName);
			if (current != null)
			{
				newRoot = newRoot.ReplaceNode(current, current.WithParameterList(newParameters));
			}
		}

		// Create missing handler methods on their interfaces with the alias-qualified parameter list.
		foreach (var edit in DistinctEdits(batch.Creations))
		{
			var methodDeclaration = CreateHandlerMethodCodeFixProvider.CreateMethodDeclaration(
				edit.MethodName, edit.HasPreImage, edit.HasPostImage, GetAlias(edit.ImageNamespace));
			var interfaceDeclaration = newRoot.DescendantNodes().OfType<InterfaceDeclarationSyntax>()
				.FirstOrDefault(i => i.Identifier.Text == edit.TypeName);
			if (interfaceDeclaration != null)
			{
				newRoot = newRoot.ReplaceNode(interfaceDeclaration, interfaceDeclaration.AddMembers(methodDeclaration));
			}
		}

		return solution.WithDocumentSyntaxRoot(documentId, newRoot);
	}

	private static string GetAlias(string imageNamespace) =>
		string.IsNullOrEmpty(imageNamespace) ? null : SyntaxFactoryHelper.GetAliasForImageNamespace(imageNamespace);

	private static IEnumerable<MethodEdit> DistinctEdits(IEnumerable<MethodEdit> edits)
	{
		var seen = new HashSet<(string, string)>();
		foreach (var edit in edits)
		{
			if (seen.Add((edit.TypeName, edit.MethodName)))
			{
				yield return edit;
			}
		}
	}

	private static DocumentBatch GetBatch(Dictionary<DocumentId, DocumentBatch> batches, DocumentId documentId)
	{
		if (!batches.TryGetValue(documentId, out var batch))
		{
			batch = new DocumentBatch();
			batches[documentId] = batch;
		}

		return batch;
	}

	private sealed class DocumentBatch
	{
		public List<MethodEdit> SignatureEdits { get; } = new();

		public List<MethodEdit> Creations { get; } = new();

		public HashSet<string> Namespaces { get; } = new();

		public void AddNamespace(string imageNamespace)
		{
			if (!string.IsNullOrEmpty(imageNamespace))
			{
				Namespaces.Add(imageNamespace);
			}
		}
	}

	private readonly struct MethodEdit
	{
		public MethodEdit(string typeName, string methodName, bool hasPreImage, bool hasPostImage, string imageNamespace)
		{
			TypeName = typeName;
			MethodName = methodName;
			HasPreImage = hasPreImage;
			HasPostImage = hasPostImage;
			ImageNamespace = imageNamespace;
		}

		public string TypeName { get; }

		public string MethodName { get; }

		public bool HasPreImage { get; }

		public bool HasPostImage { get; }

		public string ImageNamespace { get; }
	}
}
