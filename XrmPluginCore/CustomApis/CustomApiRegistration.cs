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

		/// <summary>
		/// Creates a registration that defers to a source-generated ActionWrapper, discovered by naming
		/// convention from the (sanitized) API name. Used by the type-safe
		/// <c>RegisterAPI&lt;TService&gt;(name, handlerMethodName)</c> overload.
		/// </summary>
		public CustomApiRegistration(CustomApiConfigBuilder customApiConfig, string handlerMethodName)
		{
			ConfigBuilder = customApiConfig;
			Action = null;
			HandlerMethodName = handlerMethodName;
		}

		public CustomApiConfigBuilder ConfigBuilder { get; set; }

		public Action<IExtendedServiceProvider> Action { get; set; }

		public string HandlerMethodName { get; }
	}
}
