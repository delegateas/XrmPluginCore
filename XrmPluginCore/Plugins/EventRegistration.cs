using System;

namespace DG.XrmPluginCore.Plugins
{
    public class EventRegistration
    {
        public EventRegistration(IPluginStepConfig stepConfig, Action<LocalPluginContext> action)
        {
            StepConfig = stepConfig;
            Action = action;
        }

        public IPluginStepConfig StepConfig { get; set; }
        public Action<LocalPluginContext> Action { get; set; }
    }
}
