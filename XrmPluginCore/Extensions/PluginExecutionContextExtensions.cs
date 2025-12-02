using XrmPluginCore.Enums;
using XrmPluginCore.Helpers;
using Microsoft.Xrm.Sdk;

namespace XrmPluginCore.Extensions;

public static class PluginExecutionContextExtensions
{
	public static T GetEntity<T>(this IPluginExecutionContext context, ITracingService trace = null) where T : Entity, new()
	{
		if (!context.InputParameters.Contains("Target"))
		{
			trace?.Trace("Context does not contain 'Target'");
			return null;
		}

		var target = context.InputParameters["Target"];
		if (target is Entity entity)
		{
			var logicalName = EntityLogicalNameCache.GetLogicalName<T>();
			if (logicalName != entity.LogicalName)
			{
				trace?.Trace("'Entity' is not of specified type: {0} vs. {1}",
					entity.LogicalName, logicalName);
				return null;
			}

			return entity.ToEntity<T>();
		}

		var typeName = target.GetType().Name;
		trace?.Trace("'Target' is not an Entity. It's of type: {0}", typeName);
		return null;
	}

	public static T GetImage<T>(this IPluginExecutionContext context, ImageType imageType, string name) where T : Entity
	{
		EntityImageCollection collection = null;
		if (imageType == ImageType.PreImage)
		{
			collection = context.PreEntityImages;
		}
		else if (imageType == ImageType.PostImage)
		{
			collection = context.PostEntityImages;
		}

		if (collection != null && collection.TryGetValue(name, out var entity))
		{
			return entity.ToEntity<T>();
		}
		else
		{
			return null;
		}
	}

	public static T GetImage<T>(this IPluginExecutionContext context, ImageType imageType) where T : Entity
	{
		return context.GetImage<T>(imageType, imageType.ToString());
	}

	public static T GetPreImage<T>(this IPluginExecutionContext context, string name = "PreImage") where T : Entity
	{
		return context.GetImage<T>(ImageType.PreImage, name);
	}

	public static T GetPostImage<T>(this IPluginExecutionContext context, string name = "PostImage") where T : Entity
	{
		return context.GetImage<T>(ImageType.PostImage, name);
	}
}
