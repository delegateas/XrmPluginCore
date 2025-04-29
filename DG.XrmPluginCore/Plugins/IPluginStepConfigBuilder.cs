using DG.XrmPluginCore.Interfaces.Plugin;
using Microsoft.Xrm.Sdk;

namespace DG.XrmPluginCore.Plugins
{
    public interface IPluginStepConfigBuilder
    {
        IPluginStepConfig Build();

        bool Matches(IPluginExecutionContext pluginExecutionContext);
    }
}