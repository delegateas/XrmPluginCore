using DG.XrmPluginCore.Abstractions.Enums;

namespace DG.XrmPluginCore.Abstractions.Models
{
    public class StepConfig
    {
        public StepConfig(string className, ExecutionStage executionStage, EventOperation eventOperation, string entityLogicalName)
        {
            ClassName = className;
            ExecutionStage = executionStage;
            EventOperation = eventOperation;
            EntityLogicalName = entityLogicalName;
        }

        public string ClassName { get; set; }
        public ExecutionStage ExecutionStage { get; set; }
        public EventOperation EventOperation { get; set; }
        public string EntityLogicalName { get; set; }
    }
}
