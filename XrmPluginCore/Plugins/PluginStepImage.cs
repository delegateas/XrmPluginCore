using XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using System.Linq.Expressions;
using XrmPluginCore.Extensions;

namespace XrmPluginCore.Plugins
{
    /// <summary>
    /// Container for information about images attached to steps
    /// </summary>
    public class PluginStepImage : ImageSpecification
    {
        public static PluginStepImage Create<T>(string name,
            string entityAlias,
            ImageType imageType,
            Expression<Func<T, object>>[] attributes)
            where T : Entity =>
                new PluginStepImage(
                    name,
                    entityAlias,
                    imageType,
                    attributes?.Select(x => x.GetMemberName()).ToArray()
                );

        public PluginStepImage(
            string name,
            string entityAlias,
            ImageType imageType,
            string attributes) : base(name, entityAlias, imageType, attributes) { }

        public PluginStepImage(
            string name,
            string entityAlias,
            ImageType imageType,
            string[] attributes) : base(
                name,
                entityAlias,
                imageType,
                string.Join(",", attributes ?? Array.Empty<string>()).ToLower()
            )
        { }
    }
}
