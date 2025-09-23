using DG.XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;
using System;

namespace DG.XrmPluginCore.Tests.TestCustomApis
{
    public class TestCustomAPI : CustomAPI
    {
        public bool ExecutedAction { get; private set; }
        public LocalPluginContext LastContext { get; private set; }

        public TestCustomAPI()
        {
            RegisterCustomAPI("test_custom_api", Execute);
        }

        private void Execute(LocalPluginContext context)
        {
            ExecutedAction = true;
            LastContext = context;
        }
    }

    public class TestCustomAPIWithConfig : CustomAPI
    {
        public bool ExecutedAction { get; private set; }

        public TestCustomAPIWithConfig()
        {
            RegisterCustomAPI("test_custom_api_with_config", Execute)
                .SetDescription("Test Custom API")
                .MakeFunction()
                .MakePrivate()
                .EnableForWorkFlow()
                .AddRequestParameter("input_param", CustomApiParameterType.String, "Input Parameter", "Test input parameter")
                .AddResponseProperty("output_prop", CustomApiParameterType.String, "Output Property", "Test output property");
        }

        private void Execute(LocalPluginContext context)
        {
            ExecutedAction = true;
        }
    }

    public class TestNoRegistrationCustomAPI : CustomAPI
    {
        public bool ExecutedAction { get; private set; }

        // No registrations added
    }

    public class TestMultipleRegistrationCustomAPI : CustomAPI
    {
        public TestMultipleRegistrationCustomAPI()
        {
            RegisterCustomAPI("first_api", Execute);
            // This should throw an exception when we try to register a second API
        }

        private void Execute(LocalPluginContext context)
        {
        }

        public void TryRegisterSecond()
        {
            RegisterCustomAPI("second_api", Execute);
        }

        private void Execute2(LocalPluginContext context)
        {
        }
    }
}