using FluentAssertions;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using Xunit;
using XrmPluginCore.Enums;
using XrmPluginCore.Tests.Helpers;
using XrmPluginCore.Tests.TestCustomApis.TypeSafe;

namespace XrmPluginCore.Tests;

/// <summary>
/// End-to-end tests for the type-safe Custom API request/response wrappers. The generated
/// ActionWrapper marshals InputParameters into the request, invokes the handler, and writes the
/// returned response back into OutputParameters.
/// </summary>
public class TypeSafeCustomApiTests
{
	[Fact]
	public void Execute_ShouldMarshalRequestAndResponse()
	{
		// Arrange
		var customApi = new TypeSafeCustomApi();
		var mockProvider = new MockServiceProvider();

		var entityId = Guid.NewGuid();
		mockProvider.SetupInputParameters(new ParameterCollection
		{
			{ "EntityLogicalName", "account" },
			{ "EntityId", entityId },
			{ "Count", 42 },
		});

		var outputParameters = new ParameterCollection();
		mockProvider.SetupOutputParameters(outputParameters);

		// Act
		customApi.Execute(mockProvider.ServiceProvider);

		// Assert - the handler echoed the request into the response, proving both directions worked
		outputParameters.Should().ContainKey("StatusCode");
		outputParameters["StatusCode"].Should().Be(42);

		outputParameters.Should().ContainKey("ErrorMessage");
		outputParameters["ErrorMessage"].Should().Be($"account:{entityId}");
	}

	[Fact]
	public void Execute_ShouldUseDefault_WhenOptionalParameterMissing()
	{
		// Arrange - omit the optional "Count" request parameter
		var customApi = new TypeSafeCustomApi();
		var mockProvider = new MockServiceProvider();

		var entityId = Guid.NewGuid();
		mockProvider.SetupInputParameters(new ParameterCollection
		{
			{ "EntityLogicalName", "contact" },
			{ "EntityId", entityId },
		});

		var outputParameters = new ParameterCollection();
		mockProvider.SetupOutputParameters(outputParameters);

		// Act
		customApi.Execute(mockProvider.ServiceProvider);

		// Assert - missing optional int? maps to null, which the handler turns into -1
		outputParameters["StatusCode"].Should().Be(-1);
		outputParameters["ErrorMessage"].Should().Be($"contact:{entityId}");
	}

	[Fact]
	public void Execute_ShouldDiscoverWrapper_ForDigitStartingApiName()
	{
		// The API name "1DigitApi" sanitizes to "_1DigitApi". This passes only if the runtime's
		// discovery sanitizer and the generator's emission sanitizer agree on the result.
		var customApi = new DigitNamedCustomApi();
		var mockProvider = new MockServiceProvider();
		var outputParameters = new ParameterCollection();
		mockProvider.SetupOutputParameters(outputParameters);

		customApi.Execute(mockProvider.ServiceProvider);

		outputParameters.Should().ContainKey("StatusCode");
		outputParameters["StatusCode"].Should().Be(200);
	}

	[Fact]
	public void Registration_ShouldContainDeclaredParameters()
	{
		// Arrange
		var customApi = new TypeSafeCustomApi();

		// Act
		var registration = customApi.GetRegistration();

		// Assert
		registration.Should().NotBeNull();
		registration!.Name.Should().Be("TypeSafeCustomApi");

		registration.RequestParameters.Select(p => p.UniqueName)
			.Should().BeEquivalentTo(["EntityLogicalName", "EntityId", "Count"]);
		registration.RequestParameters.Single(p => p.UniqueName == "Count").IsOptional.Should().BeTrue();

		registration.ResponseProperties.Select(p => p.UniqueName)
			.Should().BeEquivalentTo(["StatusCode", "ErrorMessage"]);
	}
}
