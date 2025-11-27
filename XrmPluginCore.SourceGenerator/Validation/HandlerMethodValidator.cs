using Microsoft.CodeAnalysis;
using System.Linq;
using XrmPluginCore.SourceGenerator.Helpers;
using XrmPluginCore.SourceGenerator.Models;

namespace XrmPluginCore.SourceGenerator.Validation;

internal static class HandlerMethodValidator
{
	/// <summary>
	/// Validates handler method existence and signature.
	/// Sets HasValidationError on metadata if validation fails.
	/// Note: XPC4002 and XPC4003 diagnostics are handled by separate analyzers.
	/// </summary>
	public static void ValidateHandlerMethod(
		PluginStepMetadata metadata,
		Compilation compilation)
	{
		if (string.IsNullOrEmpty(metadata.HandlerMethodName) ||
			string.IsNullOrEmpty(metadata.ServiceTypeFullName))
			return;

		var serviceType = compilation.GetTypeByMetadataName(metadata.ServiceTypeFullName);
		if (serviceType is null)
			return;

		var methods = TypeHelper.GetAllMethodsIncludingInherited(serviceType, metadata.HandlerMethodName);
		if (!methods.Any())
		{
			// Method not found - abort generation for this registration
			// XPC4002 diagnostic is handled by HandlerMethodNotFoundAnalyzer
			metadata.HasValidationError = true;
			return;
		}

		var hasPreImage = metadata.Images.Any(i => i.ImageType == Constants.PreImageTypeName);
		var hasPostImage = metadata.Images.Any(i => i.ImageType == Constants.PostImageTypeName);

		var hasMatchingOverload = methods.Any(method => SignatureMatches(method, hasPreImage, hasPostImage));
		if (!hasMatchingOverload)
		{
			// Signature mismatch - abort generation for this registration
			// XPC4003 diagnostic is handled by HandlerSignatureMismatchAnalyzer
			metadata.HasValidationError = true;
		}
	}

	private static bool SignatureMatches(IMethodSymbol method, bool hasPreImage, bool hasPostImage)
	{
		var parameters = method.Parameters;
		var expectedParamCount = (hasPreImage ? 1 : 0) + (hasPostImage ? 1 : 0);

		if (parameters.Length != expectedParamCount)
			return false;

		var paramIndex = 0;

		if (hasPreImage)
		{
			if (paramIndex >= parameters.Length)
				return false;
			if (parameters[paramIndex].Type.Name != Constants.PreImageTypeName)
				return false;
			paramIndex++;
		}

		if (hasPostImage)
		{
			if (paramIndex >= parameters.Length)
				return false;
			if (parameters[paramIndex].Type.Name != Constants.PostImageTypeName)
				return false;
		}

		return true;
	}
}
