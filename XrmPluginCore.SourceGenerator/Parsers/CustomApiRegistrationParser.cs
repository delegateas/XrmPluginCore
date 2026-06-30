using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using XrmPluginCore.SourceGenerator.Helpers;
using XrmPluginCore.SourceGenerator.Models;

namespace XrmPluginCore.SourceGenerator.Parsers;

/// <summary>
/// Parses plugin class syntax to extract type-safe Custom API registration metadata.
/// </summary>
internal static class CustomApiRegistrationParser
{
	/// <summary>
	/// Parses a plugin class and extracts the type-safe Custom API registration, if any.
	/// Returns null when the class declares no <c>RegisterAPI&lt;TService&gt;(name, handlerMethodName)</c> call.
	/// </summary>
	public static CustomApiMetadata ParsePluginClass(
		ClassDeclarationSyntax classDeclaration,
		SemanticModel semanticModel)
	{
		var hasExplicitConstructors = classDeclaration.Members
			.OfType<ConstructorDeclarationSyntax>()
			.Any();

		var constructor = classDeclaration.Members
			.OfType<ConstructorDeclarationSyntax>()
			.FirstOrDefault(c => c.ParameterList.Parameters.Count == 0);

		// If the class has explicit constructors but no parameterless one, the registration pipeline
		// cannot run (mirrors RegistrationParser / XPC2001).
		if (constructor == null && hasExplicitConstructors)
		{
			return null;
		}

		if (constructor == null)
		{
			return null;
		}

		foreach (var invocation in FindRegisterApiInvocations(constructor))
		{
			if (!RegisterApiHelper.IsRegisterApiCall(invocation, out var genericName) ||
				!RegisterApiHelper.IsTypedHandlerOverload(invocation, semanticModel))
			{
				continue;
			}

			return ParseRegisterApiInvocation(invocation, genericName, semanticModel, classDeclaration);
		}

		return null;
	}

	private static CustomApiMetadata ParseRegisterApiInvocation(
		InvocationExpressionSyntax invocation,
		GenericNameSyntax genericName,
		SemanticModel semanticModel,
		ClassDeclarationSyntax classDeclaration)
	{
		var serviceType = RegisterApiHelper.GetServiceType(genericName, semanticModel);
		if (serviceType == null)
		{
			return null;
		}

		var apiName = RegisterApiHelper.GetApiName(invocation, semanticModel);
		if (string.IsNullOrEmpty(apiName))
		{
			// Cannot determine the API name as a constant => cannot name generated classes.
			return null;
		}

		var metadata = new CustomApiMetadata
		{
			ApiName = apiName,
			Namespace = classDeclaration.GetNamespace(),
			PluginClassName = classDeclaration.Identifier.Text,
			ServiceTypeName = serviceType.Name,
			ServiceTypeFullName = serviceType.ToDisplayString(),
			HandlerMethodName = RegisterApiHelper.GetHandlerMethodName(invocation, semanticModel),
		};

		foreach (var parameterCall in RegisterApiHelper.FindParameterInvocations(invocation))
		{
			var methodName = ((MemberAccessExpressionSyntax)parameterCall.Expression).Name.Identifier.Text;
			var isRequest = methodName == Constants.AddRequestParameterMethodName;

			var parameter = ParseParameterInvocation(parameterCall, semanticModel, isRequest);
			if (parameter == null)
			{
				continue;
			}

			if (isRequest)
			{
				metadata.RequestParameters.Add(parameter);
			}
			else
			{
				metadata.ResponseProperties.Add(parameter);
			}
		}

		return metadata;
	}

	private static CustomApiParameterMetadata ParseParameterInvocation(
		InvocationExpressionSyntax invocation,
		SemanticModel semanticModel,
		bool isRequest)
	{
		var boundArguments = ArgumentBinder.Bind(invocation, semanticModel);

		// uniqueName (first parameter) - must be a compile-time constant string.
		if (!boundArguments.TryGetValue(Constants.ParameterUniqueName, out var uniqueNameExpr))
		{
			return null;
		}

		var uniqueNameConstant = semanticModel.GetConstantValue(uniqueNameExpr);
		if (!uniqueNameConstant.HasValue || uniqueNameConstant.Value is not string uniqueName || string.IsNullOrEmpty(uniqueName))
		{
			return null;
		}

		// type (CustomApiParameterType member access, or constant integer fallback).
		var parameterType = boundArguments.TryGetValue(Constants.ParameterType, out var typeExpr)
			? GetParameterTypeName(typeExpr, semanticModel)
			: null;

		var isOptional = isRequest
			&& boundArguments.TryGetValue(Constants.ParameterIsOptional, out var optionalExpr)
			&& semanticModel.GetConstantValue(optionalExpr) is { HasValue: true, Value: true };

		var clrType = CustomApiParameterTypeMapper.GetClrType(parameterType);
		if (isOptional && CustomApiParameterTypeMapper.IsValueType(parameterType))
		{
			clrType += "?";
		}

		return new CustomApiParameterMetadata
		{
			UniqueName = uniqueName,
			PropertyName = IdentifierHelper.Sanitize(uniqueName),
			ParameterType = parameterType,
			ClrType = clrType,
			IsOptional = isOptional,
		};
	}

	/// <summary>
	/// Extracts the CustomApiParameterType member name from <c>CustomApiParameterType.String</c> or a
	/// constant integer.
	/// </summary>
	private static string GetParameterTypeName(ExpressionSyntax expression, SemanticModel semanticModel)
	{
		if (expression is MemberAccessExpressionSyntax memberAccess)
		{
			return memberAccess.Name.Identifier.Text;
		}

		var constant = semanticModel.GetConstantValue(expression);
		if (constant is { HasValue: true, Value: int value })
		{
			return CustomApiParameterTypeMapper.GetNameForValue(value);
		}

		return null;
	}

	private static IEnumerable<InvocationExpressionSyntax> FindRegisterApiInvocations(ConstructorDeclarationSyntax constructor)
	{
		IEnumerable<SyntaxNode> nodes = constructor.Body != null
			? constructor.Body.DescendantNodes()
			: constructor.ExpressionBody?.DescendantNodes() ?? Enumerable.Empty<SyntaxNode>();

		foreach (var invocation in nodes.OfType<InvocationExpressionSyntax>())
		{
			if (RegisterApiHelper.IsRegisterApiCall(invocation, out _))
			{
				yield return invocation;
			}
		}
	}
}
