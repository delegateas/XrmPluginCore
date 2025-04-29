using System;
using System.Globalization;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using DG.XrmPluginCore.CustomApis;
using DG.XrmPluginCore.Interfaces.CustomApi;

namespace DG.XrmPluginCore
{
    /// <summary>
    /// Base class for all CustomAPIs.
    /// </summary>
    public class CustomAPI : PluginBase, ICustomApiDefinition
    {
        protected Action<LocalPluginContext> RegisteredEvent { get; private set; }

        /// <summary>
        /// Executes the plug-in.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <remarks>
        /// For improved performance, Microsoft Dynamics CRM caches plug-in instances. 
        /// The plug-in's Execute method should be written to be stateless as the constructor 
        /// is not called for every invocation of the plug-in. Also, multiple system threads 
        /// could execute the plug-in at the same time. All per invocation state information 
        /// is stored in the context. This means that you should not use global variables in plug-ins.
        /// </remarks>
        public override void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            // Construct the Local plug-in context.
            LocalPluginContext localcontext = new LocalPluginContext(serviceProvider);

            localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.Execute()", ChildClassName));
            localcontext.Trace(localcontext.PluginExecutionContext.Stage.ToString());

            try
            {
                if (RegisteredEvent == null)
                {
                    localcontext.Trace(string.Format(
                        CultureInfo.InvariantCulture,
                        "No registered event found for Entity: {0}, Message: {2} in {2}",
                        localcontext.PluginExecutionContext.PrimaryEntityName,
                        localcontext.PluginExecutionContext.MessageName,
                        ChildClassName));
                    return;
                }

                localcontext.Trace(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} is firing for Entity: {1}, Message: {2}\n",
                    ChildClassName,
                    localcontext.PluginExecutionContext.PrimaryEntityName,
                    localcontext.PluginExecutionContext.MessageName));

                RegisteredEvent.Invoke(localcontext);
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", e.ToString()));

                // Handle the exception.
                throw;
            }
            finally
            {
                localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Exiting {0}.Execute()", ChildClassName));
            }
        }

        private CustomApiConfigBuilder ConfigBuilder { get; set; }

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
