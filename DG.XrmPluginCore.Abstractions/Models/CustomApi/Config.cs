using DG.XrmPluginCore.Abstractions.Enums;

namespace DG.XrmPluginCore.Abstractions.Models.CustomApi
{
    public class Config
    {
        public string UniqueName { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public bool IsFunction { get; set; }

        public bool EnabledForWorkflow { get; set; }

        public AllowedCustomProcessingStepType AllowedCustomProcessingStepType { get; set; }

        public BindingType BindingType { get; set; }

        public string BoundEntityLogicalName { get; set; }

        public string PluginType { get; set; }

        public string OwnerId { get; set; }

        public string OwnerType { get; set; }

        public bool IsCustomizable { get; set; }

        public bool IsPrivate { get; set; }

        public string ExecutePrivilegeName { get; set; }

        public string Description { get; set; }
    }
}
