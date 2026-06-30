using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace XrmPluginCore.SourceGenerator.Helpers;

/// <summary>
/// Shared utilities for analyzing type-safe Custom API registrations
/// (<c>RegisterAPI&lt;TService&gt;(name, handlerMethodName)</c>) and their fluent
/// <c>AddRequestParameter</c>/<c>AddResponseProperty</c> chains. Mirrors <see cref="RegisterStepHelper"/>.
/// </summary>
internal static class RegisterApiHelper
{
	/// <summary>
	/// Checks if an invocation is a generic <c>RegisterAPI&lt;TService&gt;(...)</c> call and extracts the generic name.
	/// </summary>
	public static bool IsRegisterApiCall(InvocationExpressionSyntax invocation, out GenericNameSyntax genericName)
	{
		genericName = GetGenericName(invocation);
		return genericName != null && genericName.Identifier.Text == Constants.RegisterApiMethodName;
	}

	/// <summary>
	/// Gets the GenericNameSyntax from a RegisterAPI call (handles <c>this.RegisterAPI</c> and bare forms).
	/// </summary>
	public static GenericNameSyntax GetGenericName(InvocationExpressionSyntax invocation)
	{
		if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
			memberAccess.Name is GenericNameSyntax generic)
		{
			return generic;
		}

		if (invocation.Expression is GenericNameSyntax directGeneric)
		{
			return directGeneric;
		}

		return null;
	}

	/// <summary>
	/// Determines whether the invocation binds to the typed handler-name overload
	/// <c>RegisterAPI&lt;TService&gt;(string name, string handlerMethodName)</c> (vs the action overloads).
	/// </summary>
	public static bool IsTypedHandlerOverload(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
	{
		var method = ResolveMethod(invocation, semanticModel);
		return method != null
			&& method.Parameters.Length == 2
			&& method.Parameters[1].Type.SpecialType == SpecialType.System_String;
	}

	/// <summary>
	/// Resolves the API name (the first argument) as a compile-time constant string. Returns null when
	/// it is not a constant.
	/// </summary>
	public static string GetApiName(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
	{
		var arguments = invocation.ArgumentList.Arguments;
		if (arguments.Count < 1)
		{
			return null;
		}

		var constant = semanticModel.GetConstantValue(arguments[0].Expression);
		return constant.HasValue ? constant.Value as string : null;
	}

	/// <summary>
	/// Extracts the handler method name (second argument) from nameof()/string literal.
	/// </summary>
	public static string GetHandlerMethodName(InvocationExpressionSyntax invocation)
	{
		var arguments = invocation.ArgumentList.Arguments;
		return arguments.Count < 2 ? null : RegisterStepHelper.GetMethodName(arguments[1].Expression);
	}

	/// <summary>
	/// Resolves the service type (the single generic argument) of a RegisterAPI call.
	/// </summary>
	public static ITypeSymbol GetServiceType(GenericNameSyntax genericName, SemanticModel semanticModel)
	{
		if (genericName == null || genericName.TypeArgumentList.Arguments.Count < 1)
		{
			return null;
		}

		return semanticModel.GetTypeInfo(genericName.TypeArgumentList.Arguments[0]).Type;
	}

	/// <summary>
	/// Finds all AddRequestParameter/AddResponseProperty invocations chained to a RegisterAPI call,
	/// in source order.
	/// </summary>
	public static IEnumerable<InvocationExpressionSyntax> FindParameterInvocations(InvocationExpressionSyntax registerApiInvocation)
	{
		var parent = registerApiInvocation.Parent;
		while (parent != null)
		{
			if (parent is InvocationExpressionSyntax invocation &&
				invocation.Expression is MemberAccessExpressionSyntax memberAccess)
			{
				var methodName = memberAccess.Name.Identifier.Text;
				if (methodName == Constants.AddRequestParameterMethodName ||
					methodName == Constants.AddResponsePropertyMethodName)
				{
					yield return invocation;
				}
			}
			parent = parent.Parent;
		}
	}

	/// <summary>
	/// Counts the request parameters and response properties declared in the fluent chain.
	/// </summary>
	public static (bool hasRequest, bool hasResponse) CheckForParameters(InvocationExpressionSyntax registerApiInvocation)
	{
		var hasRequest = false;
		var hasResponse = false;

		foreach (var invocation in FindParameterInvocations(registerApiInvocation))
		{
			var methodName = ((MemberAccessExpressionSyntax)invocation.Expression).Name.Identifier.Text;
			if (methodName == Constants.AddRequestParameterMethodName)
			{
				hasRequest = true;
			}
			else if (methodName == Constants.AddResponsePropertyMethodName)
			{
				hasResponse = true;
			}
		}

		return (hasRequest, hasResponse);
	}

	private static IMethodSymbol ResolveMethod(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
	{
		var symbolInfo = semanticModel.GetSymbolInfo(invocation);
		return symbolInfo.Symbol as IMethodSymbol
			?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
	}
}
