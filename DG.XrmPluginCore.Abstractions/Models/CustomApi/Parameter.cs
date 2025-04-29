namespace DG.XrmPluginCore.Models.CustomApi
{
    public abstract class Parameter
    {
        public string Name { get; set; }
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public bool IsCustomizable { get; set; }
        public string LogicalEntityName { get; set; }
        public CustomApiParameterType Type { get; set; }
    }
}
