using System.Collections.Generic;
using System.Linq;
using System.Text;
using XrmPluginCore.SourceGenerator.Models;

namespace XrmPluginCore.SourceGenerator.CodeGeneration;

/// <summary>
/// Generates wrapper class source code for plugin step registrations
/// </summary>
internal static class WrapperClassGenerator
{
	/// <summary>
	/// Generates a complete source file containing wrapper classes for a plugin step registration.
	/// Generates PreImage and PostImage wrappers, and ActionWrapper for the new method reference API.
	/// </summary>
	public static string GenerateWrapperClasses(PluginStepMetadata metadata)
	{
		var imagesWithAttributes = metadata.Images.Where(i => i.Attributes.Any()).ToList();

		// Estimate capacity: ~500 chars per image wrapper class + ~300 for ActionWrapper
		var estimatedCapacity = (imagesWithAttributes.Count * 500) + 500;
		var sb = new StringBuilder(estimatedCapacity);

		// File header and using directives
		sb.Append(GetFileHeader());

		var namespaceToUse = metadata.RegistrationNamespace;

		// Namespace declaration
		sb.AppendLine($"namespace {namespaceToUse}");
		sb.AppendLine("{");

		// Generate Image wrapper classes if we have images with attributes
		foreach (var image in imagesWithAttributes)
		{
			GenerateImageWrapperClass(sb, metadata, image);
		}

		GenerateActionWrapperClass(sb, metadata, imagesWithAttributes);

		// Close namespace
		sb.AppendLine("}");

		return sb.ToString();
	}

	/// <summary>
	/// Generates an Image wrapper class (PreImage or PostImage)
	/// </summary>
	private static void GenerateImageWrapperClass(StringBuilder sb, PluginStepMetadata metadata, ImageMetadata image)
	{
		var className = image.WrapperClassName;

		// Class header with documentation, attribute, field, and constructor
		sb.Append(GetImageClassHeader(
			className,
			metadata.EntityTypeName,
			metadata.EventOperation,
			metadata.ExecutionStage,
			image.ImageType));

		// Generate properties for each image attribute
		foreach (var attr in image.Attributes)
		{
			sb.Append(GetPropertyTemplate(attr.TypeName, attr.PropertyName, attr.LogicalName));
		}

		// Class footer with ToEntity and GetUnderlyingEntity methods
		sb.Append(GetImageClassFooter());
	}

	/// <summary>
	/// Generates the ActionWrapper class that wraps the service method call.
	/// This is used by the runtime to discover and invoke the plugin action.
	/// </summary>
	private static void GenerateActionWrapperClass(StringBuilder sb, PluginStepMetadata metadata, List<ImageMetadata> images)
	{
		var hasPreImage = images.Any(i => i.ImageType == "PreImage");
		var hasPostImage = images.Any(i => i.ImageType == "PostImage");

		// ActionWrapper header with documentation, class declaration, and service retrieval
		sb.Append(GetActionWrapperHeader(
			metadata.ServiceTypeName,
			metadata.HandlerMethodName,
			metadata.ServiceTypeFullName));

		// Get context if images are needed
		if (hasPreImage || hasPostImage)
		{
			sb.AppendLine();
			sb.Append(GetContextRetrieval());
		}

		var args = new List<string>();
		if (hasPreImage)
		{
			sb.AppendLine();
			sb.Append(GetPreImageRetrieval());
			args.Add("preImage");
		}
		if (hasPostImage)
		{
			sb.AppendLine();
			sb.Append(GetPostImageRetrieval());
			args.Add("postImage");
		}

		var argsString = string.Join(", ", args);

		// ActionWrapper footer with method invocation and closing braces
		sb.AppendLine();
		sb.Append(GetActionWrapperFooter(metadata.HandlerMethodName, argsString));
	}

	/// <summary>
	/// Generates a unique hint name for the source file
	/// </summary>
	public static string GenerateHintName(PluginStepMetadata metadata)
	{
		// UniqueId already contains PluginClassName, so no need to duplicate
		return $"{metadata.UniqueId}.g.cs";
	}

