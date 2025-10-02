using System;

namespace XrmPluginCore.Plugins
{
    internal class PluginStepRegistration
    {
        public PluginStepRegistration(IPluginStepConfigBuilder pluginStepConfig, Action<IServiceProvider> action)
        {
            ConfigBuilder = pluginStepConfig;
            Action = action;
        }

        public IPluginStepConfigBuilder ConfigBuilder { get; set; }
        
        public Action<IServiceProvider> Action { get; set; }
    }
}
