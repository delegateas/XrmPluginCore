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
		generated.Should().Contain("this.StatusCode = statusCode;");
		generated.Should().Contain("this.ErrorMessage = errorMessage;");
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
		generated.Should().Contain("this.Class = @class;");
	}

	[Fact]
	public void Should_Escape_Keyword_Property_Names_At_Every_Emission_Site()
	{
		// A unique name that sanitizes to a reserved keyword (e.g. "namespace"/"event") must be escaped
		// everywhere it is emitted as an identifier: property declarations, the request object
		// initializer, the response constructor assignment, and the response member access in the wrapper.
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
			                .AddRequestParameter("namespace", CustomApiParameterType.String)
			                .AddResponseProperty("event", CustomApiParameterType.String);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			            => services.AddScoped<CallbackService>();
			    }

			    public class CallbackService
			    {
			        public SomeApiResponse Handle(SomeApiRequest request) => new SomeApiResponse(null);
			    }
			}
			""";

		var result = GeneratorTestHelper.RunCustomApiGenerator(CompilationHelper.CreateCompilation(source));
		var generated = result.GeneratedTrees[0].GetText().ToString();

		// Property declarations
		generated.Should().Contain("public string? @namespace { get; set; }");
		generated.Should().Contain("public string? @event { get; set; }");
		// Request object initializer (dictionary key stays the raw unique name)
		generated.Should().Contain("@namespace = context.InputParameters.Contains(\"namespace\")");
		// Response constructor assignment is this-qualified so it sets the property, not the parameter
		generated.Should().Contain("this.@event = @event;");
		// Response member access in the wrapper
		generated.Should().Contain("context.OutputParameters[\"event\"] = response.@event;");

		// The generated source compiles (no keyword-as-identifier errors)
		using var ms = new System.IO.MemoryStream();
		var emit = result.OutputCompilation.Emit(ms);
		var errors = emit.Diagnostics
			.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
			.Select(d => $"{d.Id}: {d.GetMessage()}")
			.ToArray();
		errors.Should().BeEmpty();
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
	public void Should_Preserve_Leading_Digit_In_Sanitized_Class_Names()
	{
		// An API name that starts with a digit is not a valid identifier. The generator prefixes a single
		// '_' rather than dropping the digit, so "1CustomApi" -> "_1CustomApi" (not "__CustomApi"), which
		// also keeps names that differ only by their leading digit distinct.
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
			            RegisterAPI<CallbackService>("1CustomApi", nameof(CallbackService.Handle))
			                .AddResponseProperty("StatusCode", CustomApiParameterType.Integer);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			            => services.AddScoped<CallbackService>();
			    }

			    public class CallbackService
			    {
			        public _1CustomApiResponse Handle() => new _1CustomApiResponse(0);
			    }
			}
			""";

		var result = GeneratorTestHelper.RunCustomApiGenerator(CompilationHelper.CreateCompilation(source));
		var generated = result.GeneratedTrees[0].GetText().ToString();

		generated.Should().Contain("public sealed class _1CustomApiResponse");
		generated.Should().Contain("internal sealed class _1CustomApiActionWrapper");
		// The digit is preserved, not collapsed into a second underscore
		generated.Should().NotContain("__CustomApi");
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
