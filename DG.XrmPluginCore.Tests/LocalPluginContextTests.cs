using DG.XrmPluginCore.Tests.Helpers;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using NSubstitute;
using System;
using Xunit;

namespace DG.XrmPluginCore.Tests
{
    public class LocalPluginContextTests
    {
        [Fact]
        public void Constructor_ValidServiceProvider_ShouldInitializeCorrectly()
        {
            // Arrange
            var mockProvider = new MockServiceProvider();

            // Act
            var context = new LocalPluginContext(mockProvider.ServiceProvider);

            // Assert
            context.PluginExecutionContext.Should().Be(mockProvider.PluginExecutionContext);
            context.TracingService.Should().Be(mockProvider.TracingService);
            context.OrganizationService.Should().Be(mockProvider.OrganizationService);
            context.OrganizationAdminService.Should().Be(mockProvider.OrganizationAdminService);
        }

        [Fact]
        public void Constructor_NullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new LocalPluginContext(null));
        }

        [Fact]
        public void Trace_ValidMessage_ShouldCallTracingService()
        {
            // Arrange
            var mockProvider = new MockServiceProvider();
            var context = new LocalPluginContext(mockProvider.ServiceProvider);
            var testMessage = "Test trace message";

            // Act
            context.Trace(testMessage);

            // Assert
            mockProvider.TracingService.Received(1).Trace(
                "{0}, Correlation Id: {1}, Initiating User: {2}",
                testMessage,
                Arg.Any<Guid>(),
                Arg.Any<Guid>());
        }

        [Fact]
        public void Trace_NullMessage_ShouldNotCallTracingService()
        {
            // Arrange
            var mockProvider = new MockServiceProvider();
            var context = new LocalPluginContext(mockProvider.ServiceProvider);

            // Act
            context.Trace(null);

            // Assert
            mockProvider.TracingService.DidNotReceive().Trace(Arg.Any<string>());
        }

        [Fact]
        public void Trace_EmptyMessage_ShouldNotCallTracingService()
        {
            // Arrange
            var mockProvider = new MockServiceProvider();
            var context = new LocalPluginContext(mockProvider.ServiceProvider);

            // Act
            context.Trace("");

            // Assert
            mockProvider.TracingService.DidNotReceive().Trace(Arg.Any<string>());
        }

        [Fact]
        public void Trace_WhitespaceMessage_ShouldNotCallTracingService()
        {
            // Arrange
            var mockProvider = new MockServiceProvider();
            var context = new LocalPluginContext(mockProvider.ServiceProvider);

            // Act
            context.Trace("   ");

            // Assert
            mockProvider.TracingService.DidNotReceive().Trace(Arg.Any<string>());
        }

        [Fact]
        public void Trace_NullTracingService_ShouldNotThrow()
        {
            // Arrange
            var mockProvider = new MockServiceProvider();
            mockProvider.ServiceProvider.GetService(typeof(ITracingService)).Returns((ITracingService)null);
            var context = new LocalPluginContext(mockProvider.ServiceProvider);

            // Act & Assert
            var exception = Record.Exception(() => context.Trace("test message"));
            exception.Should().BeNull();
        }

        [Fact]
        public void OrganizationService_ShouldUseUserId()
        {
            // Arrange
            var mockProvider = new MockServiceProvider();
            var userId = Guid.NewGuid();
            mockProvider.PluginExecutionContext.UserId.Returns(userId);

            // Act
            var context = new LocalPluginContext(mockProvider.ServiceProvider);

            // Assert
            mockProvider.OrganizationServiceFactory.Received(1).CreateOrganizationService(userId);
        }

        [Fact]
        public void OrganizationAdminService_ShouldUseNullUserId()
        {
            // Arrange
            var mockProvider = new MockServiceProvider();

            // Act
            var context = new LocalPluginContext(mockProvider.ServiceProvider);

            // Assert
            mockProvider.OrganizationServiceFactory.Received(1).CreateOrganizationService(null);
        }
    }
}