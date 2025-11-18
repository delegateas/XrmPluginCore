using XrmPluginCore.Enums;
using XrmPluginCore.Tests.Helpers;
using XrmPluginCore.Tests.TestCustomApis;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using NSubstitute;
using System;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace XrmPluginCore.Tests
{
    public class CustomAPITests
    {
        [Fact]
        public void ExecuteNullServiceProviderShouldThrowArgumentNullException()
        {
            // Arrange
            var customApi = new TestCustomAPI();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => customApi.Execute(null));
        }

        [Fact]
        public void ExecuteValidRegistrationShouldExecuteActionWithLocalPluginContext()
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
        public void ExecuteValidRegistrationShouldExecuteActionWithServiceProvider()
        {
            // Arrange
            var customApi = new TestCustomAPIServiceProvider();
            var mockProvider = new MockServiceProvider();

            // Act
            customApi.Execute(mockProvider.ServiceProvider);

            // Assert
            customApi.ExecutedAction.Should().BeTrue();
            customApi.LastProvider.Should().NotBeNull();
        }

        [Fact]
        public void ExecuteNoRegistrationShouldNotExecuteAction()
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
        public void ExecuteFaultExceptionShouldRethrow()
        {
            // Arrange
            var mockProvider = new MockServiceProvider();
            var customApi = new TestCustomAPI();

            // Setup organization service factory to throw exception when creating organization service
            var faultException = new FaultException<OrganizationServiceFault>(new OrganizationServiceFault());
            mockProvider.OrganizationServiceFactory.CreateOrganizationService(Arg.Any<Guid?>())
                .Returns(x => { throw faultException; });

            // Act & Assert
            var exception = Assert.Throws<InvalidPluginExecutionException>(() => customApi.Execute(mockProvider.ServiceProvider));
            exception.Status.Should().Be(OperationStatus.Failed);
            exception.Message.Should().Be(faultException.Message);
        }

        [Fact]
        public void RegisterCustomAPIMultipleRegistrationsShouldThrowInvalidOperationException()
        {
            // Arrange
            var customApi = new TestMultipleRegistrationCustomAPI();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => customApi.TryRegisterSecond());
        }

        [Fact]
        public void GetRegistrationValidRegistrationShouldReturnConfiguration()
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
        public void GetRegistrationWithConfigurationShouldReturnFullConfiguration()
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
        public void OnBeforeBuildServiceProviderShouldAllowModification()
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
        public void ExecuteShouldTraceEntryAndExit()
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

            mockProvider.PluginTelemetryLogger.Received().LogInformation(
                "{0}, Correlation Id: {1}, Initiating User: {2}",
                Arg.Is<string>(s => s.Contains("Entered") && s.Contains("Execute")),
                Arg.Any<Guid>(),
                Arg.Any<Guid>());

            mockProvider.PluginTelemetryLogger.Received().LogInformation(
                "{0}, Correlation Id: {1}, Initiating User: {2}",
                Arg.Is<string>(s => s.Contains("Exiting") && s.Contains("Execute")),
                Arg.Any<Guid>(),
                Arg.Any<Guid>());
        }

        [Fact]
        public void ExecuteShouldTraceExecutionInfo()
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
}