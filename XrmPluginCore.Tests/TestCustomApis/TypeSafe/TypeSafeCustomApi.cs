using Microsoft.Extensions.DependencyInjection;
using XrmPluginCore.Enums;

namespace XrmPluginCore.Tests.TestCustomApis.TypeSafe;

/// <summary>
/// Test Custom API using the type-safe API. The Request/Response classes
/// (TypeSafeCustomApiRequest/TypeSafeCustomApiResponse) and the ActionWrapper are emitted by the
/// source generator from the AddRequestParameter/AddResponseProperty declarations below.
/// </summary>
public class TypeSafeCustomApi : Plugin
{
	public TypeSafeCustomApi()
	{
		RegisterAPI<TypeSafeCustomApiService>(nameof(TypeSafeCustomApi), nameof(TypeSafeCustomApiService.Handle))
			.AddRequestParameter("EntityLogicalName", CustomApiParameterType.String)
			.AddRequestParameter("EntityId", CustomApiParameterType.Guid)
			.AddRequestParameter("Count", CustomApiParameterType.Integer, isOptional: true)
			.AddResponseProperty("StatusCode", CustomApiParameterType.Integer)
			.AddResponseProperty("ErrorMessage", CustomApiParameterType.String);
	}

	protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
	{
		services.AddScoped<TypeSafeCustomApiService>();
		return base.OnBeforeBuildServiceProvider(services);
	}
}
