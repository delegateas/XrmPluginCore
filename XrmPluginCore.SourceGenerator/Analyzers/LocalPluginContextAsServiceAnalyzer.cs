using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
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

		// Only fire for exactly 2 type args: RegisterStep<TEntity, TService>
		if (genericName.TypeArgumentList.Arguments.Count != 2)
		{
			return;
		}

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
}
