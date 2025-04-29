using DG.XrmPluginCore.Enums;
using System.Collections.Generic;

namespace DG.XrmPluginCore.Interfaces.CustomApi
{
    public interface ICustomApiConfig
    {
        AllowedCustomProcessingStepType AllowedCustomProcessingStepType { get; }
        BindingType BindingType { get; }
        string BoundEntityLogicalName { get; }
        string Description { get; }
        string DisplayName { get; }
        bool EnabledForWorkflow { get; }
        string ExecutePrivilegeName { get; }
        bool IsCustomizable { get; }
        bool IsFunction { get; }
        bool IsPrivate { get; }
        string Name { get; }
        string OwnerId { get; }
        string OwnerType { get; }
        string PluginType { get; }
        string UniqueName { get; }

        IEnumerable<IRequestParameter> RequestParameters { get; }

        IEnumerable<IResponseProperty> ResponseParameters { get; }
    }
}