using DG.XrmPluginCore.Abstractions.Enums;
using System;

namespace DG.XrmPluginCore.Abstractions.Models
{
    public class ExtendedStepConfig
    {
        public ExtendedStepConfig(Deployment deployment, ExecutionMode executionMode, string name, int executionOrder, string filteredAttributes, Guid? impersonatingUserId)
        {
            Deployment = deployment;
            ExecutionMode = executionMode;
            Name = name;
            ExecutionOrder = executionOrder;
            FilteredAttributes = filteredAttributes;
            ImpersonatingUserId = impersonatingUserId;
        }

        public Deployment Deployment { get; set; }
        public ExecutionMode ExecutionMode { get; set; }
        public string Name { get; set; }
        public int ExecutionOrder { get; set; }
        public string FilteredAttributes { get; set; }
        public Guid? ImpersonatingUserId { get; set; }
    }
}
