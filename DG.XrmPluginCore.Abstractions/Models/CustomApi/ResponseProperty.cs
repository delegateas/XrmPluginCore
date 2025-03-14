namespace DG.XrmPluginCore.Abstractions.Models.CustomApi
{
    public class ResponseProperty : Parameter
    {
        public ResponseProperty(
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
    }
}
