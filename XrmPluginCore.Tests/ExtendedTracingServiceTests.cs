using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;
using NSubstitute;
using System;
using Xunit;

namespace XrmPluginCore.Tests;

public class ExtendedTracingServiceTests
{
	#region Constructor Tests

	[Fact]
	public void Constructor_ShouldAcceptNullTracingService()
	{
		// Arrange & Act
		var logger = Substitute.For<ILogger>();
		var service = new ExtendedTracingService(null, logger);

		// Assert - should not throw, service can be used
		service.Should().NotBeNull();
	}

	[Fact]
	public void Constructor_ShouldAcceptNullLogger()
	{
		// Arrange & Act
		var tracingService = Substitute.For<ITracingService>();
		var service = new ExtendedTracingService(tracingService, null);

		// Assert - should not throw, service can be used
		service.Should().NotBeNull();
	}

	[Fact]
	public void Constructor_ShouldAcceptBothNullParameters()
	{
		// Arrange & Act
		var service = new ExtendedTracingService(null, null);

		// Assert - should not throw, service can be used
		service.Should().NotBeNull();
	}

	#endregion

	#region Trace(string, params object[]) Tests

	[Fact]
	public void Trace_WithValidMessage_ShouldCallBothServices()
	{
		// Arrange
		var tracingService = Substitute.For<ITracingService>();
		var logger = Substitute.For<ILogger>();
		var service = new ExtendedTracingService(tracingService, logger);

		// Act
		service.Trace("Test message");

		// Assert
		tracingService.Received(1).Trace("Test message");
		logger.Received(1).LogInformation("Test message");
	}

	[Fact]
	public void Trace_WithFormatAndArgs_ShouldPassArgsToServices()
	{
		// Arrange
		var tracingService = Substitute.For<ITracingService>();
		var logger = Substitute.For<ILogger>();
		var service = new ExtendedTracingService(tracingService, logger);

		// Act
		service.Trace("Value: {0}, Name: {1}", 42, "Test");

		// Assert
		tracingService.Received(1).Trace("Value: {0}, Name: {1}", 42, "Test");
		logger.Received(1).LogInformation("Value: {0}, Name: {1}", 42, "Test");
	}

	[Fact]
	public void Trace_WithNullTracingService_ShouldOnlyCallLogger()
	{
		// Arrange
		var logger = Substitute.For<ILogger>();
		var service = new ExtendedTracingService(null, logger);

		// Act
		service.Trace("Test message");

		// Assert
		logger.Received(1).LogInformation("Test message");
	}

	[Fact]
	public void Trace_WithNullLogger_ShouldOnlyCallTracingService()
	{
		// Arrange
		var tracingService = Substitute.For<ITracingService>();
		var service = new ExtendedTracingService(tracingService, null);

		// Act
		service.Trace("Test message");

		// Assert
		tracingService.Received(1).Trace("Test message");
	}

	[Fact]
	public void Trace_WithBothServicesNull_ShouldNotThrow()
	{
		// Arrange
		var service = new ExtendedTracingService(null, null);

		// Act
		var act = () => service.Trace("Test message");

		// Assert
		act.Should().NotThrow();
	}

	#endregion

	#region Trace(string, IPluginExecutionContext, params object[]) Tests

	[Fact]
	public void TraceWithContext_WithValidParameters_ShouldAppendCorrelationInfo()
	{
		// Arrange
		var tracingService = Substitute.For<ITracingService>();
		var logger = Substitute.For<ILogger>();
		var context = Substitute.For<IPluginExecutionContext>();
		var correlationId = Guid.NewGuid();
		var initiatingUserId = Guid.NewGuid();

		context.CorrelationId.Returns(correlationId);
		context.InitiatingUserId.Returns(initiatingUserId);

		var service = new ExtendedTracingService(tracingService, logger);

		// Act
		service.Trace("Test message", context);

		// Assert
		tracingService.Received(1).Trace(
			"{0}, Correlation Id: {1}, Initiating User: {2}",
			"Test message",
			correlationId,
			initiatingUserId);
	}

