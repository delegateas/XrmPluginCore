using Microsoft.Xrm.Sdk;

namespace XrmPluginCore
{
    public interface IExtendedTracingService : ITracingService
    {
        void Trace(string message, IPluginExecutionContext pluginExecutionContext, params object[] args);
    }
}