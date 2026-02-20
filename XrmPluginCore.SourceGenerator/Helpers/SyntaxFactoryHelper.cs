using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace XrmPluginCore.SourceGenerator.Helpers;

/// <summary>
/// Shared utilities for syntax generation.
/// </summary>
internal static class SyntaxFactoryHelper
{
	/// <summary>
	/// Creates a nameof(ServiceType.MethodName) expression.
	/// </summary>
	public static InvocationExpressionSyntax CreateNameofExpression(string serviceType, string methodName)
	{
		return SyntaxFactory.InvocationExpression(
			SyntaxFactory.IdentifierName("nameof"),
			SyntaxFactory.ArgumentList(
				SyntaxFactory.SingletonSeparatedList(
					SyntaxFactory.Argument(
						SyntaxFactory.MemberAccessExpression(
							SyntaxKind.SimpleMemberAccessExpression,
							SyntaxFactory.IdentifierName(serviceType),
							SyntaxFactory.IdentifierName(methodName))))));
	}

	/// <summary>
	/// Creates a parameter list for PreImage/PostImage parameters.
	/// </summary>
	public static ParameterListSyntax CreateImageParameterList(bool hasPreImage, bool hasPostImage)
	{
		return CreateImageParameterList(hasPreImage, hasPostImage, qualifier: null);
	}

