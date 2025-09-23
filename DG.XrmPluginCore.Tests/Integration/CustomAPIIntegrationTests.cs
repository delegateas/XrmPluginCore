using DG.XrmPluginCore.Enums;
using DG.XrmPluginCore.Tests.Helpers;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using Xunit;

namespace DG.XrmPluginCore.Tests.Integration
{
    public class CustomAPIIntegrationTests
    {
        [Fact]
        public void FullCustomAPIWorkflow_ShouldExecuteCorrectly()
        {
            // Arrange
            var customApi = new IntegrationTestCustomAPI();
            var mockProvider = new MockServiceProvider();

            var inputParameters = new ParameterCollection
            {
                { "InputParameter", "Test Value" }
            };

            var outputParameters = new ParameterCollection();

            mockProvider.SetupInputParameters(inputParameters);
            mockProvider.SetupOutputParameters(outputParameters);

            // Act
            customApi.Execute(mockProvider.ServiceProvider);

            // Assert
            customApi.ExecutedCorrectly.Should().BeTrue();
            customApi.ReceivedInputValue.Should().Be("Test Value");
            outputParameters.Should().ContainKey("OutputParameter");
            outputParameters["OutputParameter"].Should().Be("Processed: Test Value");
        }

        [Fact]
        public void CustomAPIRegistration_ShouldContainCorrectConfiguration()
        {
            // Arrange
            var customApi = new IntegrationTestCustomAPI();

            // Act
            var registration = customApi.GetRegistration();

            // Assert
            registration.Should().NotBeNull();
            registration.Name.Should().Be("integration_test_api");
            registration.IsFunction.Should().BeTrue();
            registration.EnabledForWorkflow.Should().BeTrue();
            registration.Description.Should().Be("Integration test custom API");

            var requestParams = registration.RequestParameters.ToList();
            requestParams.Should().HaveCount(1);
            requestParams[0].UniqueName.Should().Be("InputParameter");
            requestParams[0].Type.Should().Be(CustomApiParameterType.String);

            var responseProps = registration.ResponseProperties.ToList();
            responseProps.Should().HaveCount(1);
            responseProps[0].UniqueName.Should().Be("OutputParameter");
            responseProps[0].Type.Should().Be(CustomApiParameterType.String);
        }

        [Fact]
        public void BoundCustomAPI_ShouldConfigureBinding()
        {
            // Arrange
            var customApi = new BoundCustomAPI();

            // Act
            var registration = customApi.GetRegistration();

            // Assert
            registration.BindingType.Should().Be(BindingType.Entity);
            registration.BoundEntityLogicalName.Should().Be("account");
        }

        [Fact]
        public void PrivateCustomAPI_ShouldBePrivate()
        {
            // Arrange
            var customApi = new PrivateCustomAPI();

            // Act
            var registration = customApi.GetRegistration();

            // Assert
            registration.IsPrivate.Should().BeTrue();
            registration.IsFunction.Should().BeFalse();
        }
    }

    public class IntegrationTestCustomAPI : CustomAPI
    {
        public bool ExecutedCorrectly { get; private set; }
        public string ReceivedInputValue { get; private set; }

        public IntegrationTestCustomAPI()
        {
            RegisterCustomAPI("integration_test_api", Execute)
                .SetDescription("Integration test custom API")
                .MakeFunction()
                .EnableForWorkFlow()
                .AddRequestParameter("InputParameter", CustomApiParameterType.String, "Input Parameter", "Test input parameter")
                .AddResponseProperty("OutputParameter", CustomApiParameterType.String, "Output Parameter", "Test output parameter");
        }

        private void Execute(LocalPluginContext context)
        {
            try
            {
                // Get input parameter
                if (context.PluginExecutionContext.InputParameters.Contains("InputParameter"))
                {
                    ReceivedInputValue = context.PluginExecutionContext.InputParameters["InputParameter"] as string;
                }

                // Set output parameter
                var processedValue = $"Processed: {ReceivedInputValue}";
                context.PluginExecutionContext.OutputParameters["OutputParameter"] = processedValue;

                context.Trace($"Processed input: {ReceivedInputValue}");
                context.Trace($"Generated output: {processedValue}");

                ExecutedCorrectly = true;
            }
            catch (Exception ex)
            {
                context.Trace($"Error in custom API execution: {ex.Message}");
                throw;
            }
        }
    }

    public class BoundCustomAPI : CustomAPI
    {
        public BoundCustomAPI()
        {
            RegisterCustomAPI("bound_test_api", Execute)
                .Bind<CustomAPIAccount>(BindingType.Entity);
        }

        private void Execute(LocalPluginContext context)
        {
            // Implementation
        }
    }

    public class PrivateCustomAPI : CustomAPI
    {
        public PrivateCustomAPI()
        {
            RegisterCustomAPI("private_test_api", Execute)
                .MakePrivate();
        }

        private void Execute(LocalPluginContext context)
        {
            // Implementation
        }
    }

    public class CustomAPIAccount : Entity
    {
        public CustomAPIAccount() : base("account") { }
    }
}