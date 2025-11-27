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

		// Check if this is a RegisterStep call
		if (!RegisterStepHelper.IsRegisterStepCall(invocation, out var genericName))
		{
			return;
		}

		// Check if there are at least 2 type arguments (TEntity, TService)
		if (genericName.TypeArgumentList.Arguments.Count < 2)
		{
			return;
		}

		// Check if there's a 3rd argument (the handler method)
		var arguments = invocation.ArgumentList.Arguments;
		if (arguments.Count < 3)
		{
			return;
		}

		var handlerArgument = arguments[2].Expression;

		// Check if the 3rd argument is a string literal
		if (handlerArgument is not LiteralExpressionSyntax literal ||
			!literal.IsKind(SyntaxKind.StringLiteralExpression))
		{
			return;
		}

		// Get the service type name (TService)
		var serviceType = genericName.TypeArgumentList.Arguments[1].ToString();
		var methodName = literal.Token.ValueText;

		// Create diagnostic properties for the code fix
		var properties = ImmutableDictionary.CreateBuilder<string, string>();
		properties.Add("ServiceType", serviceType);
		properties.Add("MethodName", methodName);

		var diagnostic = Diagnostic.Create(
			DiagnosticDescriptors.PreferNameofOverStringLiteral,
			literal.GetLocation(),
			properties.ToImmutable(),
			serviceType,
			methodName);

		context.ReportDiagnostic(diagnostic);
	}
}
