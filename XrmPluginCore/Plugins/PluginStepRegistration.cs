using System;

namespace XrmPluginCore.Plugins;

internal sealed class PluginStepRegistration(
	IPluginStepConfigBuilder pluginStepConfig,
	Action<IExtendedServiceProvider> action,
	string handlerMethodName = null,
	string wrapperTypeName = null)
{
	public IPluginStepConfigBuilder ConfigBuilder { get; } = pluginStepConfig;

	public Action<IExtendedServiceProvider> Action { get; } = action;

	public string HandlerMethodName { get; } = handlerMethodName;

	/// <summary>
	/// The fully qualified type name of the source-generated <c>ActionWrapper</c> to discover and invoke
	/// when <see cref="Action"/> is null. Computed at registration time for both plugin steps and Custom
	/// APIs, so runtime discovery has a single source of truth regardless of naming convention.
	/// </summary>
	public string WrapperTypeName { get; } = wrapperTypeName;
}
