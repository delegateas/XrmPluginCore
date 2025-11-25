using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using XrmPluginCore.SourceGenerator.Helpers;
using XrmPluginCore.SourceGenerator.Models;

namespace XrmPluginCore.SourceGenerator.Parsers;

/// <summary>
/// Parses plugin class syntax to extract registration metadata
/// </summary>
internal static class RegistrationParser
{
	/// <summary>
	/// Parses a plugin class and extracts all plugin step metadata
	/// </summary>
	public static IEnumerable<PluginStepMetadata> ParsePluginClass(
		ClassDeclarationSyntax classDeclaration,
		SemanticModel semanticModel)
	{
		// Check if plugin class has a parameterless constructor
		var hasParameterlessConstructor = classDeclaration.Members
			.OfType<ConstructorDeclarationSyntax>()
			.Any(c => c.ParameterList.Parameters.Count == 0);

		// Check if class has ANY explicit constructors
		var hasExplicitConstructors = classDeclaration.Members
			.OfType<ConstructorDeclarationSyntax>()
			.Any();

		// If class has explicit constructors but no parameterless one, report diagnostic
		if (hasExplicitConstructors && !hasParameterlessConstructor)
		{
			var diagnosticMetadata = new PluginStepMetadata
			{
				PluginClassName = classDeclaration.Identifier.Text,
				Namespace = classDeclaration.GetNamespace(),
				Location = classDeclaration.GetLocation(),
				Images = [] // Empty - no generation
			};

			diagnosticMetadata.Diagnostics.Add(new DiagnosticInfo
			{
				Descriptor = DiagnosticDescriptors.NoParameterlessConstructor,
				Location = classDeclaration.Identifier.GetLocation(),
				MessageArgs = [classDeclaration.Identifier.Text]
			});

			yield return diagnosticMetadata;
			yield break;
		}

		// Find the parameterless constructor (registration pipeline only supports parameterless)
		var constructor = classDeclaration.Members
			.OfType<ConstructorDeclarationSyntax>()
			.FirstOrDefault(c => c.ParameterList.Parameters.Count == 0);

		if (constructor == null)
			yield break;

		// Find all RegisterStep invocations
		foreach (var registerStep in SyntaxHelper.FindRegisterStepInvocations(constructor))
		{
			var metadata = ParseRegisterStepInvocation(registerStep, semanticModel, classDeclaration);
			if (metadata != null)
			{
				yield return metadata;
			}
		}
	}

	/// <summary>
	/// Parses a single RegisterStep invocation
	/// </summary>
	private static PluginStepMetadata ParseRegisterStepInvocation(
		InvocationExpressionSyntax registerStepInvocation,
		SemanticModel semanticModel,
		ClassDeclarationSyntax classDeclaration)
	{
		// Get the symbol info to extract type arguments
		var symbolInfo = semanticModel.GetSymbolInfo(registerStepInvocation);

		// Handle both resolved symbols and candidate symbols (when overload resolution is ambiguous)
		IMethodSymbol methodSymbol = symbolInfo.Symbol as IMethodSymbol;
		if (methodSymbol == null && symbolInfo.CandidateSymbols.Length > 0)
		{
			methodSymbol = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
		}

		if (methodSymbol == null)
		{
			return null;
		}

		// Extract entity type from generic parameter TEntity
		if (methodSymbol.TypeArguments.Length == 0)
		{
			return null;
		}

		var entityType = methodSymbol.TypeArguments[0];
		var metadata = new PluginStepMetadata
		{
			EntityTypeName = entityType.Name,
			Namespace = classDeclaration.GetNamespace(),
			PluginClassName = classDeclaration.Identifier.Text
		};

		// Extract service type from generic parameter TService (if present)
		if (methodSymbol.TypeArguments.Length >= 2)
		{
			var serviceType = methodSymbol.TypeArguments[1];
			metadata.ServiceTypeName = serviceType.Name;
			metadata.ServiceTypeFullName = serviceType.ToDisplayString();
		}

		// Extract EventOperation and ExecutionStage from arguments
		var arguments = registerStepInvocation.ArgumentList.Arguments;
		if (arguments.Count >= 2)
		{
			metadata.EventOperation = ExtractEnumValue(arguments[0].Expression);
			metadata.ExecutionStage = ExtractEnumValue(arguments[1].Expression);
		}

		// Extract method reference from 3rd argument if present
		if (arguments.Count >= 3)
		{
			metadata.HandlerMethodName = ParseMethodReference(arguments[2].Expression, semanticModel);
		}

		// Find image calls
		foreach (var imageCall in SyntaxHelper.FindImageInvocations(registerStepInvocation))
		{
			var imageMetadata = ParseImageInvocation(imageCall, entityType);
			if (imageMetadata != null)
			{
				metadata.Images.Add(imageMetadata);
			}
		}

		// After parsing images, check if we have images but no method reference
		// This indicates using old API (s => s.Method()) with new image methods (WithPreImage/WithPostImage)
		if (metadata.Images.Any() && string.IsNullOrEmpty(metadata.HandlerMethodName))
		{
			metadata.Diagnostics.Add(new DiagnosticInfo
			{
				Descriptor = DiagnosticDescriptors.ImageWithoutMethodReference,
				Location = registerStepInvocation.GetLocation(),
				MessageArgs = []
			});
		}

		// Return metadata if we have a method reference (for code generation)
		// OR if we have diagnostics to report
		return !string.IsNullOrEmpty(metadata.HandlerMethodName) || metadata.Diagnostics.Any() ? metadata : null;
	}

