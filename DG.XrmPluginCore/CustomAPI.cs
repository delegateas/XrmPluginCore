namespace DG.XrmPluginCore
{
    using System;
    using System.Globalization;
    using System.ServiceModel;
    using Microsoft.Xrm.Sdk;
    using DG.XrmPluginCore.CustomApis;
    using DG.XrmPluginCore.Abstractions;
    using DG.XrmPluginCore.Abstractions.Models.CustomApi;

    /// <summary>
    /// Base class for all CustomAPIs.
    /// </summary>    
    public class CustomAPI : PluginBase, ICustomApi
    {
        protected Action<LocalPluginContext> RegisteredEvent { get; private set; }

        private CustomApiConfigBuilder CustomAPIConfig { get; set; }

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
                // Iterate over all of the expected registered events to ensure that the CustomAPI
                // has been invoked by an expected event
                // For any given plug-in event at an instance in time, we would expect at most 1 result to match.
                Action<LocalPluginContext> action = RegisteredEvent;

                if (action != null)
                {
                    localcontext.Trace(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} is firing for Entity: {1}, Message: {2}\n",
                        ChildClassName,
                        localcontext.PluginExecutionContext.PrimaryEntityName,
                        localcontext.PluginExecutionContext.MessageName));

                    action.Invoke(localcontext);

                    // now exit - if the derived plug-in has incorrectly registered overlapping event registrations,
                    // guard against multiple executions.
                    return;
                }
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

        #region CustomAPI retrieval

        /// <summary>
        /// Made by Delegate A/S
        /// Get the CustomAPI configurations.
        /// </summary>
        /// <returns>API</returns>
        public Registration GetCustomAPIRegistration()
        {
            return new Registration
            {
                Config = CustomAPIConfig.Build(),
                RequestParameters = CustomAPIConfig.GetRequestParameters(),
                ResponseParameters = CustomAPIConfig.GetResponseProperties()
            };
        }

        protected CustomApiConfigBuilder RegisterCustomAPI(string name, Action<LocalPluginContext> action)
        {
            var apiConfig = new CustomApiConfigBuilder(name);

            if (CustomAPIConfig != null || RegisteredEvent != null)
            {
                throw new InvalidOperationException("The CustomAPI class does not support multiple registrations");
            }

            CustomAPIConfig = apiConfig;
            RegisteredEvent = action;
            return apiConfig;
        }
        #endregion
    }
}
