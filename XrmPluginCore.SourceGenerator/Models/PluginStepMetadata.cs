using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace XrmPluginCore.SourceGenerator.Models;

/// <summary>
/// Represents metadata about a plugin step registration that includes filtered attributes
/// </summary>
internal sealed class PluginStepMetadata
{
	public string EntityTypeName { get; set; }
	public string EventOperation { get; set; }
	public string ExecutionStage { get; set; }
	public List<ImageMetadata> Images { get; set; } = [];
	public string Namespace { get; set; }
	public string PluginClassName { get; set; }
	public bool HasExecuteCall { get; set; }

	/// <summary>
	/// Source location for diagnostic reporting. Not included in equality comparison.
	/// </summary>
	public Location Location { get; set; }

	/// <summary>
	/// Diagnostics to report for this plugin step. Not included in equality comparison.
	/// </summary>
	public List<DiagnosticInfo> Diagnostics { get; set; } = [];

	/// <summary>
	/// Gets the namespace for generated image wrapper classes.
	/// Format: {OriginalNamespace}.PluginImages.{PluginClassName}.{Entity}{Op}{Stage}
	/// </summary>
	public string ImageNamespace =>
		$"{Namespace}.PluginImages.{PluginClassName}.{EntityTypeName}{EventOperation}{ExecutionStage}";

	/// <summary>
	/// Gets a unique identifier for this registration.
	/// Includes plugin class name to differentiate multiple registrations for the same entity/operation/stage.
	/// </summary>
	public string UniqueId =>
		$"{PluginClassName}_{EntityTypeName}_{EventOperation}_{ExecutionStage}";

	public override bool Equals(object obj)
	{
		if (obj is PluginStepMetadata other)
		{
			return PluginClassName == other.PluginClassName
				&& EntityTypeName == other.EntityTypeName
				&& EventOperation == other.EventOperation
				&& ExecutionStage == other.ExecutionStage
				&& Images.SequenceEqual(other.Images)
				&& Namespace == other.Namespace;
		}
		return false;
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hash = 17;
			hash = (hash * 31) + (PluginClassName?.GetHashCode() ?? 0);
			hash = (hash * 31) + (EntityTypeName?.GetHashCode() ?? 0);
			hash = (hash * 31) + (EventOperation?.GetHashCode() ?? 0);
			hash = (hash * 31) + (ExecutionStage?.GetHashCode() ?? 0);
			hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
			foreach (var img in Images)
			{
				hash = (hash * 31) + img.GetHashCode();
			}
			return hash;
		}
	}
}

/// <summary>
/// Represents metadata about an entity attribute
/// </summary>
internal sealed class AttributeMetadata
{
	public string PropertyName { get; set; }
	public string LogicalName { get; set; }
	public string TypeName { get; set; }

	public override bool Equals(object obj)
	{
		if (obj is AttributeMetadata other)
		{
			return PropertyName == other.PropertyName
				&& LogicalName == other.LogicalName
				&& TypeName == other.TypeName;
		}
		return false;
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hash = 17;
			hash = (hash * 31) + (PropertyName?.GetHashCode() ?? 0);
			hash = (hash * 31) + (LogicalName?.GetHashCode() ?? 0);
			hash = (hash * 31) + (TypeName?.GetHashCode() ?? 0);
			return hash;
		}
	}
}

/// <summary>
/// Represents metadata about a plugin step image (PreImage or PostImage)
/// </summary>
internal sealed class ImageMetadata
{
	public string ImageType { get; set; } // "PreImage" or "PostImage"
	public string ImageName { get; set; }
	public List<AttributeMetadata> Attributes { get; set; } = [];

	/// <summary>
	/// Gets the generated wrapper class name for this image.
	/// Simply "PreImage" or "PostImage" - namespace provides isolation.
	/// </summary>
	public string WrapperClassName => ImageType;

	public override bool Equals(object obj)
	{
		if (obj is ImageMetadata other)
		{
			return ImageType == other.ImageType
				&& ImageName == other.ImageName
				&& Attributes.SequenceEqual(other.Attributes);
		}
		return false;
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hash = 17;
			hash = (hash * 31) + (ImageType?.GetHashCode() ?? 0);
			hash = (hash * 31) + (ImageName?.GetHashCode() ?? 0);
			foreach (var attr in Attributes)
			{
				hash = (hash * 31) + attr.GetHashCode();
			}
			return hash;
		}
	}
}

/// <summary>
/// Represents a diagnostic to be reported during source generation
/// </summary>
internal sealed class DiagnosticInfo
{
	public DiagnosticDescriptor Descriptor { get; set; }
	public Location Location { get; set; }
	public object[] MessageArgs { get; set; }
}