	/// <summary>
	/// Parses a method reference expression like "service => service.HandleUpdate"
	/// </summary>
	private static string ParseMethodReference(ExpressionSyntax expression, SemanticModel semanticModel)
	{
		// Handle: service => service.HandleUpdate
		if (expression is SimpleLambdaExpressionSyntax lambda)
		{
			if (lambda.Body is MemberAccessExpressionSyntax memberAccess)
			{
				return memberAccess.Name.Identifier.Text;
			}
		}

		// Handle: (service) => service.HandleUpdate
		if (expression is ParenthesizedLambdaExpressionSyntax parenLambda)
		{
			if (parenLambda.Body is MemberAccessExpressionSyntax memberAccess)
			{
				return memberAccess.Name.Identifier.Text;
			}
		}

		return null;
	}

	/// <summary>
	/// Parses WithPreImage, WithPostImage, or AddImage call to extract image metadata.
	/// </summary>
	private static ImageMetadata ParseImageInvocation(
		InvocationExpressionSyntax imageInvocation,
		ITypeSymbol entityType)
	{
		if (imageInvocation.Expression is not MemberAccessExpressionSyntax memberAccess)
			return null;

		// Get method name - handle both generic (AddPreImage<T>) and non-generic (AddPreImage)
		string methodName;
		bool isGenericMethod = false;
		if (memberAccess.Name is GenericNameSyntax genericName)
		{
			methodName = genericName.Identifier.Text;
			isGenericMethod = true;
		}
		else if (memberAccess.Name is IdentifierNameSyntax identifierName)
		{
			methodName = identifierName.Identifier.Text;
		}
		else
		{
			return null;
		}

		var imageMetadata = new ImageMetadata();
		var arguments = imageInvocation.ArgumentList.Arguments;
		int attributeStartIndex = 0;

		// Determine image type and starting index for attributes
		if (methodName == Constants.AddImageMethodName)
		{
			// Old API: AddImage(ImageType.PreImage, "name", attr1, attr2, ...)
			if (arguments.Count > 0)
			{
				var imageTypeArg = arguments[0].Expression;
				imageMetadata.ImageType = ExtractEnumValue(imageTypeArg);

				// Skip first argument (ImageType), process remaining
				attributeStartIndex = 1;
			}
		}
		else if (methodName == Constants.WithPreImageMethodName)
		{
			// New API: WithPreImage(x => x.Name, ...)
			imageMetadata.ImageType = Constants.PreImageTypeName;
			attributeStartIndex = 0;
		}
		else if (methodName == Constants.WithPostImageMethodName)
		{
			// New API: WithPostImage(x => x.Name, ...)
			imageMetadata.ImageType = Constants.PostImageTypeName;
			attributeStartIndex = 0;
		}

		// For WithPreImage/WithPostImage, all arguments are attributes
		// For AddImage, first string after ImageType might be image name
		bool allArgumentsAreAttributes = isGenericMethod ||
			methodName == Constants.WithPreImageMethodName || methodName == Constants.WithPostImageMethodName;

		// Process arguments starting from attributeStartIndex
		for (int i = attributeStartIndex; i < arguments.Count; i++)
		{
			var argument = arguments[i];

			// Try to extract from nameof expression
			string value = SyntaxHelper.GetPropertyNameFromNameof(argument.Expression);

			// Try to extract from string literal
			if (value is null && argument.Expression is LiteralExpressionSyntax literal)
			{
				value = literal.Token.ValueText;
			}

			// Try to extract from lambda
			if (value is null && argument.Expression is LambdaExpressionSyntax lambda)
			{
				value = SyntaxHelper.GetPropertyNameFromLambda(lambda);
			}

			if (value is not null)
			{
				// Lambdas are always attributes, never image names
				// String literals in old AddImage API: first one might be image name
				bool isLambda = argument.Expression is LambdaExpressionSyntax;
				bool treatAsAttribute = allArgumentsAreAttributes || isLambda || !string.IsNullOrEmpty(imageMetadata.ImageName);

				if (treatAsAttribute)
				{
					// This is an attribute
					var attrMetadata = GetAttributeMetadata(value, entityType);
					if (attrMetadata != null)
					{
						imageMetadata.Attributes.Add(attrMetadata);
					}
				}
				else
				{
					// Old AddImage API: first string literal is image name
					imageMetadata.ImageName = value;
				}
			}
		}

		// Default image name if not provided
		if (string.IsNullOrEmpty(imageMetadata.ImageName))
		{
			imageMetadata.ImageName = imageMetadata.ImageType;
		}

		return imageMetadata.Attributes.Any() ? imageMetadata : null;
	}

