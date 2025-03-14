namespace DG.XrmPluginCore.Abstractions.Models.CustomApi
{
    public class RequestParameter : Parameter
    {
        public RequestParameter(
            string name,
            string uniqueName,
            string displayName,
            string description,
            bool isCustomizable,
            string logicalEntityName,
            CustomApiParameterType type,
            bool isOptional)
        {
            Name = name;
            UniqueName = uniqueName;
            DisplayName = displayName;
            Description = description;
            IsCustomizable = isCustomizable;
            LogicalEntityName = logicalEntityName;
            Type = type;
            IsOptional = isOptional;
        }

        public bool IsOptional { get; set; }
    }
}
