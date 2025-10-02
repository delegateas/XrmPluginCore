using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;

namespace XrmPluginCore
{

    public class ExtendedTracingService : IExtendedTracingService
    {
        private readonly ITracingService tracingService;
        private readonly ILogger pluginTelemetryLogger;

        public ExtendedTracingService(ITracingService tracingService, ILogger pluginTelemetryLogger)
        {
            this.tracingService = tracingService;
            this.pluginTelemetryLogger = pluginTelemetryLogger;
        }

        public void Trace(string format, params object[] args)
        {
            tracingService?.Trace(format, args);
            pluginTelemetryLogger?.LogInformation(format, args);
        }

        public void Trace(string format, IPluginExecutionContext pluginExecutionContext, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return;
            }

            if (pluginExecutionContext == null)
            {
                Trace(format, args);
            }
            else
            {
                var message = args == null || args.Length == 0
                    ? format : string.Format(format, args);

                Trace(
                    "{0}, Correlation Id: {1}, Initiating User: {2}",
                    message,
                    pluginExecutionContext.CorrelationId,
                    pluginExecutionContext.InitiatingUserId);
            }
        }
    }
}