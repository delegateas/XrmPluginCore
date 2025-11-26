using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using XrmPluginCore.SourceGenerator.Models;

namespace XrmPluginCore.SourceGenerator.Validation;

internal static class HandlerMethodValidator
{
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

		var methods = GetAllMethodsIncludingInherited(serviceType, metadata.HandlerMethodName);
		if (!methods.Any())
		{
			metadata.Diagnostics.Add(new DiagnosticInfo
			{
				Descriptor = DiagnosticDescriptors.HandlerMethodNotFound,
				MessageArgs = [metadata.HandlerMethodName, metadata.ServiceTypeName]
			});
			return;
		}

		var hasPreImage = metadata.Images.Any(i => i.ImageType == Constants.PreImageTypeName);
		var hasPostImage = metadata.Images.Any(i => i.ImageType == Constants.PostImageTypeName);
		var expectedSignature = BuildExpectedSignature(hasPreImage, hasPostImage);

		var hasMatchingOverload = methods.Any(method => SignatureMatches(method, hasPreImage, hasPostImage));
		if (!hasMatchingOverload)
		{
			metadata.Diagnostics.Add(new DiagnosticInfo
			{
				Descriptor = DiagnosticDescriptors.HandlerSignatureMismatch,
				MessageArgs = [metadata.HandlerMethodName, expectedSignature]
			});
		}
	}

	private static IReadOnlyList<IMethodSymbol> GetAllMethodsIncludingInherited(ITypeSymbol type, string methodName)
	{
		var methods = new List<IMethodSymbol>();
		var currentType = type;
		while (currentType is not null)
		{
			foreach (var member in currentType.GetMembers(methodName))
			{
				if (member is IMethodSymbol method)
					methods.Add(method);
			}
			currentType = currentType.BaseType;
		}
		return methods;
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
			if (!IsImageParameter(parameters[paramIndex], Constants.PreImageTypeName))
				return false;
			paramIndex++;
		}

		if (hasPostImage)
		{
			if (paramIndex >= parameters.Length)
				return false;
			if (!IsImageParameter(parameters[paramIndex], Constants.PostImageTypeName))
				return false;
		}

		return true;
	}

	private static bool IsImageParameter(IParameterSymbol parameter, string expectedImageType)
	{
		return parameter.Type.Name == expectedImageType;
	}

	private static string BuildExpectedSignature(bool hasPreImage, bool hasPostImage)
	{
		var parts = new List<string>();
		if (hasPreImage)
			parts.Add(Constants.PreImageTypeName);
		if (hasPostImage)
			parts.Add(Constants.PostImageTypeName);

		if (parts.Count == 0)
			return "no parameters";

		return string.Join(", ", parts);
	}
}
