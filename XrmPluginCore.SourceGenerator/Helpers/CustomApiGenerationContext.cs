using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using XrmPluginCore.SourceGenerator.Parsers;

namespace XrmPluginCore.SourceGenerator.Helpers;

/// <summary>
/// Resolves the generated-class names and namespace for a type-safe Custom API registration, shared
/// between the analyzers (to detect signature mismatches) and the code-fix providers (to emit the
/// correct handler signature).
/// </summary>
internal sealed class CustomApiGenerationContext
{
	private CustomApiGenerationContext(string pluginNamespace, string sanitizedApiName, bool hasRequest, bool hasResponse)
	{
		PluginNamespace = pluginNamespace;
		SanitizedApiName = sanitizedApiName;
		HasRequest = hasRequest;
		HasResponse = hasResponse;
	}

	public string PluginNamespace { get; }
	public string SanitizedApiName { get; }
	public bool HasRequest { get; }
	public bool HasResponse { get; }

	public string RequestClassName => $"{SanitizedApiName}{Constants.RequestClassSuffix}";
	public string ResponseClassName => $"{SanitizedApiName}{Constants.ResponseClassSuffix}";

	public string RequestTypeFullName => $"{PluginNamespace}.{RequestClassName}";
	public string ResponseTypeFullName => $"{PluginNamespace}.{ResponseClassName}";

	/// <summary>
	/// Builds the context from a RegisterAPI invocation, or returns null when the API name cannot be
	/// resolved as a compile-time constant.
	/// </summary>
	public static CustomApiGenerationContext TryCreate(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
	{
		var apiName = RegisterApiHelper.GetApiName(invocation, semanticModel);
		if (string.IsNullOrEmpty(apiName))
		{
			return null;
		}

		var classDeclaration = invocation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
		if (classDeclaration == null)
		{
			return null;
		}

		var (hasRequest, hasResponse) = RegisterApiHelper.CheckForParameters(invocation);

		return new CustomApiGenerationContext(
			classDeclaration.GetNamespace(),
			IdentifierHelper.Sanitize(apiName),
			hasRequest,
			hasResponse);
	}
}
