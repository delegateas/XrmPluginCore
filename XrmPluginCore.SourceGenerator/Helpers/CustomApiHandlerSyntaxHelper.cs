using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XrmPluginCore.SourceGenerator.Helpers;

/// <summary>
/// Builds the syntax pieces for a Custom API handler method
/// (<c>{Response} {Method}({Request} request)</c>, adapting to the presence of request/response).
/// </summary>
internal static class CustomApiHandlerSyntaxHelper
{
	public const string RequestParameterName = "request";

	public static TypeSyntax CreateReturnType(bool hasResponse, string responseTypeName)
	{
		return hasResponse && !string.IsNullOrEmpty(responseTypeName)
			? SyntaxFactory.ParseTypeName(responseTypeName)
			: SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
	}

	public static ParameterListSyntax CreateParameterList(bool hasRequest, string requestTypeName)
	{
		if (!hasRequest || string.IsNullOrEmpty(requestTypeName))
		{
			return SyntaxFactory.ParameterList();
		}

		var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(RequestParameterName))
			.WithType(SyntaxFactory.ParseTypeName(requestTypeName));

		return SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(parameter));
	}

	/// <summary>
	/// Builds a one-line signature description (e.g. "FooResponse Handle(FooRequest request)") used in
	/// code-fix titles. Uses the supplied type names verbatim.
	/// </summary>
	public static string BuildSignatureTitle(string methodName, bool hasRequest, bool hasResponse, string requestTypeName, string responseTypeName)
	{
		var returnType = hasResponse && !string.IsNullOrEmpty(responseTypeName) ? Short(responseTypeName) : "void";
		var parameter = hasRequest && !string.IsNullOrEmpty(requestTypeName) ? $"{Short(requestTypeName)} {RequestParameterName}" : string.Empty;
		return $"{returnType} {methodName}({parameter})";
	}

	private static string Short(string typeName)
	{
		var lastDot = typeName.LastIndexOf('.');
		return lastDot >= 0 ? typeName.Substring(lastDot + 1) : typeName;
	}
}
