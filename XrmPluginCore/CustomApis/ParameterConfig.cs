using DG.XrmPluginCore.Abstractions.Models;

namespace DG.XrmPluginCore.CustomApis
{
    public abstract class ParameterConfig : ICustomApiParameter
    {
        public string Name { get; internal set; }
        public string UniqueName { get; internal set;}
        public string DisplayName { get; internal set;}
        public string Description { get; internal set; }
        public bool IsCustomizable { get; internal set;}
        public string LogicalEntityName { get; internal set;}
        public CustomApiParameterType Type { get; internal set;}

        protected abstract string Direction { get; }

        protected ParameterConfig() { }

        protected ParameterConfig(string apiName, string uniqueName, string displayName, string description, bool isCustomizable, string logicalEntityName, CustomApiParameterType type)
        {
            Name = CalculatedName(apiName);
            UniqueName = uniqueName;
            DisplayName = displayName;
            Description = description ?? $"{type} {Direction} parameter {uniqueName}";
            IsCustomizable = isCustomizable;
            LogicalEntityName = logicalEntityName;
            Type = type;
        }

        public void SetNameFromAPI(string apiName)
        {
            var name = CalculatedName(apiName);
            Name = name;
            DisplayName = name;
        }

        protected string CalculatedName(string apiName) => $"{apiName}-{Direction}-{UniqueName}";
    }
}
