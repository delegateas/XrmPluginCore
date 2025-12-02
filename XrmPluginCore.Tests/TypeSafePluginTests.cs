using FluentAssertions;
using Microsoft.Xrm.Sdk;
using System;
using Xunit;
using XrmPluginCore.Enums;
using XrmPluginCore.Tests.Helpers;
using XrmPluginCore.Tests.TestPlugins.TypeSafe;
using XrmPluginCore.Tests.Context.BusinessDomain;

namespace XrmPluginCore.Tests;

/// <summary>
/// Tests for type-safe image access via the new builder-based API.
/// Images (PreImage/PostImage) are passed directly to the Execute callback.
/// </summary>
public class TypeSafePluginTests
{
	#region Account Plugin Tests (PreImage + PostImage)

	[Fact]
	public void AccountPlugin_ShouldExecuteWithImages()
	{
		// Arrange
		var plugin = new TypeSafeAccountPlugin();
		var mockProvider = new MockServiceProvider();

		mockProvider.SetupPrimaryEntityName("account");
		mockProvider.SetupMessageName("Update");
		mockProvider.SetupStage((int)ExecutionStage.PreOperation);

		var targetEntity = new Entity("account")
		{
			Id = Guid.NewGuid(),
			["name"] = "Test Account",
			["accountnumber"] = "ACC-001"
		};

		var inputParameters = new ParameterCollection
			{
				{ "Target", targetEntity }
			};
		mockProvider.SetupInputParameters(inputParameters);

		// Setup PreImage
		var preImageEntity = new Entity("account")
		{
			Id = targetEntity.Id,
			["name"] = "Old Account Name",
			["accountnumber"] = "ACC-OLD",
			["sharesoutstanding"] = 50000
		};
		mockProvider.SetupPreEntityImages(new EntityImageCollection { { "PreImage", preImageEntity } });

		// Setup PostImage
		var postImageEntity = new Entity("account")
		{
			Id = targetEntity.Id,
			["name"] = "New Account Name",
			["accountnumber"] = "ACC-001"
		};
		mockProvider.SetupPostEntityImages(new EntityImageCollection { { "PostImage", postImageEntity } });

		// Act
		plugin.Execute(mockProvider.ServiceProvider);

		// Assert
		plugin.UpdateExecuted.Should().BeTrue();
		plugin.LastPreImage.Should().NotBeNull();
		plugin.LastPostImage.Should().NotBeNull();
	}

	[Fact]
	public void AccountPlugin_PreImage_ShouldProvideAttributes()
	{
		// Arrange
		var plugin = new TypeSafeAccountPlugin();
		var mockProvider = new MockServiceProvider();

		mockProvider.SetupPrimaryEntityName("account");
		mockProvider.SetupMessageName("Update");
		mockProvider.SetupStage((int)ExecutionStage.PreOperation);

		var targetEntity = new Entity("account") { Id = Guid.NewGuid() };
		mockProvider.SetupInputParameters(new ParameterCollection { { "Target", targetEntity } });

		// Setup PreImage with specific values
		var preImageEntity = new Entity("account")
		{
			Id = targetEntity.Id,
			["name"] = "PreImage Name",
			["accountnumber"] = "PRE-001",
			["revenue"] = new Money(100000)
		};
		mockProvider.SetupPreEntityImages(new EntityImageCollection { { "PreImage", preImageEntity } });
		mockProvider.SetupPostEntityImages([]);

		// Act
		plugin.Execute(mockProvider.ServiceProvider);

		// Assert - Type-safe access to PreImage attributes
		plugin.LastPreImage.Should().NotBeNull();
		plugin.LastPreImage.Name.Should().Be("PreImage Name");
		plugin.LastPreImage.AccountNumber.Should().Be("PRE-001");
		plugin.LastPreImage.Revenue.Value.Should().Be(100000);
	}

	[Fact]
	public void AccountPlugin_ShouldHandleNoImages()
	{
		// Arrange
		var plugin = new TypeSafeAccountPlugin();
		var mockProvider = new MockServiceProvider();

		mockProvider.SetupPrimaryEntityName("account");
		mockProvider.SetupMessageName("Update");
		mockProvider.SetupStage((int)ExecutionStage.PreOperation);

		var targetEntity = new Entity("account") { Id = Guid.NewGuid() };
		mockProvider.SetupInputParameters(new ParameterCollection { { "Target", targetEntity } });

		// No images
		mockProvider.SetupPreEntityImages([]);
		mockProvider.SetupPostEntityImages([]);

		// Act
		plugin.Execute(mockProvider.ServiceProvider);

		// Assert - Images are null when not present
		plugin.UpdateExecuted.Should().BeTrue();
		plugin.LastPreImage.Should().BeNull();
		plugin.LastPostImage.Should().BeNull();
	}

	#endregion

	#region Contact Plugin Tests (PreImage only)

	[Fact]
	public void ContactPlugin_ShouldExecuteWithPreImage()
	{
		// Arrange
		var plugin = new TypeSafeContactPlugin();
		var mockProvider = new MockServiceProvider();

		mockProvider.SetupPrimaryEntityName("contact");
		mockProvider.SetupMessageName("Create");
		mockProvider.SetupStage((int)ExecutionStage.PostOperation);

		var targetEntity = new Entity("contact") { Id = Guid.NewGuid() };
		mockProvider.SetupInputParameters(new ParameterCollection { { "Target", targetEntity } });

		// Setup PreImage
		var preImageEntity = new Entity("contact")
		{
			Id = targetEntity.Id,
			["firstname"] = "John",
			["lastname"] = "Doe",
			["mobilephone"] = "555-1234"
		};
		mockProvider.SetupPreEntityImages(new EntityImageCollection { { "PreImage", preImageEntity } });

		// Act
		plugin.Execute(mockProvider.ServiceProvider);

		// Assert
		plugin.CreateExecuted.Should().BeTrue();
		plugin.LastPreImage.Should().NotBeNull();
	}

