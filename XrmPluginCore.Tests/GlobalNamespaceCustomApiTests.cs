using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using XrmPluginCore;
using XrmPluginCore.Enums;
using XrmPluginCore.Tests.Helpers;
using Xunit;

// The generated Request/Response/ActionWrapper for a plugin declared in the GLOBAL namespace are
// emitted under the literal "GlobalNamespace" namespace by the source generator.
using GlobalNamespace;

// Intentionally declared in the GLOBAL namespace (no namespace declaration) to exercise the
// GlobalNamespace fallback in runtime wrapper discovery: Type.Namespace is null here, and the runtime
// must mirror the generator's "GlobalNamespace" fallback to discover the generated ActionWrapper.
public class GlobalNsCustomApi : Plugin
{
	public GlobalNsCustomApi()
	{
		RegisterAPI<GlobalNsCustomApiService>(nameof(GlobalNsCustomApi), nameof(GlobalNsCustomApiService.Handle))
			.AddResponseProperty("StatusCode", CustomApiParameterType.Integer);
	}

	protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
	{
		services.AddScoped<GlobalNsCustomApiService>();
		return base.OnBeforeBuildServiceProvider(services);
	}
}

public class GlobalNsCustomApiService
{
	public GlobalNsCustomApiResponse Handle() => new GlobalNsCustomApiResponse(200);
}

namespace XrmPluginCore.Tests
{
	public class GlobalNamespaceCustomApiTests
	{
		[Fact]
		public void Execute_ShouldDiscoverGeneratedWrapper_ForGlobalNamespacePlugin()
		{
			// Arrange
			var customApi = new GlobalNsCustomApi();
			var mockProvider = new MockServiceProvider();
			var outputParameters = new ParameterCollection();
			mockProvider.SetupOutputParameters(outputParameters);

			// Act - before the GlobalNamespace fallback this threw (no ActionWrapper discovered)
			customApi.Execute(mockProvider.ServiceProvider);

			// Assert
			outputParameters.Should().ContainKey("StatusCode");
			outputParameters["StatusCode"].Should().Be(200);
		}
	}
}
