namespace XrmPluginCore.Tests.TestCustomApis.TypeSafe;

/// <summary>
/// Service for <see cref="TypeSafeCustomApi"/>. Receives the strongly-typed request and returns the
/// strongly-typed response. Echoes request values into the response so tests can assert that the
/// generated wrapper read InputParameters and wrote OutputParameters correctly.
/// </summary>
public class TypeSafeCustomApiService
{
	public TypeSafeCustomApiResponse Handle(TypeSafeCustomApiRequest request)
	{
		var status = request.Count ?? -1;
		var message = $"{request.EntityLogicalName}:{request.EntityId}";

		return new TypeSafeCustomApiResponse(status, message);
	}
}
