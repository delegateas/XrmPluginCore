using DG.XrmPluginCore.Abstractions.Enums;
using System;

namespace DG.XrmPluginCore.Abstractions.Models.Plugin
{
    public class StepConfig
    {

        public string ClassName { get; set; }
        public ExecutionStage ExecutionStage { get; set; }
        public EventOperation EventOperation { get; set; }
        public string EntityLogicalName { get; set; }
        public Deployment Deployment { get; set; }
        public ExecutionMode ExecutionMode { get; set; }
        public string Name { get; set; }
        public int ExecutionOrder { get; set; }
        public string FilteredAttributes { get; set; }
        public Guid? ImpersonatingUserId { get; set; }

        public StepConfig(
            string className,
            ExecutionStage executionStage,
            EventOperation eventOperation,
            string entityLogicalName,
            Deployment deployment,
            ExecutionMode executionMode,
            string name,
            int executionOrder,
            string filteredAttributes,
            Guid? impersonatingUserId)
        {
            ClassName = className;
            ExecutionStage = executionStage;
            EventOperation = eventOperation;
            EntityLogicalName = entityLogicalName;
            Deployment = deployment;
            ExecutionMode = executionMode;
            Name = name;
            ExecutionOrder = executionOrder;
            FilteredAttributes = filteredAttributes;
            ImpersonatingUserId = impersonatingUserId;
        }
    }
}
