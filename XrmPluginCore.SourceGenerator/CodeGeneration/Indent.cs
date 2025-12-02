namespace XrmPluginCore.SourceGenerator.CodeGeneration;

/// <summary>
/// Provides centralized indentation for generated code.
/// </summary>
internal static class Indent
{
	private const string Tab = "\t";

	/// <summary>Class level indentation (1 tab)</summary>
	public static readonly string L1 = Tab;

	/// <summary>Member level indentation (2 tabs)</summary>
	public static readonly string L2 = Tab + Tab;

	/// <summary>Body level indentation (3 tabs)</summary>
	public static readonly string L3 = Tab + Tab + Tab;

	/// <summary>Deep body level indentation (4 tabs)</summary>
	public static readonly string L4 = Tab + Tab + Tab + Tab;
}