	/// <summary>
	/// Creates a parameter list for PreImage/PostImage parameters with optional qualifier.
	/// </summary>
	public static ParameterListSyntax CreateImageParameterList(bool hasPreImage, bool hasPostImage, string qualifier)
	{
		var parameters = new List<ParameterSyntax>();

		if (hasPreImage)
		{
			parameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("preImage"))
				.WithType(CreateTypeName(Constants.PreImageTypeName, qualifier)));
		}

		if (hasPostImage)
		{
			parameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("postImage"))
				.WithType(CreateTypeName(Constants.PostImageTypeName, qualifier)));
		}

		return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
	}

	/// <summary>
	/// Builds a signature description string for PreImage/PostImage parameters.
	/// </summary>
	/// <param name="hasPreImage">Whether PreImage is included.</param>
	/// <param name="hasPostImage">Whether PostImage is included.</param>
	/// <param name="includeParameterNames">If true, includes parameter names (e.g., "PreImage preImage"); if false, just type names.</param>
	public static string BuildSignatureDescription(bool hasPreImage, bool hasPostImage, bool includeParameterNames = false)
	{
		if (!hasPreImage && !hasPostImage)
		{
			return "";
		}

		var parts = new List<string>();
		if (hasPreImage)
		{
			parts.Add(includeParameterNames ? "PreImage preImage" : "PreImage");
		}

		if (hasPostImage)
		{
			parts.Add(includeParameterNames ? "PostImage postImage" : "PostImage");
		}

		return string.Join(", ", parts);
	}

	/// <summary>
	/// Adds a using directive to the compilation unit if it doesn't already exist.
	/// </summary>
	public static SyntaxNode AddUsingDirectiveIfMissing(SyntaxNode root, string namespaceName)
	{
		if (string.IsNullOrEmpty(namespaceName))
		{
			return root;
		}

		if (root is not CompilationUnitSyntax compilationUnit)
		{
			return root;
		}

		var alreadyExists = compilationUnit.Usings.Any(u => u.Name?.ToString() == namespaceName);
		if (alreadyExists)
		{
			return root;
		}

		var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
			.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

		return compilationUnit.AddUsings(usingDirective);
	}

	/// <summary>
	/// Detects whether adding the given image namespace would create ambiguity with existing usings.
	/// </summary>
	/// <returns>(needsAlias, alias) where alias is the last segment of the namespace.</returns>
	public static (bool needsAlias, string alias) DetectImageAmbiguity(SyntaxNode root, string imageNamespace)
	{
		if (string.IsNullOrEmpty(imageNamespace) || root is not CompilationUnitSyntax compilationUnit)
		{
			return (false, null);
		}

		foreach (var usingDirective in compilationUnit.Usings)
		{
			// Skip aliased usings
			if (usingDirective.Alias != null)
			{
				continue;
			}

			var existingNs = usingDirective.Name?.ToString();
			if (existingNs == null)
			{
				continue;
			}

			if (existingNs != imageNamespace && IsImageRegistrationNamespace(existingNs))
			{
				return (true, GetLastNamespaceSegment(imageNamespace));
			}
		}

		return (false, null);
	}

	/// <summary>
	/// Converts existing plain image usings to aliased form, qualifies all bare PreImage/PostImage
	/// type references, and adds the new aliased using.
	/// </summary>
	public static SyntaxNode ConvertToAliasedUsingsAndQualifyRefs(SyntaxNode root, string newImageNamespace)
	{
		if (root is not CompilationUnitSyntax compilationUnit)
		{
			return root;
		}

		// Build map of existing plain image namespace usings → alias
		var plainNamespaceToAlias = new Dictionary<string, string>();
		foreach (var usingDirective in compilationUnit.Usings)
		{
			if (usingDirective.Alias != null)
			{
				continue;
			}

			var ns = usingDirective.Name?.ToString();
			if (ns != null && IsImageRegistrationNamespace(ns))
			{
				plainNamespaceToAlias[ns] = GetLastNamespaceSegment(ns);
			}
		}

		// Also include the new namespace
		if (!string.IsNullOrEmpty(newImageNamespace))
		{
			plainNamespaceToAlias[newImageNamespace] = GetLastNamespaceSegment(newImageNamespace);
		}

		// Rewrite the tree
		var rewriter = new ImageAmbiguityRewriter(plainNamespaceToAlias);
		var newRoot = rewriter.Visit(root);

		// Add the new aliased using if not already present
		if (newRoot is CompilationUnitSyntax newCompUnit && !string.IsNullOrEmpty(newImageNamespace))
		{
			var newAlias = GetLastNamespaceSegment(newImageNamespace);
			var alreadyExists = newCompUnit.Usings.Any(u =>
				u.Alias?.Name.ToString() == newAlias ||
				u.Name?.ToString() == newImageNamespace);

			if (!alreadyExists)
			{
				var aliasedUsing = SyntaxFactory.UsingDirective(
						SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(newAlias)),
						SyntaxFactory.ParseName(newImageNamespace))
					.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

				newRoot = newCompUnit.AddUsings(aliasedUsing);
			}
		}

		return newRoot;
	}

	private static bool IsImageRegistrationNamespace(string ns)
	{
		return ns.Contains(".PluginRegistrations.");
	}

	private static string GetLastNamespaceSegment(string ns)
	{
		var lastDot = ns.LastIndexOf('.');
		return lastDot >= 0 ? ns.Substring(lastDot + 1) : ns;
	}

	private static TypeSyntax CreateTypeName(string typeName, string qualifier)
	{
		if (qualifier == null)
		{
			return SyntaxFactory.IdentifierName(typeName);
		}

		return SyntaxFactory.QualifiedName(
			SyntaxFactory.IdentifierName(qualifier),
			SyntaxFactory.IdentifierName(typeName));
	}

	/// <summary>
	/// Rewrites a syntax tree to convert plain image usings to aliased form
	/// and qualify bare PreImage/PostImage references.
	/// </summary>
	private sealed class ImageAmbiguityRewriter : CSharpSyntaxRewriter
	{
		private readonly Dictionary<string, string> _plainNamespaceToAlias;

		// Reverse map: for each bare type name, which alias qualifies it
		// Built from existing usings only (not the new one being added)
		private readonly Dictionary<string, string> _typeToExistingAlias;

		public ImageAmbiguityRewriter(Dictionary<string, string> plainNamespaceToAlias)
		{
			_plainNamespaceToAlias = plainNamespaceToAlias;

			// Build type-to-alias map for qualifying existing bare references
			_typeToExistingAlias = new Dictionary<string, string>();
		}

		public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
		{
			if (node.Alias != null)
			{
				return base.VisitUsingDirective(node);
			}

			var ns = node.Name?.ToString();
			if (ns != null && _plainNamespaceToAlias.TryGetValue(ns, out var alias))
			{
				// Track which alias maps to which namespace for qualifying references
				_typeToExistingAlias[ns] = alias;

				// Convert to aliased using
				return SyntaxFactory.UsingDirective(
						SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(alias)),
						node.Name!)
					.WithLeadingTrivia(node.GetLeadingTrivia())
					.WithTrailingTrivia(node.GetTrailingTrivia());
			}

			return base.VisitUsingDirective(node);
		}

		public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
		{
			var name = node.Identifier.Text;

			// Only qualify PreImage and PostImage
			if (name != Constants.PreImageTypeName && name != Constants.PostImageTypeName)
			{
				return base.VisitIdentifierName(node);
			}

			// Exclusion: already qualified (parent is QualifiedNameSyntax and we are the right side)
			if (node.Parent is QualifiedNameSyntax qualifiedParent && qualifiedParent.Right == node)
			{
				return base.VisitIdentifierName(node);
			}

			// Exclusion: member access name (e.g., obj.PreImage) — not a type reference
			if (node.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == node)
			{
				return base.VisitIdentifierName(node);
			}

			// Exclusion: alias name in using directive
			if (node.Parent is NameEqualsSyntax)
			{
				return base.VisitIdentifierName(node);
			}

			// Find the alias for this type reference based on the existing usings that were converted
			// We need to figure out which alias applies to this bare reference.
			// The bare reference was valid under the old plain using, so find which converted namespace it belonged to.
			string matchingAlias = null;
			foreach (var kvp in _typeToExistingAlias)
			{
				matchingAlias = kvp.Value;
				break; // There should be exactly one existing plain image using at this point
			}

			if (matchingAlias == null)
			{
				return base.VisitIdentifierName(node);
			}

			// Replace bare identifier with qualified name
			var qualifiedName = SyntaxFactory.QualifiedName(
				SyntaxFactory.IdentifierName(matchingAlias),
				SyntaxFactory.IdentifierName(name));

			return qualifiedName
				.WithLeadingTrivia(node.GetLeadingTrivia())
				.WithTrailingTrivia(node.GetTrailingTrivia());
		}
	}
}
