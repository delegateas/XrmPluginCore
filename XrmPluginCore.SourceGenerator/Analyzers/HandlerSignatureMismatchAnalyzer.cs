using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using XrmPluginCore.SourceGenerator.Helpers;

namespace XrmPluginCore.SourceGenerator.Analyzers;

/// <summary>
/// Analyzer that reports an error when a handler method signature does not match the registered images.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerSignatureMismatchAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(DiagnosticDescriptors.HandlerSignatureMismatch);

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

		// Get the method name from nameof or string literal
		var methodName = RegisterStepHelper.GetMethodName(handlerArgument);
		if (methodName == null)
		{
			return;
		}

		// Get the service type symbol
		var serviceTypeSyntax = genericName.TypeArgumentList.Arguments[1];
		var serviceTypeInfo = context.SemanticModel.GetTypeInfo(serviceTypeSyntax);
		var serviceType = serviceTypeInfo.Type;

		if (serviceType == null)
		{
			return;
		}

		// Check if the method exists on the service type
		var methods = TypeHelper.GetAllMethodsIncludingInherited(serviceType, methodName);
		if (!methods.Any())
		{
			return; // Method doesn't exist - XPC4002 handles this
		}

		// Check for registered images
		var (hasPreImage, hasPostImage) = RegisterStepHelper.CheckForImages(invocation);

		// If no images registered, no signature check needed
		if (!hasPreImage && !hasPostImage)
		{
			return;
		}

		// Check if any overload matches the expected signature
		var hasMatchingOverload = methods.Any(method => SignatureMatches(method, hasPreImage, hasPostImage));
		if (hasMatchingOverload)
		{
			return;
		}

		// Build expected signature description
		var expectedSignature = SyntaxFactoryHelper.BuildSignatureDescription(hasPreImage, hasPostImage);

		// Create diagnostic properties for the code fix
		var properties = ImmutableDictionary.CreateBuilder<string, string>();
		properties.Add("ServiceType", serviceType.Name);
		properties.Add("MethodName", methodName);
		properties.Add("HasPreImage", hasPreImage.ToString());
		properties.Add("HasPostImage", hasPostImage.ToString());

		var diagnostic = Diagnostic.Create(
			DiagnosticDescriptors.HandlerSignatureMismatch,
			handlerArgument.GetLocation(),
			properties.ToImmutable(),
			methodName,
			expectedSignature);

		context.ReportDiagnostic(diagnostic);
	}

	private static bool SignatureMatches(IMethodSymbol method, bool hasPreImage, bool hasPostImage)
	{
		var parameters = method.Parameters;
		var expectedParamCount = (hasPreImage ? 1 : 0) + (hasPostImage ? 1 : 0);

		if (parameters.Length != expectedParamCount)
		{
			return false;
		}

		var paramIndex = 0;

		if (hasPreImage)
		{
			if (paramIndex >= parameters.Length)
			{
				return false;
			}

			if (!IsImageParameter(parameters[paramIndex], Constants.PreImageTypeName))
			{
				return false;
			}

			paramIndex++;
		}

		if (hasPostImage)
		{
			if (paramIndex >= parameters.Length)
			{
				return false;
			}

			if (!IsImageParameter(parameters[paramIndex], Constants.PostImageTypeName))
			{
				return false;
			}
		}

		return true;
	}

	private static bool IsImageParameter(IParameterSymbol parameter, string expectedImageType)
	{
		return parameter.Type.Name == expectedImageType;
	}
}
