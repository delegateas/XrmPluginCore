using Microsoft.CodeAnalysis;

namespace XrmPluginCore.SourceGenerator.Helpers;

/// <summary>
/// Helpers for emitting nullable annotations in generated code in a way that stays backwards compatible
/// with consumers that have not enabled nullable reference types (including .NET Framework / C# 7.3,
/// where a nullable-reference <c>?</c> or a <c>#nullable</c> directive is a compile error).
/// <para>
/// Nullable <b>value</b> types (<c>int?</c>) are always safe and are never affected by this gate.
/// Only nullable <b>reference</b> annotations (<c>string?</c>) and the <c>#nullable enable</c> directive
/// are gated on the consuming compilation having NRT annotations enabled.
/// </para>
/// </summary>
internal static class NullableHelper
{
	private static readonly SymbolDisplayFormat NonNullableReferenceFormat =
		SymbolDisplayFormat.CSharpErrorMessageFormat.WithMiscellaneousOptions(
			SymbolDisplayFormat.CSharpErrorMessageFormat.MiscellaneousOptions
			& ~SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

	/// <summary>
	/// True when the compilation has nullable reference-type annotations enabled
	/// (<c>&lt;Nullable&gt;annotations&lt;/Nullable&gt;</c> or <c>enable</c>), which also implies C# 8+.
	/// </summary>
	public static bool AnnotationsEnabled(Compilation compilation)
		=> (compilation.Options.NullableContextOptions & NullableContextOptions.Annotations) == NullableContextOptions.Annotations;

	/// <summary>
	/// Renders a type for use in generated code. When NRT annotations are disabled, strips the
	/// nullable-reference <c>?</c> (so the output compiles without a <c>#nullable</c> context) while
	/// keeping nullable value-type <c>?</c> intact.
	/// </summary>
	public static string DisplayType(ITypeSymbol type, bool nullableEnabled)
		=> nullableEnabled ? type.ToDisplayString() : type.ToDisplayString(NonNullableReferenceFormat);

	/// <summary>
	/// The <c>#nullable enable</c> directive line to place in a generated file, or an empty string when
	/// NRT annotations are not enabled (so no directive is emitted on C# 7.3 / NRT-off consumers).
	/// </summary>
	public static string FileDirective(bool nullableEnabled)
		=> nullableEnabled ? "#nullable enable\n" : string.Empty;
}
