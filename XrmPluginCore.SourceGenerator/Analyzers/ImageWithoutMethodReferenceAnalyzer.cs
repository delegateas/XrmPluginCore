using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using XrmPluginCore.SourceGenerator.Helpers;

namespace XrmPluginCore.SourceGenerator.Analyzers;

/// <summary>
/// Analyzer that reports:
/// - XPC3002: When AddImage is used (suggesting migration to WithPreImage/WithPostImage)
/// - XPC3003: When lambda invocation syntax (s => s.Method()) is used with modern API (WithPreImage/WithPostImage)
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ImageWithoutMethodReferenceAnalyzer : DiagnosticAnalyzer
{
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

		// Get image registration info
		var (hasModernApi, addImageLocations) = GetImageRegistrationInfo(invocation);

		// XPC3002: Report for any AddImage usage (suggesting migration to modern API)
		foreach (var addImageLocation in addImageLocations)
		{
			var diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.LegacyImageRegistration,
				addImageLocation);

			context.ReportDiagnostic(diagnostic);
		}

		// XPC3003: Report when modern API is used with lambda invocation syntax
		if (hasModernApi && IsLambdaWithInvocation(handlerArgument, out var methodName, out var hasArguments))
		{
			var serviceType = genericName.TypeArgumentList.Arguments[1].ToString();

			var properties = ImmutableDictionary.CreateBuilder<string, string>();
			properties.Add("ServiceType", serviceType);
			properties.Add("MethodName", methodName ?? string.Empty);
			properties.Add("HasArguments", hasArguments.ToString());

			var diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.ImageWithoutMethodReference,
				handlerArgument.GetLocation(),
				properties.ToImmutable());

			context.ReportDiagnostic(diagnostic);
		}
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

	/// <summary>
	/// Checks the call chain for image registrations.
	/// Returns whether modern API is used and the locations of all AddImage calls.
	/// </summary>
	private static (bool hasModernApi, List<Location> addImageLocations) GetImageRegistrationInfo(
		InvocationExpressionSyntax registerStepInvocation)
	{
		var hasModernApi = false;
		var addImageLocations = new List<Location>();

		// Walk up to find the full fluent call chain
		var current = registerStepInvocation.Parent;

		while (current != null)
		{
			// Check if this is a method call in the chain
			if (current is MemberAccessExpressionSyntax memberAccess)
			{
				var methodName = memberAccess.Name.Identifier.Text;
				if (methodName == Constants.WithPreImageMethodName ||
					methodName == Constants.WithPostImageMethodName)
				{
					hasModernApi = true;
				}
				else if (methodName == Constants.AddImageMethodName)
				{
					// Collect the location of the AddImage identifier for the diagnostic
					addImageLocations.Add(memberAccess.Name.GetLocation());
				}
			}

			// Move up the syntax tree
			current = current.Parent;
		}

		return (hasModernApi, addImageLocations);
	}
}
