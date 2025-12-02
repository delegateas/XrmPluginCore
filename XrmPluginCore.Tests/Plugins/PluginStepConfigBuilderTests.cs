using FluentAssertions;
using System;
using System.Reflection;
using Xunit;
using XrmPluginCore.Enums;
using XrmPluginCore.Plugins;
using XrmPluginCore.Tests.Context.BusinessDomain;

namespace XrmPluginCore.Tests.Plugins;

public class PluginStepConfigBuilderTests
{
	[Fact]
	public void FilteredAttributes_WithMultipleAttributes_ReturnsCommaSeparatedLowercaseString()
	{
		// Arrange
		var builder = CreateBuilder();
		builder.AddFilteredAttributes(a => a.Name, a => a.AccountNumber, a => a.Revenue);

		// Act
		var result = builder.FilteredAttributes;

		// Assert
		result.Should().Be("name,accountnumber,revenue");
	}

	[Fact]
	public void FilteredAttributes_MultipleCalls_ReturnsSameCachedValue()
	{
		// Arrange
		var builder = CreateBuilder();
		builder.AddFilteredAttributes(a => a.Name, a => a.AccountNumber);

		// Act - call multiple times
		var result1 = builder.FilteredAttributes;
		var result2 = builder.FilteredAttributes;
		var result3 = builder.FilteredAttributes;

		// Assert - should be the exact same string reference (cached)
		ReferenceEquals(result1, result2).Should().BeTrue(
			"FilteredAttributes should return the cached value on repeated calls");
		ReferenceEquals(result2, result3).Should().BeTrue(
			"FilteredAttributes should return the cached value on repeated calls");
	}

	[Fact]
	public void FilteredAttributes_WithNoAttributes_ReturnsEmptyString()
	{
		// Arrange
		var builder = CreateBuilder();

		// Act
		var result = builder.FilteredAttributes;

		// Assert
		result.Should().Be(string.Empty);
	}

	[Fact]
	public void FilteredAttributes_ReturnsCommaSeparatedLowercaseString()
	{
		// Arrange
		var builder = CreateBuilder();
		builder.AddFilteredAttributes("Name", "AccountNumber", "REVENUE");

		// Act
		var result = builder.FilteredAttributes;

		// Assert
		// Note: String attributes are stored as-is, but FilteredAttributes lowercases the result
		result.Should().Be("name,accountnumber,revenue");
	}

	[Fact]
	public void Build_ReturnsPluginStepConfigWithCorrectFilteredAttributes()
	{
		// Arrange
		var builder = CreateBuilder();
		builder.AddFilteredAttributes(a => a.Name, a => a.AccountNumber);

		// Act
		var config = builder.Build();

		// Assert
		config.FilteredAttributes.Should().Be("name,accountnumber");
	}

	[Fact]
	public void FilteredAttributesCollection_ContainsAddedAttributes()
	{
		// Arrange
		var builder = CreateBuilder();
		builder.AddFilteredAttributes(a => a.Name, a => a.AccountNumber);

		// Assert
		builder.FilteredAttributesCollection.Should().HaveCount(2);
		builder.FilteredAttributesCollection.Should().Contain("Name");
		builder.FilteredAttributesCollection.Should().Contain("AccountNumber");
	}

	[Fact]
	public void FilteredAttributes_CacheIsRecomputedAfterAddingMoreAttributes()
	{
		// Arrange
		var builder = CreateBuilder();
		builder.AddFilteredAttributes(a => a.Name);

		// Act - access filtered attributes to cache the value
		var cachedResult = builder.FilteredAttributes;

		// Add more attributes after caching
		builder.AddFilteredAttributes(a => a.AccountNumber);

		// Access again
		var resultAfterAdding = builder.FilteredAttributes;

		// Assert
		cachedResult.Should().Be("name");
		resultAfterAdding.Should().Be("name,accountnumber");
	}

	private static PluginStepConfigBuilder<Account> CreateBuilder()
	{
		// Use reflection to call internal constructor
		var builderType = typeof(PluginStepConfigBuilder<Account>);
		var constructor = builderType.GetConstructor(
			BindingFlags.NonPublic | BindingFlags.Instance,
			null,
			[typeof(string), typeof(ExecutionStage)],
			null) ?? throw new InvalidOperationException("Could not find internal constructor");

		return (PluginStepConfigBuilder<Account>)constructor.Invoke([nameof(EventOperation.Update), ExecutionStage.PostOperation]);
	}
}
