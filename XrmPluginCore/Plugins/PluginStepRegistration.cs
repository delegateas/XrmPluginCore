using System;

namespace XrmPluginCore.Plugins
{
    internal class PluginStepRegistration
    {
        public PluginStepRegistration(IPluginStepConfigBuilder pluginStepConfig, Action<IExtendedServiceProvider> action)
        {
            ConfigBuilder = pluginStepConfig;
            Action = action;
        }

        public IPluginStepConfigBuilder ConfigBuilder { get; set; }
        
        public Action<IExtendedServiceProvider> Action { get; set; }
    }
}
