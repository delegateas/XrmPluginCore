using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using System;

namespace XrmPluginCore
{
    public class LocalPluginContext
    {
        public IServiceProvider ServiceProvider { get; }

        public IOrganizationService OrganizationService { get; }

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
            PluginExecutionContext = serviceProvider.GetService<IPluginExecutionContext>();

            // Obtain the tracing service from the service provider.
            TracingService = serviceProvider.GetService<ITracingService>();

            // Obtain the Organization Service factory service from the service provider
            var factory = serviceProvider.GetService<IOrganizationServiceFactory>();

            // Use the factory to generate the Organization Service.
            OrganizationService = factory.CreateOrganizationService(PluginExecutionContext.UserId);

            // Context& added: Use the factory to generate the Organization Admin Service.
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
