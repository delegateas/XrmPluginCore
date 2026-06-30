using Microsoft.Extensions.DependencyInjection;
using XrmPluginCore.Enums;

namespace XrmPluginCore.Tests.TestCustomApis.TypeSafe;

/// <summary>
/// Custom API whose name starts with a digit ("1DigitApi"). Exercises the identifier sanitizer end to
/// end: the runtime (wrapper discovery) and the generator (class emission) must produce the same
/// "_1DigitApi" prefix, otherwise the generated ActionWrapper is never discovered.
/// </summary>
public class DigitNamedCustomApi : Plugin
{
	public DigitNamedCustomApi()
	{
		RegisterAPI<DigitNamedCustomApiService>("1DigitApi", nameof(DigitNamedCustomApiService.Handle))
			.AddResponseProperty("StatusCode", CustomApiParameterType.Integer);
	}

	protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
	{
		services.AddScoped<DigitNamedCustomApiService>();
		return base.OnBeforeBuildServiceProvider(services);
	}
}

public class DigitNamedCustomApiService
{
	public _1DigitApiResponse Handle() => new _1DigitApiResponse(200);
}
