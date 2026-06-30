using System.Collections.Generic;

namespace XrmPluginCore.SourceGenerator.Helpers;

/// <summary>
/// Maps <c>XrmPluginCore.Enums.CustomApiParameterType</c> values to the CLR type used for the
/// generated request/response properties.
/// </summary>
internal static class CustomApiParameterTypeMapper
{
	private static readonly Dictionary<string, string> TypeByName = new()
	{
		["Boolean"] = "bool",
		["DateTime"] = "System.DateTime",
		["Decimal"] = "decimal",
		["Entity"] = "Microsoft.Xrm.Sdk.Entity",
		["EntityCollection"] = "Microsoft.Xrm.Sdk.EntityCollection",
		["EntityReference"] = "Microsoft.Xrm.Sdk.EntityReference",
		["Float"] = "double",
		["Integer"] = "int",
		["Money"] = "Microsoft.Xrm.Sdk.Money",
		["Picklist"] = "Microsoft.Xrm.Sdk.OptionSetValue",
		["String"] = "string",
		["StringArray"] = "string[]",
		["Guid"] = "System.Guid",
	};

	// Index matches the underlying integer value of CustomApiParameterType, used as a fallback when
	// the argument is a constant integer rather than a CustomApiParameterType.X member access.
	private static readonly string[] NameByValue =
	[
		"Boolean", "DateTime", "Decimal", "Entity", "EntityCollection", "EntityReference",
		"Float", "Integer", "Money", "Picklist", "String", "StringArray", "Guid",
	];

	/// <summary>
	/// Value types (need a trailing <c>?</c> when the corresponding request parameter is optional).
	/// </summary>
	private static readonly HashSet<string> ValueTypeNames =
	[
		"Boolean", "DateTime", "Decimal", "Float", "Integer", "Guid",
	];

	public static string GetClrType(string parameterTypeName)
		=> parameterTypeName != null && TypeByName.TryGetValue(parameterTypeName, out var clr) ? clr : "object";

	public static string GetNameForValue(int value)
		=> value >= 0 && value < NameByValue.Length ? NameByValue[value] : null;

	public static bool IsValueType(string parameterTypeName)
		=> parameterTypeName != null && ValueTypeNames.Contains(parameterTypeName);
}
