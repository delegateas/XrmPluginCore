using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using XrmPluginCore.SourceGenerator.Helpers;

namespace XrmPluginCore.SourceGenerator.Analyzers;

/// <summary>
/// Analyzer that warns when lambda invocation syntax (s => s.Method()) is used with image registrations
/// instead of method reference syntax (nameof(Service.Method)).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ImageWithoutMethodReferenceAnalyzer : DiagnosticAnalyzer
{
	private enum ImageRegistrationType
	{
		None,
		Modern,  // WithPreImage, WithPostImage
		Legacy   // AddImage
	}

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(
			DiagnosticDescriptors.ImageWithoutMethodReference,
			DiagnosticDescriptors.LegacyImageRegistration);

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

		// Check if the 3rd argument is a lambda with an invocation body (s => s.Method())
		if (!IsLambdaWithInvocation(handlerArgument, out var methodName, out var hasArguments))
		{
			return;
		}

		// Check if the call chain has image registration and which type
		var imageType = GetImageRegistrationType(invocation);
		if (imageType == ImageRegistrationType.None)
		{
			return;
		}

		// Get the service type name (TService)
		var serviceType = genericName.TypeArgumentList.Arguments[1].ToString();

		// Create diagnostic properties for the code fix
		var properties = ImmutableDictionary.CreateBuilder<string, string>();
		properties.Add("ServiceType", serviceType);
		properties.Add("MethodName", methodName);
		properties.Add("HasArguments", hasArguments.ToString());

		// Select appropriate diagnostic based on API type
		var descriptor = imageType == ImageRegistrationType.Modern
			? DiagnosticDescriptors.ImageWithoutMethodReference
			: DiagnosticDescriptors.LegacyImageRegistration;

		var diagnostic = Diagnostic.Create(
			descriptor,
			handlerArgument.GetLocation(),
			properties.ToImmutable());

		context.ReportDiagnostic(diagnostic);
	}

	private static bool IsLambdaWithInvocation(ExpressionSyntax expression, out string methodName, out bool hasArguments)
	{
		methodName = null;
		hasArguments = false;

		// Check for simple lambda: s => s.Method()
		if (expression is SimpleLambdaExpressionSyntax simpleLambda)
		{
			if (simpleLambda.Body is InvocationExpressionSyntax invocation &&
				invocation.Expression is MemberAccessExpressionSyntax memberAccess)
			{
				methodName = memberAccess.Name.Identifier.Text;
				hasArguments = invocation.ArgumentList.Arguments.Count > 0;
				return true;
			}
		}

		// Check for parenthesized lambda: (s) => s.Method()
		if (expression is ParenthesizedLambdaExpressionSyntax parenLambda)
		{
			if (parenLambda.Body is InvocationExpressionSyntax invocation &&
				invocation.Expression is MemberAccessExpressionSyntax memberAccess)
			{
				methodName = memberAccess.Name.Identifier.Text;
				hasArguments = invocation.ArgumentList.Arguments.Count > 0;
				return true;
			}
		}

		return false;
	}

	private static ImageRegistrationType GetImageRegistrationType(InvocationExpressionSyntax registerStepInvocation)
	{
		// Walk up to find the full fluent call chain
		var current = registerStepInvocation.Parent;
		var result = ImageRegistrationType.None;

		while (current != null)
		{
			// Check if this is a method call in the chain
			if (current is MemberAccessExpressionSyntax memberAccess)
			{
				var methodName = memberAccess.Name.Identifier.Text;
				if (methodName == Constants.WithPreImageMethodName ||
					methodName == Constants.WithPostImageMethodName)
				{
					// Modern API takes precedence
					return ImageRegistrationType.Modern;
				}

				if (methodName == Constants.AddImageMethodName)
				{
					// Track legacy, but keep looking for modern
					result = ImageRegistrationType.Legacy;
				}
			}

			// Move up the syntax tree
			current = current.Parent;
		}

		return result;
	}
}
