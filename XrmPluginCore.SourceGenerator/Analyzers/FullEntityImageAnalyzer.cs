using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace XrmPluginCore.SourceGenerator.Analyzers;

/// <summary>
/// Analyzer that reports XPC3005 when WithPreImage or WithPostImage is called
/// without specifying any attributes, resulting in a full entity image registration.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FullEntityImageAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(DiagnosticDescriptors.FullEntityImage);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;

		if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
			return;

		var methodName = memberAccess.Name.Identifier.Text;

		if (methodName != Constants.WithPreImageMethodName && methodName != Constants.WithPostImageMethodName)
			return;

		// Only report when called with no arguments (full entity image)
		if (invocation.ArgumentList.Arguments.Count > 0)
			return;

		context.ReportDiagnostic(Diagnostic.Create(
			DiagnosticDescriptors.FullEntityImage,
			invocation.GetLocation(),
			methodName));
	}
}
