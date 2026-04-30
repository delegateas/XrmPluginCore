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

		bool isLocalPluginContextUsage = actionArg is LambdaExpressionSyntax lambda
			? LambdaHasExplicitLocalPluginContextParameter(context, lambda)
			: MethodGroupUsesLocalPluginContext(context, actionArg);

		if (!isLocalPluginContextUsage)
		{
			return;
		}

		var entityTypeName = genericName.TypeArgumentList.Arguments[0].ToString();
		context.ReportDiagnostic(Diagnostic.Create(
			DiagnosticDescriptors.LocalPluginContextAsService,
			invocation.GetLocation(),
			entityTypeName));
	}

	private static bool MethodGroupUsesLocalPluginContext(
		SyntaxNodeAnalysisContext context,
		ExpressionSyntax actionArg)
	{
		// We intentionally use GetMemberGroup rather than GetSymbolInfo.Symbol here.
		//
		// RegisterStep<TEntity> takes Action<IExtendedServiceProvider>. Plugin also inherits
		// IPlugin.Execute(IServiceProvider), so the method group "Execute" always contains at least
		// two candidates. Because IExtendedServiceProvider : IServiceProvider, the inherited
		// Execute(IServiceProvider) satisfies the delegate via contravariance — GetSymbolInfo.Symbol
		// resolves to that method, not to the user's Execute(LocalPluginContext). Using Symbol as
		// the primary check would therefore suppress the diagnostic precisely in the cases where it
		// is most needed: the user defined Execute(LocalPluginContext) intending to use it, but the
		// compiler silently picked the base-class method instead.
		//
		// GetMemberGroup returns the full candidate set regardless of conversion success, so it
		// correctly detects the LocalPluginContext overload even in this inherited-method scenario.
		return context.SemanticModel.GetMemberGroup(actionArg)
			.OfType<IMethodSymbol>()
			.Any(m => m.Parameters.Length == 1
					  && m.Parameters[0].Type.ToDisplayString() == LocalPluginContextFullName);
	}

	private static bool LambdaHasExplicitLocalPluginContextParameter(
		SyntaxNodeAnalysisContext context,
		LambdaExpressionSyntax lambda)
	{
		// Only parenthesized lambdas can have explicit parameter types: (LocalPluginContext ctx) => ...
		if (lambda is not ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: 1 } paren)
		{
			return false;
		}

		var paramTypeSyntax = paren.ParameterList.Parameters[0].Type;
		if (paramTypeSyntax == null)
		{
			return false;
		}

		var typeInfo = context.SemanticModel.GetTypeInfo(paramTypeSyntax);
		return typeInfo.Type?.ToDisplayString() == LocalPluginContextFullName;
	}
}
