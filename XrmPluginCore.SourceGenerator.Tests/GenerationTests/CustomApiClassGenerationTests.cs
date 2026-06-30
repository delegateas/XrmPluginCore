using FluentAssertions;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.GenerationTests;

/// <summary>
/// Tests for the type-safe Custom API Request/Response/ActionWrapper code generation.
/// </summary>
public class CustomApiClassGenerationTests
{
	[Fact]
	public void Should_Generate_Request_Class_With_Properties()
	{
		var result = GeneratorTestHelper.RunCustomApiGenerator(
			CompilationHelper.CreateCompilation(TestFixtures.GetCustomApiPlugin()));

		result.GeneratedTrees.Should().NotBeEmpty();
		var generated = result.GeneratedTrees[0].GetText().ToString();

		generated.Should().Contain("public sealed class SomeApiRequest");
		// NRT is enabled for this test compilation, so reference types are annotated nullable
		generated.Should().Contain("public string? EntityLogicalName { get; set; }");
		generated.Should().Contain("public System.Guid EntityId { get; set; }");
		// Optional value-type parameter becomes nullable
		generated.Should().Contain("public int? Count { get; set; }");
		// The generated file opts into the nullable context so the annotations are valid
		generated.Should().Contain("#nullable enable");
	}

	[Fact]
	public void Should_Generate_Response_Class_With_Constructor()
	{
		var result = GeneratorTestHelper.RunCustomApiGenerator(
			CompilationHelper.CreateCompilation(TestFixtures.GetCustomApiPlugin()));

		var generated = result.GeneratedTrees[0].GetText().ToString();

		generated.Should().Contain("public sealed class SomeApiResponse");
		generated.Should().Contain("public int StatusCode { get; set; }");
		generated.Should().Contain("public string? ErrorMessage { get; set; }");
		generated.Should().Contain("public SomeApiResponse(int statusCode, string? errorMessage)");
		generated.Should().Contain("StatusCode = statusCode;");
		generated.Should().Contain("ErrorMessage = errorMessage;");
	}

	[Fact]
	public void Should_Generate_ActionWrapper_Marshalling_Inputs_And_Outputs()
	{
		var result = GeneratorTestHelper.RunCustomApiGenerator(
			CompilationHelper.CreateCompilation(TestFixtures.GetCustomApiPlugin()));

		var generated = result.GeneratedTrees[0].GetText().ToString();

		generated.Should().Contain("internal sealed class SomeApiActionWrapper : IActionWrapper");
		generated.Should().Contain("var service = serviceProvider.GetRequiredService<TestNamespace.CallbackService>();");
		generated.Should().Contain("var request = new SomeApiRequest");
		generated.Should().Contain("EntityLogicalName = context.InputParameters.Contains(\"EntityLogicalName\") ? (string?)context.InputParameters[\"EntityLogicalName\"] : default,");
		generated.Should().Contain("var response = service.Handle(request);");
		generated.Should().Contain("context.OutputParameters[\"StatusCode\"] = response.StatusCode;");
		generated.Should().Contain("context.OutputParameters[\"ErrorMessage\"] = response.ErrorMessage;");
	}

	[Fact]
	public void Should_Omit_Request_When_No_Request_Parameters()
	{
		var result = GeneratorTestHelper.RunCustomApiGenerator(
			CompilationHelper.CreateCompilation(TestFixtures.GetCustomApiPlugin(withRequest: false)));

		var generated = result.GeneratedTrees[0].GetText().ToString();

		generated.Should().NotContain("class SomeApiRequest");
		generated.Should().Contain("public sealed class SomeApiResponse");
		// Handler invoked with no argument
		generated.Should().Contain("var response = service.Handle();");
		generated.Should().NotContain("var request = new");
	}

	[Fact]
	public void Should_Return_Void_When_No_Response_Properties()
	{
		var result = GeneratorTestHelper.RunCustomApiGenerator(
			CompilationHelper.CreateCompilation(TestFixtures.GetCustomApiPlugin(withResponse: false)));

		var generated = result.GeneratedTrees[0].GetText().ToString();

		generated.Should().NotContain("class SomeApiResponse");
		generated.Should().Contain("public sealed class SomeApiRequest");
		// Handler invoked as a statement (void return), no OutputParameters writes
		generated.Should().Contain("service.Handle(request);");
		generated.Should().NotContain("var response =");
		generated.Should().NotContain("context.OutputParameters[");
	}

	[Fact]
	public void Should_Escape_Response_Constructor_Parameter_That_Is_A_Keyword()
	{
		// A response property whose camelCased name is a reserved keyword must be emitted as a verbatim
		// identifier in the constructor (e.g. "Class" -> "@class"), otherwise the generated code won't compile.
		const string source = """
			using XrmPluginCore;
			using XrmPluginCore.Enums;
			using Microsoft.Extensions.DependencyInjection;

			namespace TestNamespace
			{
			    public class SomeApi : Plugin
			    {
			        public SomeApi()
			        {
			            RegisterAPI<CallbackService>(nameof(SomeApi), nameof(CallbackService.Handle))
			                .AddResponseProperty("Class", CustomApiParameterType.String);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			            => services.AddScoped<CallbackService>();
			    }

			    public class CallbackService
			    {
			        public SomeApiResponse Handle() => new SomeApiResponse(null);
			    }
			}
			""";

		var result = GeneratorTestHelper.RunCustomApiGenerator(CompilationHelper.CreateCompilation(source));

		var generated = result.GeneratedTrees[0].GetText().ToString();

		// Constructor parameter escaped, assignment uses the same verbatim name
		generated.Should().Contain("public SomeApiResponse(string? @class)");
		generated.Should().Contain("Class = @class;");
	}

	[Fact]
	public void Should_Not_Annotate_Reference_Types_When_Nullable_Disabled()
	{
		// Backwards compatibility: with NRT disabled (e.g. .NET Framework / C# 7.3 defaults), the
		// generated code must not contain reference-type '?' annotations nor a #nullable directive.
		var result = GeneratorTestHelper.RunCustomApiGenerator(
			CompilationHelper.CreateCompilation(
				TestFixtures.GetCustomApiPlugin(),
				nullableContextOptions: Microsoft.CodeAnalysis.NullableContextOptions.Disable));

		var generated = result.GeneratedTrees[0].GetText().ToString();

		generated.Should().NotContain("#nullable");
		generated.Should().Contain("public string EntityLogicalName { get; set; }");
		generated.Should().Contain("public string ErrorMessage { get; set; }");
		generated.Should().NotContain("string?");
		// Nullable value types are still emitted (System.Nullable<T> is valid everywhere)
		generated.Should().Contain("public int? Count { get; set; }");
	}

	[Fact]
	public void Should_Not_Generate_For_Action_Based_RegisterAPI()
	{
		// The action-based RegisterAPI overload (not the typed handler-name overload) must not trigger generation.
		const string source = """
			using XrmPluginCore;
			using XrmPluginCore.Enums;

			namespace TestNamespace
			{
			    public class SomeApi : Plugin
			    {
			        public SomeApi()
			        {
			            RegisterCustomAPI("some_api", ctx => { })
			                .AddRequestParameter("EntityId", CustomApiParameterType.Guid);
			        }
			    }
			}
			""";

		var result = GeneratorTestHelper.RunCustomApiGenerator(
			CompilationHelper.CreateCompilation(source));

		result.GeneratedTrees.Should().BeEmpty();
	}
}
