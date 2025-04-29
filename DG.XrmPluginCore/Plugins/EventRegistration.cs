using DG.XrmPluginCore.Interfaces.Plugin;
using System;

namespace DG.XrmPluginCore.Plugins
{
    public class EventRegistration
    {
        public EventRegistration(IPluginStepConfigBuilder pluginStepConfig, Action<LocalPluginContext> action)
        {
            ConfigBuilder = pluginStepConfig;
            Action = action;
        }

        public IPluginStepConfigBuilder ConfigBuilder { get; set; }
        public Action<LocalPluginContext> Action { get; set; }
    }
}
