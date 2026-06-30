using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace XrmPluginCore.SourceGenerator.Helpers;

/// <summary>
/// Binds invocation argument expressions to their parameter names, honoring both positional and named
/// arguments. Callers can legally reorder arguments with <c>name:</c> syntax (e.g.
/// <c>RegisterAPI&lt;T&gt;(handlerMethodName: ..., name: ...)</c>), so analyzers and parsers must resolve
/// arguments by parameter name rather than by ordinal position.
/// </summary>
internal static class ArgumentBinder
{
	/// <summary>
	/// Returns a map of parameter name to the argument expression supplied for it. Parameters with no
	/// supplied argument (defaults omitted at the call site) are absent from the map.
	/// </summary>
	public static IReadOnlyDictionary<string, ExpressionSyntax> Bind(
		InvocationExpressionSyntax invocation,
		SemanticModel semanticModel)
	{
		var result = new Dictionary<string, ExpressionSyntax>();

		var method = ResolveMethod(invocation, semanticModel);
		if (method == null)
		{
			return result;
		}

		var arguments = invocation.ArgumentList.Arguments;
		for (var i = 0; i < arguments.Count; i++)
		{
			var argument = arguments[i];

			// A named argument maps by its explicit name; a positional argument at list index i maps to
			// parameter i (the language requires positional arguments to stay in their natural position,
			// so the index is always correct for compilable code).
			IParameterSymbol parameter;
			if (argument.NameColon != null)
			{
				var name = argument.NameColon.Name.Identifier.Text;
				parameter = method.Parameters.FirstOrDefault(p => p.Name == name);
			}
			else
			{
				parameter = i < method.Parameters.Length ? method.Parameters[i] : null;
			}

			if (parameter != null)
			{
				result[parameter.Name] = argument.Expression;
			}
		}

		return result;
	}

	/// <summary>
	/// Returns the argument expression bound to <paramref name="parameterName"/>, or null when the
	/// parameter is not supplied or the call cannot be resolved.
	/// </summary>
	public static ExpressionSyntax GetArgument(
		InvocationExpressionSyntax invocation,
		SemanticModel semanticModel,
		string parameterName)
		=> Bind(invocation, semanticModel).TryGetValue(parameterName, out var expression) ? expression : null;

	private static IMethodSymbol ResolveMethod(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
	{
		var symbolInfo = semanticModel.GetSymbolInfo(invocation);
		return symbolInfo.Symbol as IMethodSymbol
			?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
	}
}
