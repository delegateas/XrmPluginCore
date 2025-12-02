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

		public IExtendedTracingService TracingService { get; }

		public LocalPluginContext(IExtendedServiceProvider serviceProvider)
		{
			if (serviceProvider == null)
			{
				throw new ArgumentNullException(nameof(serviceProvider));
			}

			// Obtain the execution context service from the service provider.
			PluginExecutionContext = serviceProvider.GetService<IPluginExecutionContext>();

			// Obtain the tracing service from the service provider.
			TracingService = serviceProvider.GetService<IExtendedTracingService>();

			// Obtain the Organization Service factory service from the service provider
			var factory = serviceProvider.GetService<IOrganizationServiceFactory>();

			// Use the factory to generate the Organization Service.
			OrganizationService = factory.CreateOrganizationService(PluginExecutionContext.UserId);

			// Context& added: Use the factory to generate the Organization Admin Service.
			OrganizationAdminService = factory.CreateOrganizationService(null);
		}

		public void Trace(string message)
		{
			TracingService?.Trace(message, PluginExecutionContext);
		}
	}
}
