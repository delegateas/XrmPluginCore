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
	/// For classes this walks the base type chain; for interfaces it also includes
	/// members inherited from base interfaces (which are exposed via AllInterfaces, not BaseType).
	/// </summary>
	public static IMethodSymbol[] GetAllMethodsIncludingInherited(ITypeSymbol type, string methodName)
	{
		var methods = new List<IMethodSymbol>();

		void AddMethods(ITypeSymbol from)
		{
			foreach (var member in from.GetMembers(methodName))
			{
				if (member is IMethodSymbol method)
				{
					methods.Add(method);
				}
			}
		}

		var currentType = type;
		while (currentType != null)
		{
			AddMethods(currentType);
			currentType = currentType.BaseType;
		}

		// Interfaces don't expose inherited members via BaseType; their base
		// interfaces (transitively) are available through AllInterfaces. We only
		// do this for interface inputs - for classes the implementing methods live
		// on the class/base chain, and walking AllInterfaces would surface
		// (unimplemented) interface declaration methods.
		if (type.TypeKind == TypeKind.Interface)
		{
			foreach (var inheritedInterface in type.AllInterfaces)
			{
				AddMethods(inheritedInterface);
			}
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
