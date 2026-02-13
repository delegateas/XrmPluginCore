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
		var parameters = new List<ParameterSyntax>();

		if (hasPreImage)
		{
			parameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("preImage"))
				.WithType(SyntaxFactory.IdentifierName(Constants.PreImageTypeName)));
		}

		if (hasPostImage)
		{
			parameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("postImage"))
				.WithType(SyntaxFactory.IdentifierName(Constants.PostImageTypeName)));
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
}
