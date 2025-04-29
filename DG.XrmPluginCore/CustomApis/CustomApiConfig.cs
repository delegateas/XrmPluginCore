using DG.XrmPluginCore.Enums;
using System.Collections.Generic;

namespace DG.XrmPluginCore.Interfaces.CustomApi
{
    public class CustomApiConfig : ICustomApiConfig
    {
        public string UniqueName { get; internal set; }

        public string Name { get; internal set; }

        public string DisplayName { get; internal set; }

        public bool IsFunction { get; internal set; }

        public bool EnabledForWorkflow { get; internal set; }

        public AllowedCustomProcessingStepType AllowedCustomProcessingStepType { get; internal set; }

        public BindingType BindingType { get; internal set; }

        public string BoundEntityLogicalName { get; internal set; }

        public string PluginType { get; internal set; }

        public string OwnerId { get; internal set; }

        public string OwnerType { get; internal set; }

        public bool IsCustomizable { get; internal set; }

        public bool IsPrivate { get; internal set; }

        public string ExecutePrivilegeName { get; internal set; }

        public string Description { get; internal set; }

        public IEnumerable<IRequestParameter> RequestParameters { get; internal set; }

        public IEnumerable<IResponseProperty> ResponseProperties { get; internal set; }
    }
}
