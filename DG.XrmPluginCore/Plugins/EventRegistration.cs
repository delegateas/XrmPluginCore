using System;

namespace DG.XrmPluginCore.Plugins
{
    public class EventRegistration
    {
        public EventRegistration(IPluginStepConfigBuilder pluginStepConfig, Action<LocalPluginContext> action)
        {
            ConfigBuilder = pluginStepConfig;
            Action = serviceProvider => action(new LocalPluginContext(serviceProvider));
        }

        public EventRegistration(IPluginStepConfigBuilder pluginStepConfig, Action<IServiceProvider> action)
        {
            ConfigBuilder = pluginStepConfig;
            Action = action;
        }

        public IPluginStepConfigBuilder ConfigBuilder { get; set; }
        
        public Action<IServiceProvider> Action { get; set; }
    }
}
