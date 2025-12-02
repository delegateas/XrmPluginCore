using System;

namespace XrmPluginCore.Plugins;

internal sealed class PluginStepRegistration(
	IPluginStepConfigBuilder pluginStepConfig,
	Action<IExtendedServiceProvider> action,
	string pluginClassName,
	string entityTypeName,
	string eventOperation,
	string executionStage,
	string serviceTypeName = null,
	string serviceTypeFullName = null,
	string handlerMethodName = null)
{
	public IPluginStepConfigBuilder ConfigBuilder { get; } = pluginStepConfig;

	public Action<IExtendedServiceProvider> Action { get; } = action;

	public string PluginClassName { get; } = pluginClassName;

	public string EntityTypeName { get; } = entityTypeName;

	public string EventOperation { get; } = eventOperation;

	public string ExecutionStage { get; } = executionStage;

	public string ServiceTypeName { get; } = serviceTypeName;

	public string ServiceTypeFullName { get; } = serviceTypeFullName;

	public string HandlerMethodName { get; } = handlerMethodName;
}
