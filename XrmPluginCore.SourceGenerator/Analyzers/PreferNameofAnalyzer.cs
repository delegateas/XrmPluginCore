using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using XrmPluginCore.SourceGenerator.Helpers;

namespace XrmPluginCore.SourceGenerator.Analyzers;

/// <summary>
/// Analyzer that warns when string literals are used instead of nameof() for handler methods in RegisterStep calls.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PreferNameofAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(DiagnosticDescriptors.PreferNameofOverStringLiteral);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;

		var handlerArgument = GetHandlerArgument(invocation, context.SemanticModel, out var serviceType);
		if (handlerArgument == null)
		{
			return;
		}

		// Check if the handler argument is a string literal
		if (handlerArgument is not LiteralExpressionSyntax literal ||
			!literal.IsKind(SyntaxKind.StringLiteralExpression))
		{
			return;
		}

		var methodName = literal.Token.ValueText;

		// Create diagnostic properties for the code fix
		var properties = ImmutableDictionary.CreateBuilder<string, string>();
		properties.Add(Constants.PropertyServiceType, serviceType);
		properties.Add(Constants.PropertyMethodName, methodName);

		var diagnostic = Diagnostic.Create(
			DiagnosticDescriptors.PreferNameofOverStringLiteral,
			literal.GetLocation(),
			properties.ToImmutable(),
			serviceType,
			methodName);

		context.ReportDiagnostic(diagnostic);
	}

	/// <summary>
	/// Returns the handler-method argument (and the service type name) for a RegisterStep call with a
	/// handler argument, or a typed <c>RegisterAPI&lt;TService&gt;(name, handlerMethodName)</c> call.
	/// Returns null when the invocation is neither.
	/// </summary>
	private static ExpressionSyntax GetHandlerArgument(
		InvocationExpressionSyntax invocation,
		SemanticModel semanticModel,
		out string serviceType)
	{
		serviceType = null;

		if (RegisterStepHelper.IsRegisterStepCall(invocation, out var stepGeneric))
		{
			if (stepGeneric.TypeArgumentList.Arguments.Count < 2)
			{
				return null;
			}

			// Resolve by parameter name so named/reordered arguments are honored. Only the typed
			// handler-name overload has a 'handlerMethodName' parameter; the action overloads don't, so
			// this returns null for them (their lambda argument is never a string literal anyway).
			var handler = ArgumentBinder.GetArgument(invocation, semanticModel, Constants.ParameterHandlerMethodName);
			if (handler == null)
			{
				return null;
			}

			serviceType = stepGeneric.TypeArgumentList.Arguments[1].ToString();
			return handler;
		}

		if (RegisterApiHelper.IsRegisterApiCall(invocation, out var apiGeneric) &&
			RegisterApiHelper.IsTypedHandlerOverload(invocation, semanticModel))
		{
			if (apiGeneric.TypeArgumentList.Arguments.Count < 1)
			{
				return null;
			}

			var handler = RegisterApiHelper.GetHandlerArgument(invocation, semanticModel);
			if (handler == null)
			{
				return null;
			}

			serviceType = apiGeneric.TypeArgumentList.Arguments[0].ToString();
			return handler;
		}

		return null;
	}
}
