using Microsoft.Xrm.Sdk;
using XrmPluginCore.Enums;

namespace XrmPluginCore.Extensions
{
    public static class LocalPluginContextExtensions
    {
        public static T GetEntity<T>(this LocalPluginContext context) where T : Entity
        {
            return context.PluginExecutionContext.GetEntity<T>(context.TracingService);
        }

        public static T GetImage<T>(this LocalPluginContext context, ImageType imageType, string name) where T : Entity
        {
            return context.PluginExecutionContext.GetImage<T>(imageType, name);
        }

        public static T GetImage<T>(this LocalPluginContext context, ImageType imageType) where T : Entity
        {
            return GetImage<T>(context, imageType, imageType.ToString());
        }

        public static T GetPreImage<T>(this LocalPluginContext context, string name = "PreImage") where T : Entity
        {
            return GetImage<T>(context, ImageType.PreImage, name);
        }

        public static T GetPostImage<T>(this LocalPluginContext context, string name = "PostImage") where T : Entity
        {
            return GetImage<T>(context, ImageType.PostImage, name);
        }
    }
}
