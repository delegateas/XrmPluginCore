using System;

namespace XrmPluginCore.CustomApis
{
	internal class CustomApiRegistration
	{
		public CustomApiRegistration(CustomApiConfigBuilder customApiConfig, Action<IExtendedServiceProvider> action)
		{
			ConfigBuilder = customApiConfig;
			Action = action;
		}
		public CustomApiConfigBuilder ConfigBuilder { get; set; }

		public Action<IExtendedServiceProvider> Action { get; set; }
	}
}
