using Microsoft.Extensions.DependencyInjection;
using System;
using XrmPluginCore.Enums;

namespace XrmPluginCore.Tests.TestCustomApis
{
    public class TestCustomAPI : Plugin
    {
        public bool ExecutedAction { get; private set; }
        public LocalPluginContext LastContext { get; private set; }

        public TestCustomAPI()
        {
            RegisterCustomAPI("test_custom_api", ExecuteAPI);
        }

        private void ExecuteAPI(LocalPluginContext context)
        {
            ExecutedAction = true;
            LastContext = context;
        }
    }

    public class TestCustomAPIServiceProvider : Plugin
    {
        public bool ExecutedAction { get; private set; }
        public IServiceProvider LastProvider { get; private set; }

        public TestCustomAPIServiceProvider()
        {
            RegisterAPI("test_custom_api", ExecuteAPI);
        }

        private void ExecuteAPI(IServiceProvider context)
        {
            ExecutedAction = true;
            LastProvider = context;
        }
    }

    public class TestCustomAPIWithConfig : Plugin
    {
        public bool ExecutedAction { get; private set; }

        public TestCustomAPIWithConfig()
        {
            RegisterCustomAPI("test_custom_api_with_config", ExecuteAPI)
                .SetDescription("Test Custom API")
                .MakeFunction()
                .MakePrivate()
                .EnableForWorkFlow()
                .AddRequestParameter("input_param", CustomApiParameterType.String, "Input Parameter", "Test input parameter")
                .AddResponseProperty("output_prop", CustomApiParameterType.String, "Output Property", "Test output property");
        }

        private void ExecuteAPI(LocalPluginContext context)
        {
            ExecutedAction = true;
        }
    }

    public class TestNoRegistrationCustomAPI : Plugin
    {
        public bool ExecutedAction { get; private set; }

        // No registrations added
    }

    public class TestMultipleRegistrationCustomAPI : Plugin
    {
        public TestMultipleRegistrationCustomAPI()
        {
            RegisterCustomAPI("first_api", ExecuteAPI);
            // This should throw an exception when we try to register a second API
        }

        private void ExecuteAPI(LocalPluginContext context)
        {
        }

        public void TryRegisterSecond()
        {
            RegisterCustomAPI("second_api", Execute2);
        }

        private void Execute2(LocalPluginContext context)
        {
        }
    }

    // Helper custom API for testing service provider modification
    public class TestServiceProviderModificationCustomAPI : Plugin
    {
        public bool ModifiedServiceProviderUsed { get; private set; }

        public TestServiceProviderModificationCustomAPI()
        {
            RegisterAPI("test_modification_api", ExecuteApi);
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection serviceProvider)
        {
            // Inject an object we can then get
            return serviceProvider.AddScoped(_ => "Modified");
        }

        private void ExecuteApi(IServiceProvider context)
        {
            // Action implementation
            var stringValue = context.GetService<string>();
            ModifiedServiceProviderUsed = stringValue == "Modified";
        }
    }
}