using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using XrmPluginCore.SourceGenerator.Helpers;

namespace XrmPluginCore.SourceGenerator.Analyzers;

/// <summary>
/// Analyzer that reports an error when LocalPluginContext is used as TService in RegisterStep calls.
/// This causes a runtime exception because LocalPluginContext is not registered in the DI container.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LocalPluginContextAsServiceAnalyzer : DiagnosticAnalyzer
{
	private const string LocalPluginContextFullName = "XrmPluginCore.LocalPluginContext";

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(DiagnosticDescriptors.LocalPluginContextAsService);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;

		if (!RegisterStepHelper.IsRegisterStepCall(invocation, out var genericName))
		{
			return;
		}

		var typeArgCount = genericName.TypeArgumentList.Arguments.Count;

		if (typeArgCount == 2)
		{
			CheckExplicitLocalPluginContextTypeArg(context, invocation, genericName);
		}
		else if (typeArgCount == 1)
		{
			CheckImplicitLocalPluginContextMethodGroup(context, invocation, genericName);
		}
	}

	private static void CheckExplicitLocalPluginContextTypeArg(
		SyntaxNodeAnalysisContext context,
		InvocationExpressionSyntax invocation,
		GenericNameSyntax genericName)
	{
		// Use semantic model to check full type name (avoids false positives on user-defined LocalPluginContext)
		var serviceTypeArg = genericName.TypeArgumentList.Arguments[1];
		var typeInfo = context.SemanticModel.GetTypeInfo(serviceTypeArg);
		if (typeInfo.Type?.ToDisplayString() != LocalPluginContextFullName)
		{
			return;
		}

		var entityTypeName = genericName.TypeArgumentList.Arguments[0].ToString();

		context.ReportDiagnostic(Diagnostic.Create(
			DiagnosticDescriptors.LocalPluginContextAsService,
			invocation.GetLocation(),
			entityTypeName));
	}

	private static void CheckImplicitLocalPluginContextMethodGroup(
		SyntaxNodeAnalysisContext context,
		InvocationExpressionSyntax invocation,
		GenericNameSyntax genericName)
	{
		var arguments = invocation.ArgumentList.Arguments;
		if (arguments.Count < 3)
		{
			return;
		}

		var actionArg = arguments[2].Expression;

		// Only fire for method groups, not lambdas
		if (actionArg is LambdaExpressionSyntax)
		{
			return;
		}

		// Get all methods in the method group and check if any take LocalPluginContext
		var memberGroup = context.SemanticModel.GetMemberGroup(actionArg);
		var hasLocalPluginContextParam = memberGroup
			.OfType<IMethodSymbol>()
			.Any(m => m.Parameters.Length == 1
					  && m.Parameters[0].Type.ToDisplayString() == LocalPluginContextFullName);

		if (!hasLocalPluginContextParam)
		{
			return;
		}

		var entityTypeName = genericName.TypeArgumentList.Arguments[0].ToString();
		context.ReportDiagnostic(Diagnostic.Create(
			DiagnosticDescriptors.LocalPluginContextAsService,
			invocation.GetLocation(),
			entityTypeName));
	}
}
