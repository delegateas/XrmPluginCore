using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace XrmPluginCore.SourceGenerator.Helpers;

/// <summary>
/// Shared utilities for type and symbol operations.
/// </summary>
internal static class TypeHelper
{
	/// <summary>
	/// Gets all methods with the specified name, including inherited methods.
	/// </summary>
	public static IMethodSymbol[] GetAllMethodsIncludingInherited(ITypeSymbol type, string methodName)
	{
		var methods = new List<IMethodSymbol>();
		var currentType = type;
		while (currentType != null)
		{
			foreach (var member in currentType.GetMembers(methodName))
			{
				if (member is IMethodSymbol method)
				{
					methods.Add(method);
				}
			}

			currentType = currentType.BaseType;
		}

		return methods.ToArray();
	}
}
