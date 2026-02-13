using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using XrmPluginCore.SourceGenerator.Tests.Helpers;

namespace XrmPluginCore.SourceGenerator.Tests.DiagnosticTests;

public abstract class CodeFixTestBase
{
	protected static Document CreateDocument(string source)
	{
		var projectId = ProjectId.CreateNewId();
		var documentId = DocumentId.CreateNewId(projectId);

		var references = new[]
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Plugin).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(IPluginDefinition).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Microsoft.Xrm.Sdk.Entity).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions).Assembly.Location),
			MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
			MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
			MetadataReference.CreateFromFile(typeof(System.ComponentModel.DescriptionAttribute).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(XrmPluginCore.Tests.Context.BusinessDomain.Account).Assembly.Location),
		};

		var solution = new AdhocWorkspace().CurrentSolution
			.AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
			.WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
			.WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.CSharp11))
			.AddMetadataReferences(projectId, references)
			.AddDocument(documentId, "Test.cs", source);

		return solution.GetDocument(documentId)!;
	}

	protected static async Task<string> ApplyCodeFixAsync(
		string source,
		DiagnosticAnalyzer analyzer,
		CodeFixProvider codeFixProvider,
		params string[] diagnosticIds)
	{
		var compilation = CompilationHelper.CreateCompilation(source);

		var compilationWithAnalyzers = compilation.WithAnalyzers(
			[analyzer]);

		var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
		var diagnostic = diagnostics.FirstOrDefault(d => diagnosticIds.Contains(d.Id));

		if (diagnostic == null)
		{
			return source;
		}

		var document = CreateDocument(source);
		var actions = new List<CodeAction>();

		var context = new CodeFixContext(
			document,
			diagnostic,
			(action, _) => actions.Add(action),
			CancellationToken.None);

		await codeFixProvider.RegisterCodeFixesAsync(context);

		if (actions.Count == 0)
		{
			return source;
		}

		var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
		var changedSolution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
		var changedDocument = changedSolution.GetDocument(document.Id);
		var newText = await changedDocument!.GetTextAsync();

		return newText.ToString();
	}

	protected static async Task<List<CodeAction>> GetCodeActionsAsync(
		string source,
		DiagnosticAnalyzer analyzer,
		CodeFixProvider codeFixProvider,
		params string[] diagnosticIds)
	{
		var compilation = CompilationHelper.CreateCompilation(source);

		var compilationWithAnalyzers = compilation.WithAnalyzers(
			[analyzer]);

		var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
		var diagnostic = diagnostics.FirstOrDefault(d => diagnosticIds.Contains(d.Id));

		if (diagnostic == null)
		{
			return [];
		}

		var document = CreateDocument(source);
		var actions = new List<CodeAction>();

		var context = new CodeFixContext(
			document,
			diagnostic,
			(action, _) => actions.Add(action),
			CancellationToken.None);

		await codeFixProvider.RegisterCodeFixesAsync(context);

		return actions;
	}
}
