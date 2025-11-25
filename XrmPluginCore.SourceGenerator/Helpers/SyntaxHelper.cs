using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace XrmPluginCore.SourceGenerator.Helpers;

/// <summary>
/// Helper methods for analyzing syntax trees
/// </summary>
internal static class SyntaxHelper
{
	/// <summary>
	/// Determines if a class inherits from XrmPluginCore.Plugin
	/// </summary>
	public static bool InheritsFromPlugin(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
	{
		var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
		if (classSymbol == null)
			return false;

		var baseType = classSymbol.BaseType;
		while (baseType != null)
		{
			if (baseType.Name == Constants.PluginBaseClassName &&
				baseType.ContainingNamespace?.ToString() == Constants.PluginNamespace)
			{
				return true;
			}
			baseType = baseType.BaseType;
		}

		return false;
	}

	/// <summary>
	/// Finds all RegisterStep method invocations in a constructor
	/// </summary>
	public static IEnumerable<InvocationExpressionSyntax> FindRegisterStepInvocations(ConstructorDeclarationSyntax constructor)
	{
		// Handle block body: public MyPlugin() { ... }
		if (constructor.Body != null)
		{
			foreach (var statement in constructor.Body.Statements)
			{
				foreach (var invocation in statement.DescendantNodes().OfType<InvocationExpressionSyntax>())
				{
					if (IsRegisterStepInvocation(invocation))
					{
						yield return invocation;
					}
				}
			}
		}
		// Handle expression body: public MyPlugin() => RegisterStep(...);
		else if (constructor.ExpressionBody != null)
		{
			foreach (var invocation in constructor.ExpressionBody.DescendantNodes().OfType<InvocationExpressionSyntax>())
			{
				if (IsRegisterStepInvocation(invocation))
				{
					yield return invocation;
				}
			}
		}
	}

	/// <summary>
	/// Determines if an invocation is a RegisterStep call
	/// </summary>
	private static bool IsRegisterStepInvocation(InvocationExpressionSyntax invocation)
	{
		if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
		{
			// Handle member access: this.RegisterStep<...>(...)
			var methodName = GetMethodName(memberAccess.Name);
			return methodName == Constants.RegisterStepMethodName;
		}
		else
		{
			// Handle direct call: RegisterStep<...>(...) or RegisterStep(...)
			var methodName = GetMethodName(invocation.Expression);
			return methodName == Constants.RegisterStepMethodName;
		}
	}

	/// <summary>
	/// Extracts method name from various syntax node types (handles generic methods)
	/// </summary>
	private static string GetMethodName(SyntaxNode node)
	{
		return node switch
		{
			IdentifierNameSyntax identifier => identifier.Identifier.Text,
			GenericNameSyntax generic => generic.Identifier.Text,
			_ => null
		};
	}

	/// <summary>
	/// Finds all WithPreImage, WithPostImage, AddPreImage, AddPostImage, or AddImage calls chained to a RegisterStep invocation.
	/// Handles both generic methods (AddPreImage&lt;T&gt;) and non-generic methods.
	/// </summary>
	public static IEnumerable<InvocationExpressionSyntax> FindImageInvocations(InvocationExpressionSyntax registerStepInvocation)
	{
		var parent = registerStepInvocation.Parent;
		while (parent != null)
		{
			if (parent is InvocationExpressionSyntax invocation && invocation.Expression is MemberAccessExpressionSyntax memberAccess)
			{
				// Handle both GenericNameSyntax (AddPreImage<T>) and IdentifierNameSyntax (AddPreImage)
				var methodName = GetMethodName(memberAccess.Name);
				if (methodName == Constants.WithPreImageMethodName || methodName == Constants.WithPostImageMethodName ||
					methodName == Constants.AddImageMethodName)
				{
					yield return invocation;
				}
			}
			parent = parent.Parent;
		}
	}

	/// <summary>
	/// Extracts lambda expressions from method arguments
	/// </summary>
	public static IEnumerable<LambdaExpressionSyntax> ExtractLambdas(ArgumentListSyntax argumentList)
	{
		foreach (var arg in argumentList.Arguments)
		{
			if (arg.Expression is LambdaExpressionSyntax lambda)
			{
				yield return lambda;
			}
		}
	}

	/// <summary>
	/// Extracts the property name from a lambda expression like "x => x.PropertyName"
	/// </summary>
	public static string GetPropertyNameFromLambda(LambdaExpressionSyntax lambda)
	{
		// Handle: x => x.PropertyName
		if (lambda is SimpleLambdaExpressionSyntax simpleLambda)
		{
			if (simpleLambda.Body is MemberAccessExpressionSyntax memberAccess)
			{
				return memberAccess.Name.Identifier.Text;
			}
		}

		return null;
	}

	/// <summary>
	/// Extracts the property name from a nameof expression like "nameof(Entity.PropertyName)"
	/// </summary>
	public static string GetPropertyNameFromNameof(ExpressionSyntax expression)
	{
		// Handle: nameof(Entity.PropertyName)
		if (expression is InvocationExpressionSyntax invocation &&
			invocation.Expression is IdentifierNameSyntax identifier &&
			identifier.Identifier.Text == "nameof")
		{
			if (invocation.ArgumentList.Arguments.Count > 0)
			{
				var argument = invocation.ArgumentList.Arguments[0].Expression;

				// Handle: nameof(Entity.PropertyName)
				if (argument is MemberAccessExpressionSyntax memberAccess)
				{
					return memberAccess.Name.Identifier.Text;
				}

				// Handle: nameof(PropertyName)
				if (argument is IdentifierNameSyntax identifierName)
				{
					return identifierName.Identifier.Text;
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Checks if Execute() is called anywhere in the builder chain starting from RegisterStep
	/// </summary>
	public static bool HasExecuteCall(InvocationExpressionSyntax registerStepInvocation)
	{
		var current = registerStepInvocation.Parent;
		while (current is not null)
		{
			if (current is InvocationExpressionSyntax invocation &&
				invocation.Expression is MemberAccessExpressionSyntax memberAccess)
			{
				var methodName = memberAccess.Name.Identifier.Text;
				if (methodName == "Execute")
					return true;
			}
			current = current.Parent;
		}
		return false;
	}
}
