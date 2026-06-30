using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using XrmPluginCore.SourceGenerator.Helpers;

namespace XrmPluginCore.SourceGenerator.Analyzers;

/// <summary>
/// Reports when a type-safe <c>RegisterAPI&lt;TService&gt;(name, handlerMethodName)</c> call is given a
/// name that is not a compile-time constant. The generated request/response classes and ActionWrapper
/// are named after the API, so a non-constant name silently produces no generated code and fails at
/// runtime (no ActionWrapper to discover).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CustomApiNameNotConstantAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(DiagnosticDescriptors.CustomApiNameNotConstant);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;

		// Only the typed handler-name overload triggers generation; the action-based overloads do not
		// require a constant name.
		if (!RegisterApiHelper.IsRegisterApiCall(invocation, out _) ||
			!RegisterApiHelper.IsTypedHandlerOverload(invocation, context.SemanticModel))
		{
			return;
		}

		var nameArgument = RegisterApiHelper.GetNameArgument(invocation, context.SemanticModel);
		if (nameArgument == null)
		{
			return;
		}

		// A resolvable constant name means generation can proceed.
		if (RegisterApiHelper.GetApiName(invocation, context.SemanticModel) != null)
		{
			return;
		}

		context.ReportDiagnostic(Diagnostic.Create(
			DiagnosticDescriptors.CustomApiNameNotConstant,
			nameArgument.GetLocation()));
	}
}
