using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using XrmPluginCore.SourceGenerator.Helpers;

namespace XrmPluginCore.SourceGenerator.Analyzers;

/// <summary>
/// Analyzer that reports when a handler method signature does not match the registered images.
/// Reports XPC4002 (Warning) when generated types don't exist yet, XPC4003 (Error) when they do.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerSignatureMismatchAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(
			DiagnosticDescriptors.HandlerSignatureMismatch,
			DiagnosticDescriptors.HandlerSignatureMismatchError);

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
			return; // Method doesn't exist - XPC4001 handles this
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

		// Determine if generated types exist to choose appropriate diagnostic severity
		var generatedTypesExist = DoGeneratedTypesExist(
			context,
			invocation,
			genericName,
			hasPreImage,
			hasPostImage);

		// Choose diagnostic: XPC4003 (Error) if types exist, XPC4002 (Warning) if they don't
		var descriptor = generatedTypesExist
			? DiagnosticDescriptors.HandlerSignatureMismatchError
			: DiagnosticDescriptors.HandlerSignatureMismatch;

		// Create diagnostic properties for the code fix
		var properties = ImmutableDictionary.CreateBuilder<string, string>();
		properties.Add("ServiceType", serviceType.Name);
		properties.Add("MethodName", methodName);
		properties.Add("HasPreImage", hasPreImage.ToString());
		properties.Add("HasPostImage", hasPostImage.ToString());

		var diagnostic = Diagnostic.Create(
			descriptor,
			handlerArgument.GetLocation(),
			properties.ToImmutable(),
			methodName,
			expectedSignature);

		context.ReportDiagnostic(diagnostic);
	}

	private static bool DoGeneratedTypesExist(
		SyntaxNodeAnalysisContext context,
		InvocationExpressionSyntax invocation,
		GenericNameSyntax genericName,
		bool hasPreImage,
		bool hasPostImage)
	{
		// Get plugin class info
		var pluginClass = invocation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
		if (pluginClass == null)
		{
			return false;
		}

		var pluginClassName = pluginClass.Identifier.Text;
		var pluginNamespace = GetNamespace(pluginClass);

		// Get entity type name
		var entityTypeSyntax = genericName.TypeArgumentList.Arguments[0];
		var entityTypeInfo = context.SemanticModel.GetTypeInfo(entityTypeSyntax);
		var entityTypeName = entityTypeInfo.Type?.Name ?? "Unknown";

		// Get operation and stage from arguments
		var arguments = invocation.ArgumentList.Arguments;
		var operation = ExtractEnumValue(arguments[0].Expression);
		var stage = ExtractEnumValue(arguments[1].Expression);

		// Build expected namespace: {Namespace}.PluginRegistrations.{PluginClass}.{Entity}{Op}{Stage}
		var expectedNamespace = $"{pluginNamespace}.PluginRegistrations.{pluginClassName}.{entityTypeName}{operation}{stage}";

		// Check if the required generated types exist
		var compilation = context.SemanticModel.Compilation;

		if (hasPreImage)
		{
			var preImageType = compilation.GetTypeByMetadataName($"{expectedNamespace}.PreImage");
			if (preImageType == null)
			{
				return false;
			}
		}

		if (hasPostImage)
		{
			var postImageType = compilation.GetTypeByMetadataName($"{expectedNamespace}.PostImage");
			if (postImageType == null)
			{
				return false;
			}
		}

		return true;
	}

	private static string GetNamespace(SyntaxNode node)
	{
		while (node != null)
		{
			if (node is NamespaceDeclarationSyntax namespaceDecl)
			{
				return namespaceDecl.Name.ToString();
			}

			if (node is FileScopedNamespaceDeclarationSyntax fileScopedNs)
			{
				return fileScopedNs.Name.ToString();
			}

			node = node.Parent;
		}

		return string.Empty;
	}

	private static string ExtractEnumValue(ExpressionSyntax expression)
	{
		// Handle direct enum access like EventOperation.Update
		if (expression is MemberAccessExpressionSyntax memberAccess)
		{
			return memberAccess.Name.Identifier.Text;
		}

		// Handle string literal for custom messages
		if (expression is LiteralExpressionSyntax literal)
		{
			return literal.Token.ValueText;
		}

		return "Unknown";
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