	/// <summary>
	/// Merges multiple metadata instances that represent the same registration but with different attributes
	/// This handles the edge case where the same entity/operation/stage is registered multiple times
	/// </summary>
	public static PluginStepMetadata MergeMetadata(IEnumerable<PluginStepMetadata> metadataList)
	{
		var list = metadataList.ToList();
		if (!list.Any())
			return null;
		if (list.Count == 1)
			return list[0];

		var merged = new PluginStepMetadata
		{
			EntityTypeName = list[0].EntityTypeName,
			EventOperation = list[0].EventOperation,
			ExecutionStage = list[0].ExecutionStage,
			Namespace = list[0].Namespace,
			PluginClassName = list[0].PluginClassName,
			ServiceTypeName = list[0].ServiceTypeName,
			ServiceTypeFullName = list[0].ServiceTypeFullName,
			HandlerMethodName = list[0].HandlerMethodName,
			Images = []
		};

		// Merge all images (remove duplicates)
		var allImages = list.SelectMany(m => m.Images)
			.GroupBy(i => new { i.ImageType, i.ImageName })
			.Select(g =>
			{
				var first = g.First();
				return new ImageMetadata
				{
					ImageType = first.ImageType,
					ImageName = first.ImageName,
					Attributes = [.. g.SelectMany(i => i.Attributes)
							.GroupBy(a => a.LogicalName)
							.Select(ag => ag.First())]
				};
			})
			.ToList();

		merged.Images.AddRange(allImages);

		return merged;
	}

	#region Template Methods

	private static string GetFileHeader() =>
"""
// <auto-generated />

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xrm.Sdk;
using Microsoft.Extensions.DependencyInjection;
using XrmPluginCore;

""";

	private static string GetImageClassHeader(
		string className,
		string entityTypeName,
		string eventOperation,
		string executionStage,
		string imageType) =>
$$"""
    /// <summary>
    /// Type-safe wrapper for {{entityTypeName}} {{eventOperation}} {{executionStage}} {{imageType}}
    /// </summary>
    [CompilerGenerated]
    public class {{className}} : IEntityImageWrapper
    {
        private readonly Entity entity;

        /// <summary>
        /// Initializes a new instance of {{className}}
        /// </summary>
        /// <param name="entity">The image entity</param>
        public {{className}}(Entity entity)
        {
            this.entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

""";

	private static string GetPropertyTemplate(string propertyType, string propertyName, string logicalName) =>
$$"""
        /// <summary>
        /// Gets the {{propertyName}} attribute
        /// </summary>
        public {{propertyType}} {{propertyName}} => entity.GetAttributeValue<{{propertyType}}>("{{logicalName}}");

""";

	private static string GetImageClassFooter() =>
"""
        /// <summary>
        /// Converts the underlying Entity to an early-bound entity type
        /// </summary>
        /// <typeparam name="T">The early-bound entity type</typeparam>
        public T ToEntity<T>() where T : Entity => entity.ToEntity<T>();

        /// <summary>
        /// Gets the underlying Entity object for direct attribute access or service operations
        /// </summary>
        public Entity GetUnderlyingEntity() => entity;
    }

""";

	private static string GetActionWrapperHeader(string serviceTypeName, string methodName, string serviceFullName) =>
$$"""
    /// <summary>
    /// Generated action wrapper for {{serviceTypeName}}.{{methodName}}
    /// </summary>
    [CompilerGenerated]
    internal sealed class ActionWrapper : IActionWrapper
    {
        /// <summary>
        /// Creates the action delegate that invokes the service method with appropriate images.
        /// </summary>
        public Action<IExtendedServiceProvider> CreateAction()
        {
            return serviceProvider =>
            {
                var service = serviceProvider.GetRequiredService<{{serviceFullName}}>();
""";

	private static string GetContextRetrieval() =>
"""
                var context = serviceProvider.GetRequiredService<IPluginExecutionContext>();
""";

	private static string GetPreImageRetrieval() =>
"""
                var preImageEntity = context?.PreEntityImages?.Values?.FirstOrDefault();
                var preImage = preImageEntity != null ? new PreImage(preImageEntity) : null;
""";

	private static string GetPostImageRetrieval() =>
"""
                var postImageEntity = context?.PostEntityImages?.Values?.FirstOrDefault();
                var postImage = postImageEntity != null ? new PostImage(postImageEntity) : null;
""";

	private static string GetActionWrapperFooter(string methodName, string argsString) =>
$$"""
                service.{{methodName}}({{argsString}});
            };
        }
    }

""";

	#endregion
}
