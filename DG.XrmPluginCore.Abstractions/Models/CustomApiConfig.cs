using DG.XrmPluginCore.Abstractions.Enums;

namespace DG.XrmPluginCore.Abstractions.Models
{
    public class CustomApiConfig
    {
        public CustomApiConfig(string uniqueName, bool isFunction, bool enabledForWorkflow, AllowedCustomProcessingStepType allowedCustomProcessingStepType, BindingType bindingType, string boundEntityLogicalName)
        {
            UniqueName = uniqueName;
            IsFunction = isFunction;
            EnabledForWorkflow = enabledForWorkflow;
            AllowedCustomProcessingStepType = allowedCustomProcessingStepType;
            BindingType = bindingType;
            BoundEntityLogicalName = boundEntityLogicalName;
        }

        public string UniqueName { get; }
        public bool IsFunction { get; }
        public bool EnabledForWorkflow { get; }
        public AllowedCustomProcessingStepType AllowedCustomProcessingStepType { get; }
        public BindingType BindingType { get; }
        public string BoundEntityLogicalName { get; }
    }
}
