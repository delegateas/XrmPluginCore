using DG.XrmPluginCore.CustomApis;
using DG.XrmPluginCore.Interfaces.CustomApi;
using Microsoft.Xrm.Sdk;
using System;

namespace DG.XrmPluginCore
{
    /// <summary>
    /// Base class for all CustomAPIs.
    /// </summary>
    public abstract class CustomAPI : AbstractPlugin, ICustomApiDefinition
    {
        protected Action<IServiceProvider> RegisteredEvent { get; private set; }

        private CustomApiConfigBuilder ConfigBuilder { get; set; }

        protected override Action<IServiceProvider> GetAction(IPluginExecutionContext context) => RegisteredEvent;

        /// <summary>
        /// Get the CustomAPI configuration.
        /// </summary>
        public ICustomApiConfig GetRegistration() => ConfigBuilder.Build();

        /// <summary>
        /// Register a CustomAPI with the given name and action.<br/>
        /// 
        /// Returns the config builder for specifying additional settings
        /// </summary>
        /// <exception cref="InvalidOperationException">If called multiple times in the same class</exception>
        protected CustomApiConfigBuilder RegisterCustomAPI(string name, Action<LocalPluginContext> action)
        {
            return RegisterAPI(name, sp => action(new LocalPluginContext(sp)));
        }

        /// <summary>
        /// Register a CustomAPI with the given name and action.<br/>
        /// 
        /// Returns the config builder for specifying additional settings
        /// </summary>
        /// <exception cref="InvalidOperationException">If called multiple times in the same class</exception>
        protected CustomApiConfigBuilder RegisterAPI(string name, Action<IServiceProvider> action)
        {
            if (ConfigBuilder != null || RegisteredEvent != null)
            {
                throw new InvalidOperationException($"The {nameof(CustomAPI)} class does not support multiple registrations");
            }

            ConfigBuilder = new CustomApiConfigBuilder(name);
            RegisteredEvent = action;

            return ConfigBuilder;
        }
    }
}
