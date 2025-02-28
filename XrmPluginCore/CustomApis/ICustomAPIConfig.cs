namespace DG.XrmPluginCore.CustomApis
{
    using DG.XrmPluginCore.Abstractions.Enums;
    using System.Collections.Generic;

    interface ICustomAPIConfig
    {
        AllowedCustomProcessingStepType AllowedCustomProcessingStepType { get; }
        BindingType BindingType { get; }
        string BoundEntityLogicalName { get; }
        string Description { get; }
        string DisplayName { get; }
        string ExecutePrivilegeName { get; }
        bool IsCustomizable { get; }
        bool IsFunction { get; }
        bool IsPrivate { get; }
        string Name { get; }
        string PluginType { get; }
        string UniqueName { get; }
        bool EnabledForWorkflow { get; }
        IEnumerable<RequestParameterConfig> GetRequestParameters();
        IEnumerable<ResponsePropertyConfig> GetResponseProperties();
    }
}