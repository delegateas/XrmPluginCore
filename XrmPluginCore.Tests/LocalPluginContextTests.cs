using XrmPluginCore.Extensions;
using XrmPluginCore.Tests.Helpers;
using FluentAssertions;
using NSubstitute;
using System;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace XrmPluginCore.Tests
{
    public class LocalPluginContextTests
    {
        [Fact]
        public void Constructor_ValidServiceProvider_ShouldInitializeCorrectly()
        {
            // Arrange
            var mockProvider = new MockServiceProvider();
            var serviceProvider = mockProvider.ServiceProvider.BuildServiceProvider(services => services);
            var tracingService = serviceProvider.GetService<IExtendedTracingService>();

            // Act
            var context = new LocalPluginContext(serviceProvider);

            // Assert
            context.PluginExecutionContext.Should().Be(mockProvider.PluginExecutionContext);
            tracingService.Should().NotBeNull();
            context.TracingService.Should().Be(tracingService);
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
            var serviceProvider = mockProvider.ServiceProvider.BuildServiceProvider(services => services);
            var context = new LocalPluginContext(serviceProvider);
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
            var serviceProvider = mockProvider.ServiceProvider.BuildServiceProvider(services => services);
            var context = new LocalPluginContext(serviceProvider);

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
            var serviceProvider = mockProvider.ServiceProvider.BuildServiceProvider(services => services);
            var context = new LocalPluginContext(serviceProvider);

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
            var serviceProvider = mockProvider.ServiceProvider.BuildServiceProvider(services => services);
            var context = new LocalPluginContext(serviceProvider);

            // Act
            context.Trace("   ");

            // Assert
            mockProvider.TracingService.DidNotReceive().Trace(Arg.Any<string>());
        }

        [Fact]
        public void OrganizationService_ShouldUseUserId()
        {
            // Arrange
            var mockProvider = new MockServiceProvider();
            var userId = Guid.NewGuid();
            mockProvider.PluginExecutionContext.UserId.Returns(userId);
            var serviceProvider = mockProvider.ServiceProvider.BuildServiceProvider(services => services);

            // Act
            var context = new LocalPluginContext(serviceProvider);

            // Assert
            mockProvider.OrganizationServiceFactory.Received(1).CreateOrganizationService(userId);
        }

        [Fact]
        public void OrganizationAdminService_ShouldUseNullUserId()
        {
            // Arrange
            var mockProvider = new MockServiceProvider();
            var serviceProvider = mockProvider.ServiceProvider.BuildServiceProvider(services => services);

            // Act
            var context = new LocalPluginContext(serviceProvider);

            // Assert
            mockProvider.OrganizationServiceFactory.Received(1).CreateOrganizationService(null);
        }
    }
}