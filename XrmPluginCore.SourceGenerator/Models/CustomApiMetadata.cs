using System.Collections.Generic;
using System.Linq;
using XrmPluginCore.SourceGenerator.Helpers;

namespace XrmPluginCore.SourceGenerator.Models;

/// <summary>
/// Represents metadata about a type-safe Custom API registration
/// (<c>RegisterAPI&lt;TService&gt;(name, handlerMethodName)</c>).
/// </summary>
internal sealed class CustomApiMetadata
{
	public string ApiName { get; set; }
	public string Namespace { get; set; }
	public string PluginClassName { get; set; }

	public string ServiceTypeName { get; set; }
	public string ServiceTypeFullName { get; set; }
	public string HandlerMethodName { get; set; }

	/// <summary>Whether the consuming compilation has nullable reference-type annotations enabled.</summary>
	public bool NullableAnnotationsEnabled { get; set; }

	public List<CustomApiParameterMetadata> RequestParameters { get; set; } = [];
	public List<CustomApiParameterMetadata> ResponseProperties { get; set; } = [];

	/// <summary>
	/// Diagnostics to report for this registration. Not included in equality comparison.
	/// </summary>
	public List<DiagnosticInfo> Diagnostics { get; set; } = [];

	/// <summary>The API name sanitized into a valid C# identifier, used to name generated classes.</summary>
	public string SanitizedApiName => IdentifierHelper.Sanitize(ApiName);

	public string RequestClassName => $"{SanitizedApiName}{Constants.RequestClassSuffix}";
	public string ResponseClassName => $"{SanitizedApiName}{Constants.ResponseClassSuffix}";
	public string ActionWrapperClassName => $"{SanitizedApiName}{Constants.ActionWrapperClassSuffix}";

	public bool HasRequest => RequestParameters.Any();
	public bool HasResponse => ResponseProperties.Any();

	/// <summary>
	/// Gets a unique identifier for the generated source file.
	/// </summary>
	public string UniqueId =>
		$"{Namespace?.Replace(".", "_")}_{PluginClassName}_{SanitizedApiName}_CustomApi";

	public override bool Equals(object obj)
	{
		if (obj is CustomApiMetadata other)
		{
			return ApiName == other.ApiName
				&& Namespace == other.Namespace
				&& PluginClassName == other.PluginClassName
				&& ServiceTypeName == other.ServiceTypeName
				&& ServiceTypeFullName == other.ServiceTypeFullName
				&& HandlerMethodName == other.HandlerMethodName
				&& NullableAnnotationsEnabled == other.NullableAnnotationsEnabled
				&& RequestParameters.SequenceEqual(other.RequestParameters)
				&& ResponseProperties.SequenceEqual(other.ResponseProperties);
		}
		return false;
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hash = 17;
			hash = (hash * 31) + (ApiName?.GetHashCode() ?? 0);
			hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
			hash = (hash * 31) + (PluginClassName?.GetHashCode() ?? 0);
			hash = (hash * 31) + (ServiceTypeName?.GetHashCode() ?? 0);
			hash = (hash * 31) + (ServiceTypeFullName?.GetHashCode() ?? 0);
			hash = (hash * 31) + (HandlerMethodName?.GetHashCode() ?? 0);
			hash = (hash * 31) + NullableAnnotationsEnabled.GetHashCode();
			foreach (var p in RequestParameters)
			{
				hash = (hash * 31) + p.GetHashCode();
			}
			foreach (var p in ResponseProperties)
			{
				hash = (hash * 31) + p.GetHashCode();
			}
			return hash;
		}
	}
}

/// <summary>
/// Represents a single Custom API request parameter or response property.
/// </summary>
internal sealed class CustomApiParameterMetadata
{
	/// <summary>The declared unique name (the InputParameters/OutputParameters dictionary key).</summary>
	public string UniqueName { get; set; }

	/// <summary>The generated C# property name (sanitized <see cref="UniqueName"/>).</summary>
	public string PropertyName { get; set; }

	/// <summary>The <c>CustomApiParameterType</c> member name (e.g. "String", "Guid").</summary>
	public string ParameterType { get; set; }

	/// <summary>The CLR type used for the generated property (already nullable when optional).</summary>
	public string ClrType { get; set; }

	/// <summary>Whether the request parameter is optional (response properties are always false).</summary>
	public bool IsOptional { get; set; }

	public override bool Equals(object obj)
	{
		if (obj is CustomApiParameterMetadata other)
		{
			return UniqueName == other.UniqueName
				&& PropertyName == other.PropertyName
				&& ParameterType == other.ParameterType
				&& ClrType == other.ClrType
				&& IsOptional == other.IsOptional;
		}
		return false;
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hash = 17;
			hash = (hash * 31) + (UniqueName?.GetHashCode() ?? 0);
			hash = (hash * 31) + (PropertyName?.GetHashCode() ?? 0);
			hash = (hash * 31) + (ParameterType?.GetHashCode() ?? 0);
			hash = (hash * 31) + (ClrType?.GetHashCode() ?? 0);
			hash = (hash * 31) + IsOptional.GetHashCode();
			return hash;
		}
	}
}
