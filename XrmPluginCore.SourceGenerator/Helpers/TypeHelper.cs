using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

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

	/// <summary>
	/// Finds all methods that implement a given interface method across the compilation.
	/// </summary>
	public static IMethodSymbol[] FindImplementingMethods(Compilation compilation, INamedTypeSymbol interfaceType, string methodName)
	{
		var results = new List<IMethodSymbol>();

		foreach (var type in GetAllNamedTypes(compilation.GlobalNamespace))
		{
			if (type.TypeKind == TypeKind.Interface)
			{
				continue;
			}

			if (!type.AllInterfaces.Contains(interfaceType))
			{
				continue;
			}

			var methods = GetAllMethodsIncludingInherited(type, methodName);
			results.AddRange(methods);
		}

		return results.ToArray();
	}

	private static IEnumerable<INamedTypeSymbol> GetAllNamedTypes(INamespaceSymbol ns)
	{
		foreach (var type in ns.GetTypeMembers())
		{
			yield return type;
			foreach (var nested in GetNestedTypes(type))
			{
				yield return nested;
			}
		}

		foreach (var childNs in ns.GetNamespaceMembers())
		{
			foreach (var type in GetAllNamedTypes(childNs))
			{
				yield return type;
			}
		}
	}

	private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
	{
		foreach (var nested in type.GetTypeMembers())
		{
			yield return nested;
			foreach (var deepNested in GetNestedTypes(nested))
			{
				yield return deepNested;
			}
		}
	}
}
