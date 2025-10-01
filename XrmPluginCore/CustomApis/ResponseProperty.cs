using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.CustomApi;

namespace XrmPluginCore.CustomApis
{
    public class ResponseProperty : IResponseProperty
    {
        internal ResponseProperty(
            string name,
            string uniqueName,
            string displayName,
            string description,
            bool isCustomizable,
            string logicalEntityName,
            CustomApiParameterType type)
        {
            Name = name;
            UniqueName = uniqueName;
            DisplayName = displayName;
            Description = description;
            IsCustomizable = isCustomizable;
            LogicalEntityName = logicalEntityName;
            Type = type;
        }

        internal ResponseProperty(IResponseProperty responseProperty)
        {
            Name = responseProperty.Name;
            UniqueName = responseProperty.UniqueName;
            DisplayName = responseProperty.DisplayName;
            Description = responseProperty.Description;
            IsCustomizable = responseProperty.IsCustomizable;
            LogicalEntityName = responseProperty.LogicalEntityName;
            Type = responseProperty.Type;
        }

        public string Name { get; internal set; }

        public string UniqueName { get; }

        public string DisplayName { get; internal set; }

        public string Description { get; }

        public bool IsCustomizable { get; }

        public string LogicalEntityName { get; }

        public CustomApiParameterType Type { get; }
    }
}
