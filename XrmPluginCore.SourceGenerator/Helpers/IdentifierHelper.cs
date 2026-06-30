using System.Text;

namespace XrmPluginCore.SourceGenerator.Helpers;

/// <summary>
/// Sanitizes arbitrary strings into valid C# identifiers.
/// <para>
/// The exact same rule is duplicated in the runtime
/// (XrmPluginCore.Helpers.IdentifierSanitizer) so that the runtime can discover the generated
/// ActionWrapper type by name. Keep the two implementations in sync.
/// </para>
/// </summary>
internal static class IdentifierHelper
{
	public static string Sanitize(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "_";
		}

		var sb = new StringBuilder(value.Length + 1);
		foreach (var c in value)
		{
			// Keep letters, digits and underscores; replace anything else. Digits are kept here (even
			// at the start) so they are preserved rather than collapsed - the leading-digit case is
			// handled by the prefix below.
			sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
		}

		// An identifier cannot start with a digit; prefix with '_' so the original digit is preserved.
		if (char.IsDigit(value[0]))
		{
			sb.Insert(0, '_');
		}

		return sb.ToString();
	}

	/// <summary>
	/// Returns true when <paramref name="value"/> is already a valid C# identifier
	/// (and therefore needs no sanitization).
	/// </summary>
	public static bool IsValidIdentifier(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}

		if (char.IsDigit(value[0]))
		{
			return false;
		}

		foreach (var c in value)
		{
			if (!char.IsLetterOrDigit(c) && c != '_')
			{
				return false;
			}
		}

		return true;
	}
}
