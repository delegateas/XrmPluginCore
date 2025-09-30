using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;

namespace DG.XrmPluginCore
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
    }
}