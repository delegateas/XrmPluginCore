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

		var sb = new StringBuilder(value.Length);
		for (var i = 0; i < value.Length; i++)
		{
			var c = value[i];
			var isLetterOrUnderscore = char.IsLetter(c) || c == '_';
			var isDigit = char.IsDigit(c);

			if (isLetterOrUnderscore || (isDigit && i > 0))
			{
				sb.Append(c);
			}
			else
			{
				sb.Append('_');
			}
		}

		// An identifier cannot start with a digit
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