	[Fact]
	public void TraceWithContext_WithFormatArgs_ShouldFormatMessageBeforeAppendingContext()
	{
		// Arrange
		var tracingService = Substitute.For<ITracingService>();
		var logger = Substitute.For<ILogger>();
		var context = Substitute.For<IPluginExecutionContext>();
		var correlationId = Guid.NewGuid();
		var initiatingUserId = Guid.NewGuid();

		context.CorrelationId.Returns(correlationId);
		context.InitiatingUserId.Returns(initiatingUserId);

		var service = new ExtendedTracingService(tracingService, logger);

		// Act
		service.Trace("Value: {0}", context, 42);

		// Assert
		tracingService.Received(1).Trace(
			"{0}, Correlation Id: {1}, Initiating User: {2}",
			"Value: 42",
			correlationId,
			initiatingUserId);
	}

	[Fact]
	public void TraceWithContext_WithNullContext_ShouldFallbackToSimpleTrace()
	{
		// Arrange
		var tracingService = Substitute.For<ITracingService>();
		var logger = Substitute.For<ILogger>();
		var service = new ExtendedTracingService(tracingService, logger);

		// Act
		service.Trace("Test message", (IPluginExecutionContext)null);

		// Assert
		tracingService.Received(1).Trace("Test message");
		logger.Received(1).LogInformation("Test message");
	}

	[Fact]
	public void TraceWithContext_WithNullMessage_ShouldNotTrace()
	{
		// Arrange
		var tracingService = Substitute.For<ITracingService>();
		var logger = Substitute.For<ILogger>();
		var context = Substitute.For<IPluginExecutionContext>();
		var service = new ExtendedTracingService(tracingService, logger);

		// Act
		service.Trace(null, context);

		// Assert
		tracingService.DidNotReceive().Trace(Arg.Any<string>(), Arg.Any<object[]>());
		logger.DidNotReceive().LogInformation(Arg.Any<string>(), Arg.Any<object[]>());
	}

	[Fact]
	public void TraceWithContext_WithEmptyMessage_ShouldNotTrace()
	{
		// Arrange
		var tracingService = Substitute.For<ITracingService>();
		var logger = Substitute.For<ILogger>();
		var context = Substitute.For<IPluginExecutionContext>();
		var service = new ExtendedTracingService(tracingService, logger);

		// Act
		service.Trace(string.Empty, context);

		// Assert
		tracingService.DidNotReceive().Trace(Arg.Any<string>(), Arg.Any<object[]>());
		logger.DidNotReceive().LogInformation(Arg.Any<string>(), Arg.Any<object[]>());
	}

	[Fact]
	public void TraceWithContext_WithWhitespaceMessage_ShouldNotTrace()
	{
		// Arrange
		var tracingService = Substitute.For<ITracingService>();
		var logger = Substitute.For<ILogger>();
		var context = Substitute.For<IPluginExecutionContext>();
		var service = new ExtendedTracingService(tracingService, logger);

		// Act
		service.Trace("   ", context);

		// Assert
		tracingService.DidNotReceive().Trace(Arg.Any<string>(), Arg.Any<object[]>());
		logger.DidNotReceive().LogInformation(Arg.Any<string>(), Arg.Any<object[]>());
	}

	[Fact]
	public void TraceWithContext_WithEmptyArgs_ShouldNotFormat()
	{
		// Arrange
		var tracingService = Substitute.For<ITracingService>();
		var logger = Substitute.For<ILogger>();
		var context = Substitute.For<IPluginExecutionContext>();
		var correlationId = Guid.NewGuid();
		var initiatingUserId = Guid.NewGuid();

		context.CorrelationId.Returns(correlationId);
		context.InitiatingUserId.Returns(initiatingUserId);

		var service = new ExtendedTracingService(tracingService, logger);

		// Act
		service.Trace("Test message", context, []);

		// Assert
		tracingService.Received(1).Trace(
			"{0}, Correlation Id: {1}, Initiating User: {2}",
			"Test message",
			correlationId,
			initiatingUserId);
	}

	#endregion

	#region Interface Implementation Tests

	[Fact]
	public void ExtendedTracingService_ShouldImplementIExtendedTracingService()
	{
		// Arrange & Act
		var tracingService = Substitute.For<ITracingService>();
		var logger = Substitute.For<ILogger>();
		var service = new ExtendedTracingService(tracingService, logger);

		// Assert
		service.Should().BeAssignableTo<IExtendedTracingService>();
	}

	[Fact]
	public void ExtendedTracingService_ShouldImplementITracingService()
	{
		// Arrange & Act
		var tracingService = Substitute.For<ITracingService>();
		var logger = Substitute.For<ILogger>();
		var service = new ExtendedTracingService(tracingService, logger);

		// Assert
		service.Should().BeAssignableTo<ITracingService>();
	}

	#endregion
}
