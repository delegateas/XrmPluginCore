using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;
using System;
using System.Globalization;
using System.ServiceModel;

namespace DG.XrmPluginCore
{
    public abstract class AbstractPlugin : IPlugin
    {
        /// <summary>
        /// Gets the name of the child class.
        /// </summary>
        /// <value>The name of the child class.</value>
        protected string ChildClassName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractPlugin"/> class.
        /// </summary>
        protected AbstractPlugin()
        {
            ChildClassName = GetType().ToString();
        }

        /// <summary>
        /// Called to get the action to execute for the given context.
        /// </summary>
        /// <param name="context">The IPluginExecutionContext of the call</param>
        /// <returns>The action to call if matching</returns>
        protected abstract Action<IServiceProvider> GetAction(IPluginExecutionContext context);

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
        public virtual void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            // Build a local service provider to manage the lifetime of services for this execution
            var localServiceProvider = BuildServiceProvider(serviceProvider);

            try
            {
                Trace(localServiceProvider, string.Format(CultureInfo.InvariantCulture, "Entered {0}.Execute()", ChildClassName));
                var context = localServiceProvider.GetService<IPluginExecutionContext>() ?? throw new Exception("Unable to get Plugin Execution Context");
                var entityAction = GetAction(context);

                if (entityAction == null)
                {
                    Trace(localServiceProvider, string.Format(
                        CultureInfo.InvariantCulture,
                        "No registered event found for Entity: {0}, Message: {1} in {2}",
                        context.PrimaryEntityName,
                        context.MessageName,
                        ChildClassName
                    ));

                    return;
                }

                Trace(localServiceProvider, string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} is firing for Entity: {1}, Message: {2}",
                    ChildClassName,
                    context.PrimaryEntityName,
                    context.MessageName
                    ));

                entityAction.Invoke(localServiceProvider);
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                Trace(localServiceProvider, string.Format(CultureInfo.InvariantCulture, "Exception: {0}", e.ToString()));
                throw new InvalidPluginExecutionException(OperationStatus.Failed, e.Message);
            }
            catch (NotImplementedException e)
            {
                Trace(localServiceProvider, string.Format(CultureInfo.InvariantCulture, "Exception: {0}", e.ToString()));
                throw new InvalidPluginExecutionException(OperationStatus.Failed, e.Message);
            }
            finally
            {
                Trace(localServiceProvider, string.Format(CultureInfo.InvariantCulture, "Exiting {0}.Execute()", ChildClassName));
            }
        }

        protected static void Trace(IServiceProvider serviceProvider, string message)
        {
            var tracingService = serviceProvider.GetService<ITracingService>();

            if (string.IsNullOrWhiteSpace(message) || tracingService == null)
            {
                return;
            }

            var pluginExecutionContext = serviceProvider.GetService<IPluginExecutionContext>();

            if (pluginExecutionContext == null)
            {
                tracingService.Trace(message);
            }
            else
            {
                tracingService.Trace(
                    "{0}, Correlation Id: {1}, Initiating User: {2}",
                    message,
                    pluginExecutionContext.CorrelationId,
                    pluginExecutionContext.InitiatingUserId);
            }
        }

        /// <summary>
        /// Called to allow derived classes to modify the service collection before it is used
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        protected virtual IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services;
        }

        protected ServiceProvider BuildServiceProvider(IServiceProvider serviceProvider)
        {
            // Get services of the ServiceProvider
            var tracingService = serviceProvider.GetService<ITracingService>() ?? throw new Exception("Unable to get Tracing service");
            var telemetryService = serviceProvider.GetService<ILogger>();

            var extendedTracingService = new ExtendedTracingService(tracingService, telemetryService);

            // Create a new service collection and add the relevant services
            IServiceCollection services = new ServiceCollection();

            services.AddScoped(_ => serviceProvider.GetService<IPluginExecutionContext>());
            services.AddScoped(_ => serviceProvider.GetService<IPluginExecutionContext2>());
            services.AddScoped(_ => serviceProvider.GetService<IPluginExecutionContext3>());
            services.AddScoped(_ => serviceProvider.GetService<IPluginExecutionContext4>());
            services.AddScoped(_ => serviceProvider.GetService<IPluginExecutionContext5>());
            services.AddScoped(_ => serviceProvider.GetService<IPluginExecutionContext6>());
            services.AddScoped(_ => serviceProvider.GetService<IPluginExecutionContext7>());

            services.AddScoped(_ => serviceProvider.GetService<IOrganizationServiceFactory>());
            services.AddScoped(_ => telemetryService);
            services.AddScoped<ITracingService>(_ => extendedTracingService);
            services.AddScoped<IExtendedTracingService>(_ => extendedTracingService);

            services = OnBeforeBuildServiceProvider(services);
            
            // Build the service provider
            return services.BuildServiceProvider();
        }
    }
}
