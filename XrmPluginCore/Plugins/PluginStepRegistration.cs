using System;

namespace XrmPluginCore.Plugins
{
    internal sealed class PluginStepRegistration
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

        /// <summary>
        /// Gets or sets the service type name (short name) for action wrapper generation.
        /// Used by the source generator to emit the correct service resolution.
        /// </summary>
        public string ServiceTypeName { get; set; }

        /// <summary>
        /// Gets or sets the fully qualified service type name for action wrapper generation.
        /// Used by the source generator to emit the correct using directive.
        /// </summary>
        public string ServiceTypeFullName { get; set; }

        /// <summary>
        /// Gets or sets the handler method name on the service.
        /// Used by the source generator to emit the action wrapper that calls this method.
        /// </summary>
        public string HandlerMethodName { get; set; }
    }
}
