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

        /// <summary>
        /// Gets or sets the plugin class name for type-safe wrapper discovery.
        /// Used to compute wrapper class names by convention.
        /// </summary>
        public string PluginClassName { get; set; }

        /// <summary>
        /// Gets or sets the entity type name for type-safe wrapper discovery.
        /// Used to compute wrapper class names by convention.
        /// </summary>
        public string EntityTypeName { get; set; }

        /// <summary>
        /// Gets or sets the event operation for type-safe wrapper discovery.
        /// Used to compute wrapper class names by convention.
        /// </summary>
        public string EventOperation { get; set; }

        /// <summary>
        /// Gets or sets the execution stage for type-safe wrapper discovery.
        /// Used to compute wrapper class names by convention.
        /// </summary>
        public string ExecutionStage { get; set; }
    }
}
