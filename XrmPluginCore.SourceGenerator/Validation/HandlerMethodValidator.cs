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
	/// Note: XPC4001 and XPC4002 diagnostics are handled by separate analyzers.
	/// IMPORTANT: We only block generation for XPC4001 (method not found), NOT for XPC4002 (signature mismatch).
	/// This is intentional - the generated types (PreImage/PostImage) must exist before the user can update
	/// their handler signature to use them. This prevents a chicken-and-egg problem.
	/// </summary>
	public static void ValidateHandlerMethod(
		PluginStepMetadata metadata,
		Compilation compilation)
	{
		if (string.IsNullOrEmpty(metadata.HandlerMethodName) ||
			string.IsNullOrEmpty(metadata.ServiceTypeFullName))
		{
			return;
		}

		var serviceType = compilation.GetTypeByMetadataName(metadata.ServiceTypeFullName);
		if (serviceType is null)
			return;

		var methods = TypeHelper.GetAllMethodsIncludingInherited(serviceType, metadata.HandlerMethodName);
		if (!methods.Any())
		{
			// Method not found - abort generation for this registration
			// XPC4001 diagnostic is handled by HandlerMethodNotFoundAnalyzer
			metadata.HasValidationError = true;
			return;
		}

		// NOTE: We intentionally do NOT set HasValidationError for signature mismatch (XPC4002).
		// The PreImage/PostImage types must be generated first so the user can reference them
		// in their handler method signature. The analyzer (XPC4002/XPC4003) will report if
		// the signature doesn't match after the types are available.
	}
}
