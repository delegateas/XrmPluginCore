using DG.XrmPluginCore.Enums;
using DG.XrmPluginCore.Tests.Helpers;
using DG.XrmPluginCore.Tests.TestCustomApis;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using NSubstitute;
using System;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace DG.XrmPluginCore.Tests
{
    public class CustomAPITests
    {
        [Fact]
        public void Execute_NullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Arrange
            var customApi = new TestCustomAPI();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => customApi.Execute(null));
        }

        [Fact]
        public void Execute_ValidRegistration_ShouldExecuteAction()
        {
            // Arrange
            var customApi = new TestCustomAPI();
            var mockProvider = new MockServiceProvider();

            // Act
            customApi.Execute(mockProvider.ServiceProvider);

            // Assert
            customApi.ExecutedAction.Should().BeTrue();
            customApi.LastContext.Should().NotBeNull();
        }

        [Fact]
        public void Execute_NoRegistration_ShouldNotExecuteAction()
        {
            // Arrange
            var customApi = new TestNoRegistrationCustomAPI();
            var mockProvider = new MockServiceProvider();

            // Act
            customApi.Execute(mockProvider.ServiceProvider);

            // Assert
            customApi.ExecutedAction.Should().BeFalse();
        }

        [Fact]
        public void Execute_FaultException_ShouldRethrow()
        {
            // Arrange
            var mockProvider = new MockServiceProvider();
            var customApi = new TestCustomAPI();

            // Setup organization service factory to throw exception when creating organization service
            var faultException = new FaultException<OrganizationServiceFault>(new OrganizationServiceFault());
            mockProvider.OrganizationServiceFactory.CreateOrganizationService(Arg.Any<Guid?>())
                .Returns(x => { throw faultException; });

            // Act & Assert
            var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(() => customApi.Execute(mockProvider.ServiceProvider));
            exception.Should().Be(faultException);
        }

        [Fact]
        public void RegisterCustomAPI_MultipleRegistrations_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var customApi = new TestMultipleRegistrationCustomAPI();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => customApi.TryRegisterSecond());
        }

        [Fact]
        public void GetRegistration_ValidRegistration_ShouldReturnConfiguration()
        {
            // Arrange
            var customApi = new TestCustomAPI();

            // Act
            var registration = customApi.GetRegistration();

            // Assert
            registration.Should().NotBeNull();
            registration.Name.Should().Be("test_custom_api");
            registration.UniqueName.Should().Be("test_custom_api");
        }

        [Fact]
        public void GetRegistration_WithConfiguration_ShouldReturnFullConfiguration()
        {
            // Arrange
            var customApi = new TestCustomAPIWithConfig();

            // Act
            var registration = customApi.GetRegistration();

            // Assert
            registration.Should().NotBeNull();
            registration.Name.Should().Be("test_custom_api_with_config");
            registration.Description.Should().Be("Test Custom API");
            registration.IsFunction.Should().BeTrue();
            registration.IsPrivate.Should().BeTrue();
            registration.EnabledForWorkflow.Should().BeTrue();

            var requestParams = registration.RequestParameters.ToList();
            requestParams.Should().HaveCount(1);
            requestParams[0].UniqueName.Should().Be("input_param");
            requestParams[0].Type.Should().Be(CustomApiParameterType.String);

            var responseProps = registration.ResponseProperties.ToList();
            responseProps.Should().HaveCount(1);
            responseProps[0].UniqueName.Should().Be("output_prop");
            responseProps[0].Type.Should().Be(CustomApiParameterType.String);
        }

        [Fact]
        public void OnBeforeConstructLocalPluginContext_ShouldAllowModification()
        {
            // Arrange
            var customApi = new TestServiceProviderModificationCustomAPI();
            var originalProvider = new MockServiceProvider();

            // Act
            customApi.Execute(originalProvider.ServiceProvider);

            // Assert
            customApi.ModifiedServiceProviderUsed.Should().BeTrue();
        }

        [Fact]
        public void Execute_ShouldTraceEntryAndExit()
        {
            // Arrange
            var customApi = new TestCustomAPI();
            var mockProvider = new MockServiceProvider();

            // Act
            customApi.Execute(mockProvider.ServiceProvider);

            // Assert
            mockProvider.TracingService.Received().Trace(
                "{0}, Correlation Id: {1}, Initiating User: {2}",
                Arg.Is<string>(s => s.Contains("Entered") && s.Contains("Execute")),
                Arg.Any<Guid>(),
                Arg.Any<Guid>());
            mockProvider.TracingService.Received().Trace(
                "{0}, Correlation Id: {1}, Initiating User: {2}",
                Arg.Is<string>(s => s.Contains("Exiting") && s.Contains("Execute")),
                Arg.Any<Guid>(),
                Arg.Any<Guid>());
        }

        [Fact]
        public void Execute_ShouldTraceStage()
        {
            // Arrange
            var customApi = new TestCustomAPI();
            var mockProvider = new MockServiceProvider();
            var stage = 30;
            mockProvider.PluginExecutionContext.Stage.Returns(stage);

            // Act
            customApi.Execute(mockProvider.ServiceProvider);

            // Assert
            mockProvider.TracingService.Received().Trace(
                "{0}, Correlation Id: {1}, Initiating User: {2}",
                stage.ToString(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>());
        }

        [Fact]
        public void Execute_ShouldTraceExecutionInfo()
        {
            // Arrange
            var customApi = new TestCustomAPI();
            var mockProvider = new MockServiceProvider();
            var entityName = "test_entity";
            var messageName = "test_message";
            
            mockProvider.PluginExecutionContext.PrimaryEntityName.Returns(entityName);
            mockProvider.PluginExecutionContext.MessageName.Returns(messageName);

            // Act
            customApi.Execute(mockProvider.ServiceProvider);

            // Assert
            mockProvider.TracingService.Received().Trace(
                "{0}, Correlation Id: {1}, Initiating User: {2}",
                Arg.Is<string>(s => s.Contains(entityName) && s.Contains(messageName) && s.Contains("is firing for")),
                Arg.Any<Guid>(),
                Arg.Any<Guid>());
        }
    }

    // Helper custom API for testing service provider modification
    public class TestServiceProviderModificationCustomAPI : CustomAPI
    {
        public bool ModifiedServiceProviderUsed { get; private set; }

        public TestServiceProviderModificationCustomAPI()
        {
            RegisterCustomAPI("test_modification_api", Execute);
        }

        protected override IServiceProvider OnBeforeConstructLocalPluginContext(IServiceProvider serviceProvider)
        {
            // Create a wrapper that marks when it's used
            var wrapper = Substitute.For<IServiceProvider>();
            wrapper.GetService(Arg.Any<Type>()).Returns(callInfo =>
            {
                ModifiedServiceProviderUsed = true;
                return serviceProvider.GetService(callInfo.Arg<Type>());
            });

            return wrapper;
        }

        private void Execute(LocalPluginContext context)
        {
            // Action implementation
        }
    }
}