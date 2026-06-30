using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using XrmPluginCore.SourceGenerator.Helpers;

namespace XrmPluginCore.SourceGenerator.Analyzers;

/// <summary>
/// Reports when a Custom API handler method signature does not match the declared request parameters
/// and response properties. Reports XPC4005 (Warning) when the generated request/response types do not
/// exist yet, and XPC4006 (Error) when they do.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CustomApiHandlerSignatureMismatchAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(
			DiagnosticDescriptors.CustomApiHandlerSignatureMismatch,
			DiagnosticDescriptors.CustomApiHandlerSignatureMismatchError);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;

		if (!RegisterApiHelper.IsRegisterApiCall(invocation, out var genericName) ||
			!RegisterApiHelper.IsTypedHandlerOverload(invocation, context.SemanticModel))
		{
			return;
		}

		var arguments = invocation.ArgumentList.Arguments;
		if (arguments.Count < 2)
		{
			return;
		}

		var handlerArgument = arguments[1].Expression;
		var methodName = RegisterApiHelper.GetHandlerMethodName(invocation);
		if (methodName == null)
		{
			return;
		}

		var serviceType = RegisterApiHelper.GetServiceType(genericName, context.SemanticModel);
		if (serviceType == null)
		{
			return;
		}

		var methods = TypeHelper.GetAllMethodsIncludingInherited(serviceType, methodName);
		if (!methods.Any())
		{
			return; // Method doesn't exist - XPC4004 handles this
		}

		var generation = CustomApiGenerationContext.TryCreate(invocation, context.SemanticModel);
		if (generation == null)
		{
			return;
		}

		if (methods.Any(m => SignatureMatches(m, generation)))
		{
			return;
		}

		var expectedSignature = BuildSignatureDescription(methodName, generation);
		var generatedTypesExist = DoGeneratedTypesExist(context, generation);

		var descriptor = generatedTypesExist
			? DiagnosticDescriptors.CustomApiHandlerSignatureMismatchError
			: DiagnosticDescriptors.CustomApiHandlerSignatureMismatch;

		var properties = ImmutableDictionary.CreateBuilder<string, string>();
		properties.Add(Constants.PropertyServiceType, serviceType.Name);
		properties.Add(Constants.PropertyMethodName, methodName);
		properties.Add(Constants.PropertyHasRequest, generation.HasRequest.ToString());
		properties.Add(Constants.PropertyHasResponse, generation.HasResponse.ToString());
		if (generation.HasRequest)
		{
			properties.Add(Constants.PropertyRequestTypeName, generation.RequestTypeFullName);
		}
		if (generation.HasResponse)
		{
			properties.Add(Constants.PropertyResponseTypeName, generation.ResponseTypeFullName);
		}

		context.ReportDiagnostic(Diagnostic.Create(
			descriptor,
			handlerArgument.GetLocation(),
			properties.ToImmutable(),
			methodName,
			expectedSignature));
	}

	private static bool SignatureMatches(IMethodSymbol method, CustomApiGenerationContext generation)
	{
		var expectedParamCount = generation.HasRequest ? 1 : 0;
		if (method.Parameters.Length != expectedParamCount)
		{
			return false;
		}

		if (generation.HasRequest && !IsGeneratedType(method.Parameters[0].Type, generation.PluginNamespace, generation.RequestClassName))
		{
			return false;
		}

		// When response properties are declared, the handler must return the generated response so the
		// wrapper can read its properties. When none are declared the return value is ignored.
		if (generation.HasResponse && !IsGeneratedType(method.ReturnType, generation.PluginNamespace, generation.ResponseClassName))
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Matches the generated request/response type by both namespace and name, so a same-named type in a
	/// different namespace is not mistaken for the generated one.
	/// </summary>
	private static bool IsGeneratedType(ITypeSymbol type, string expectedNamespace, string expectedName)
	{
		if (type == null || type.Name != expectedName)
		{
			return false;
		}

		var ns = type.ContainingNamespace;
		var namespaceName = ns == null || ns.IsGlobalNamespace ? string.Empty : ns.ToDisplayString();
		return namespaceName == expectedNamespace;
	}

	private static bool DoGeneratedTypesExist(SyntaxNodeAnalysisContext context, CustomApiGenerationContext generation)
	{
		var compilation = context.SemanticModel.Compilation;

		if (generation.HasRequest && compilation.GetTypeByMetadataName(generation.RequestTypeFullName) == null)
		{
			return false;
		}

		if (generation.HasResponse && compilation.GetTypeByMetadataName(generation.ResponseTypeFullName) == null)
		{
			return false;
		}

		return true;
	}

	private static string BuildSignatureDescription(string methodName, CustomApiGenerationContext generation)
	{
		var returnType = generation.HasResponse ? generation.ResponseClassName : "void";
		var parameter = generation.HasRequest ? $"{generation.RequestClassName} request" : string.Empty;
		return $"{returnType} {methodName}({parameter})";
	}
}
