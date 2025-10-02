using System;

namespace XrmPluginCore.CustomApis
{
    internal class CustomApiRegistration
    {
        public CustomApiRegistration(CustomApiConfigBuilder customApiConfig, Action<IServiceProvider> action)
        {
            ConfigBuilder = customApiConfig;
            Action = action;
        }
        public CustomApiConfigBuilder ConfigBuilder { get; set; }

        public Action<IServiceProvider> Action { get; set; }
    }
}
