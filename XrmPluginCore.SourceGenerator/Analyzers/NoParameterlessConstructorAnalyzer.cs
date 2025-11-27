using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using XrmPluginCore.SourceGenerator.Helpers;

namespace XrmPluginCore.SourceGenerator.Analyzers;

/// <summary>
/// Analyzer that warns when a plugin class has explicit constructors but no parameterless constructor.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoParameterlessConstructorAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(DiagnosticDescriptors.NoParameterlessConstructor);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
	}

	private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.Node;

		// Check if the class inherits from Plugin
		if (!SyntaxHelper.InheritsFromPlugin(classDeclaration, context.SemanticModel))
		{
			return;
		}

		// Get all constructors
		var constructors = classDeclaration.Members
			.OfType<ConstructorDeclarationSyntax>()
			.ToList();

		// If no explicit constructors, compiler provides a default parameterless one
		if (constructors.Count == 0)
		{
			return;
		}

		// Check if any constructor is parameterless
		var hasParameterlessConstructor = constructors
			.Any(c => c.ParameterList.Parameters.Count == 0);

		if (hasParameterlessConstructor)
		{
			return;
		}

		// Report diagnostic at the class identifier
		var diagnostic = Diagnostic.Create(
			DiagnosticDescriptors.NoParameterlessConstructor,
			classDeclaration.Identifier.GetLocation(),
			classDeclaration.Identifier.Text);

		context.ReportDiagnostic(diagnostic);
	}
}