	[Fact]
	public void ContactPlugin_PreImage_ShouldProvideAttributes()
	{
		// Arrange
		var plugin = new TypeSafeContactPlugin();
		var mockProvider = new MockServiceProvider();

		mockProvider.SetupPrimaryEntityName("contact");
		mockProvider.SetupMessageName("Create");
		mockProvider.SetupStage((int)ExecutionStage.PostOperation);

		var targetEntity = new Entity("contact") { Id = Guid.NewGuid() };
		mockProvider.SetupInputParameters(new ParameterCollection { { "Target", targetEntity } });

		// Setup PreImage with specific values
		var preImageEntity = new Entity("contact")
		{
			Id = targetEntity.Id,
			["firstname"] = "Jane",
			["lastname"] = "Smith",
			["mobilephone"] = "555-5678"
		};
		mockProvider.SetupPreEntityImages(new EntityImageCollection { { "PreImage", preImageEntity } });

		// Act
		plugin.Execute(mockProvider.ServiceProvider);

		// Assert - Type-safe access to PreImage attributes
		plugin.LastPreImage.Should().NotBeNull();
		plugin.LastPreImage.Firstname.Should().Be("Jane");
		plugin.LastPreImage.Lastname.Should().Be("Smith");
		plugin.LastPreImage.Mobilephone.Should().Be("555-5678");
	}

	[Fact]
	public void ContactPlugin_ShouldHandleNoPreImage()
	{
		// Arrange
		var plugin = new TypeSafeContactPlugin();
		var mockProvider = new MockServiceProvider();

		mockProvider.SetupPrimaryEntityName("contact");
		mockProvider.SetupMessageName("Create");
		mockProvider.SetupStage((int)ExecutionStage.PostOperation);

		var targetEntity = new Entity("contact") { Id = Guid.NewGuid() };
		mockProvider.SetupInputParameters(new ParameterCollection { { "Target", targetEntity } });

		// No PreImage
		mockProvider.SetupPreEntityImages([]);

		// Act
		plugin.Execute(mockProvider.ServiceProvider);

		// Assert
		plugin.CreateExecuted.Should().BeTrue();
		plugin.LastPreImage.Should().BeNull();
	}

	#endregion

	#region Registration Tests

	[Fact]
	public void AccountPlugin_Registration_ShouldIncludeFilteredAttributes()
	{
		// Arrange
		var plugin = new TypeSafeAccountPlugin();

		// Act
		var registrations = plugin.GetRegistrations();

		// Assert
		var registration = registrations.Should().ContainSingle().Subject;
		registration.FilteredAttributes.Should().Be("name,accountnumber");
	}

	[Fact]
	public void AccountPlugin_Registration_ShouldIncludeImages()
	{
		// Arrange
		var plugin = new TypeSafeAccountPlugin();

		// Act
		var registrations = plugin.GetRegistrations();

		// Assert
		var registration = registrations.Should().ContainSingle().Subject;
		registration.ImageSpecifications.Should().HaveCount(2);
	}

	[Fact]
	public void ContactPlugin_Registration_ShouldIncludeFilteredAttributes()
	{
		// Arrange
		var plugin = new TypeSafeContactPlugin();

		// Act
		var registrations = plugin.GetRegistrations();

		// Assert
		var registration = registrations.Should().ContainSingle().Subject;
		registration.FilteredAttributes.Should().Be("firstname,lastname,emailaddress1");
	}

	#endregion

	#region Entity Property Tests

	[Fact]
	public void AccountPlugin_PreImage_Entity_ShouldReturnEarlyBoundEntity()
	{
		// Arrange
		var plugin = new TypeSafeAccountPlugin();
		var mockProvider = new MockServiceProvider();

		mockProvider.SetupPrimaryEntityName("account");
		mockProvider.SetupMessageName("Update");
		mockProvider.SetupStage((int)ExecutionStage.PreOperation);

		var accountId = Guid.NewGuid();
		var targetEntity = new Entity("account") { Id = accountId };
		mockProvider.SetupInputParameters(new ParameterCollection { { "Target", targetEntity } });

		var preImageEntity = new Entity("account")
		{
			Id = accountId,
			["name"] = "Test Account",
			["accountnumber"] = "ACC-001",
			["sharesoutstanding"] = 50000,
			["revenue"] = new Money(250000)
		};
		mockProvider.SetupPreEntityImages(new EntityImageCollection { { "PreImage", preImageEntity } });
		mockProvider.SetupPostEntityImages([]);

		// Act
		plugin.Execute(mockProvider.ServiceProvider);

		// Assert - Entity property should return early-bound entity
		plugin.LastPreImage.Should().NotBeNull();

		var account = plugin.LastPreImage.Entity;
		account.Should().NotBeNull();
		account.Should().BeOfType<Account>();
		account.Name.Should().Be("Test Account");
		account.AccountNumber.Should().Be("ACC-001");
		account.SharesOutstanding.Should().Be(50000);
		account.Revenue.Value.Should().Be(250000);
	}

	#endregion
}
