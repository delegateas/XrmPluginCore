using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;
using System;

namespace XrmPluginCore.Extensions
{
    internal static class ServiceProviderExtensions
    {
        public static ServiceProvider BuildServiceProvider(this IServiceProvider serviceProvider, Func<IServiceCollection, IServiceCollection> onBeforeBuild)
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

            // Allow modification of services before building the provider
            services = onBeforeBuild(services);

            // Build the service provider
            return services.BuildServiceProvider();
        }

        public static void Trace(this IServiceProvider serviceProvider, string message)
        {
            var tracingService = serviceProvider.GetService<IExtendedTracingService>();
            var pluginExecutionContext = serviceProvider.GetService<IPluginExecutionContext>();

            tracingService?.Trace(message, pluginExecutionContext);
        }
    }
}
