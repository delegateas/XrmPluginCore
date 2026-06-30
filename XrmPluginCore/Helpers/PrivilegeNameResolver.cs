using System;
using XrmPluginCore.Enums;

namespace XrmPluginCore.Helpers;

internal static class PrivilegeNameResolver
{
	/// <summary>
	/// Builds the Dataverse execute-privilege name for the given <paramref name="privilege"/> on the
	/// supplied entity, following the <c>prv{Privilege}{EntityLogicalName}</c> convention.
	/// For example, <see cref="Privilege.Read"/> on <c>account</c> resolves to <c>prvReadaccount</c>.
	/// </summary>
	/// <remarks>
	/// Privilege names predate the schema name concept and are based on the entity logical name
	/// (e.g. <c>account</c>), so the logical name is used verbatim.
	/// </remarks>
	public static string GetExecutePrivilegeName(Privilege privilege, string entityLogicalName)
	{
		return $"prv{GetPrivilegeVerb(privilege)}{entityLogicalName}";
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
