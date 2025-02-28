using Microsoft.Xrm.Sdk;
using System;

namespace DG.XrmPluginCore
{
    public class LocalPluginContext
    {
        public IServiceProvider ServiceProvider { get; }

        public IOrganizationService OrganizationService { get; }

        // Delegate A/S added:
        public IOrganizationService OrganizationAdminService { get; }

        public IPluginExecutionContext PluginExecutionContext { get; }

        public ITracingService TracingService { get; }

        public LocalPluginContext(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            // Obtain the execution context service from the service provider.
            PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain the tracing service from the service provider.
            TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the Organization Service factory service from the service provider
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            // Use the factory to generate the Organization Service.
            OrganizationService = factory.CreateOrganizationService(PluginExecutionContext.UserId);

            // Delegate A/S added: Use the factory to generate the Organization Admin Service.
            OrganizationAdminService = factory.CreateOrganizationService(null);
        }

        public void Trace(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || TracingService == null)
            {
                return;
            }

            if (PluginExecutionContext == null)
            {
                TracingService.Trace(message);
            }
            else
            {
                TracingService.Trace(
                    "{0}, Correlation Id: {1}, Initiating User: {2}",
                    message,
                    PluginExecutionContext.CorrelationId,
                    PluginExecutionContext.InitiatingUserId);
            }
        }
    }
}
