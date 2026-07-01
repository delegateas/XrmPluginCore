using System;
using XrmPluginCore.Enums;

namespace XrmPluginCore.Helpers;

internal static class PrivilegeNameResolver
{
	/// <summary>
	/// Builds the Dataverse execute-privilege name for the given <paramref name="privilege"/> on the
	/// supplied entity, following the <c>prv{Privilege}{EntitySchemaName}</c> convention.
	/// For example, <see cref="Privilege.Read"/> on <c>Account</c> resolves to <c>prvReadAccount</c>.
	/// </summary>
	/// <remarks>
	/// Privilege names use the entity <em>schema</em> name (e.g. <c>Account</c>), not the logical name
	/// (<c>account</c>): the platform stores the logical name as the lowercased schema name, so the
	/// original casing must come from the schema name. Callers pass it via the early-bound type name.
	/// </remarks>
	public static string GetExecutePrivilegeName(Privilege privilege, string entitySchemaName)
	{
		return $"prv{GetPrivilegeVerb(privilege)}{entitySchemaName}";
	}

	private static string GetPrivilegeVerb(Privilege privilege)
	{
		switch (privilege)
		{
			case Privilege.Create: return "Create";
			case Privilege.Read: return "Read";
			case Privilege.Write: return "Write";
			case Privilege.Delete: return "Delete";
			case Privilege.Append: return "Append";
			case Privilege.AppendTo: return "AppendTo";
			case Privilege.Assign: return "Assign";
			case Privilege.Share: return "Share";
			default:
				throw new ArgumentOutOfRangeException(nameof(privilege), privilege, "Unknown privilege.");
		}
	}
}
