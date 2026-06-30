using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using XrmPluginCore.SourceGenerator.Helpers;

namespace XrmPluginCore.SourceGenerator.Analyzers;

/// <summary>
/// Reports an error when a handler method referenced in a type-safe
/// <c>RegisterAPI&lt;TService&gt;(name, handlerMethodName)</c> call does not exist on the service type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CustomApiHandlerMethodNotFoundAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(DiagnosticDescriptors.CustomApiHandlerMethodNotFound);

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
		if (methods.Any())
		{
			return; // Method exists
		}

		var properties = ImmutableDictionary.CreateBuilder<string, string>();
		properties.Add(Constants.PropertyServiceType, serviceType.Name);
		properties.Add(Constants.PropertyMethodName, methodName);

		var generation = CustomApiGenerationContext.TryCreate(invocation, context.SemanticModel);
		if (generation != null)
		{
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
		}

		context.ReportDiagnostic(Diagnostic.Create(
			DiagnosticDescriptors.CustomApiHandlerMethodNotFound,
			handlerArgument.GetLocation(),
			properties.ToImmutable(),
			methodName,
			serviceType.Name));
	}
}
