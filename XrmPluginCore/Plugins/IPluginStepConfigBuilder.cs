using XrmPluginCore.Interfaces.Plugin;
using Microsoft.Xrm.Sdk;

namespace XrmPluginCore.Plugins
{
	public interface IPluginStepConfigBuilder
	{
		IPluginStepConfig Build();

		bool Matches(IPluginExecutionContext pluginExecutionContext);
	}
}