	/// <summary>
	/// Gets attribute metadata (property name, logical name, type) for a property
	/// </summary>
	private static AttributeMetadata GetAttributeMetadata(
		string propertyName,
		ITypeSymbol entityType)
	{
		// Find the property in the entity type
		var property = entityType.GetMembers(propertyName)
			.OfType<IPropertySymbol>()
			.FirstOrDefault();

		if (property == null)
			return null;

		// Get the logical name from AttributeLogicalName attribute if present
		var logicalName = GetLogicalNameFromAttribute(property) ?? propertyName.ToLowerInvariant();

		return new AttributeMetadata
		{
			PropertyName = propertyName,
			LogicalName = logicalName,
			TypeName = property.Type.ToDisplayString()
		};
	}

	/// <summary>
	/// Extracts the logical name from [AttributeLogicalName("name")] attribute
	/// </summary>
	private static string GetLogicalNameFromAttribute(IPropertySymbol property)
	{
		var attribute = property.GetAttributes()
			.FirstOrDefault(a => a.AttributeClass?.Name == Constants.LogicalNameAttributeName);

		if (attribute?.ConstructorArguments.Length > 0)
		{
			return attribute.ConstructorArguments[0].Value?.ToString();
		}

		return null;
	}

	/// <summary>
	/// Extracts enum value name from expression
	/// </summary>
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
}

/// <summary>
/// Extension methods for syntax nodes
/// </summary>
internal static class SyntaxExtensions
{
	public static string GetNamespace(this SyntaxNode node)
	{
		var namespaces = new List<string>();

		while (node != null)
		{
			if (node is NamespaceDeclarationSyntax namespaceDecl)
			{
				namespaces.Add(namespaceDecl.Name.ToString());
			}
			else if (node is FileScopedNamespaceDeclarationSyntax fileScopedNs)
			{
				namespaces.Add(fileScopedNs.Name.ToString());
			}
			node = node.Parent;
		}

		if (namespaces.Count == 0)
			return "GlobalNamespace";

		// Reverse to get outer-to-inner order, then join
		namespaces.Reverse();
		return string.Join(".", namespaces);
	}
}
