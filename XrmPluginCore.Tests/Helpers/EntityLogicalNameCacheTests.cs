using FluentAssertions;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using Xunit;
using XrmPluginCore.Tests.Context.BusinessDomain;

namespace XrmPluginCore.Tests.Helpers;

public class EntityLogicalNameCacheTests
{
	private readonly Type entityLogicalNameCacheType;
	private readonly MethodInfo getLogicalNameMethod;

	public EntityLogicalNameCacheTests()
	{
		// Get the internal EntityLogicalNameCache type via reflection
		entityLogicalNameCacheType = typeof(Plugin).Assembly.GetType("XrmPluginCore.Helpers.EntityLogicalNameCache");
		if (entityLogicalNameCacheType == null)
		{
			throw new InvalidOperationException("Could not find EntityLogicalNameCache type");
		}

		// Get the generic GetLogicalName method
		getLogicalNameMethod = entityLogicalNameCacheType.GetMethod("GetLogicalName", BindingFlags.Public | BindingFlags.Static);
		if (getLogicalNameMethod == null)
		{
			throw new InvalidOperationException("Could not find GetLogicalName method");
		}
	}

	[Fact]
	public void GetLogicalName_ReturnsCorrectLogicalNameForAccount()
	{
		// Arrange
		var genericMethod = getLogicalNameMethod.MakeGenericMethod(typeof(Account));

		// Act
		var result = genericMethod.Invoke(null, null) as string;

		// Assert
		result.Should().Be("account");
	}

	[Fact]
	public void GetLogicalName_ReturnsCorrectLogicalNameForContact()
	{
		// Arrange
		var genericMethod = getLogicalNameMethod.MakeGenericMethod(typeof(Contact));

		// Act
		var result = genericMethod.Invoke(null, null) as string;

		// Assert
		result.Should().Be("contact");
	}

	[Fact]
	public void GetLogicalName_ReturnsDifferentLogicalNamesForDifferentEntities()
	{
		// Arrange
		var accountMethod = getLogicalNameMethod.MakeGenericMethod(typeof(Account));
		var contactMethod = getLogicalNameMethod.MakeGenericMethod(typeof(Contact));

		// Act
		var accountLogicalName = accountMethod.Invoke(null, null) as string;
		var contactLogicalName = contactMethod.Invoke(null, null) as string;

		// Assert
		accountLogicalName.Should().NotBe(contactLogicalName);
		accountLogicalName.Should().Be("account");
		contactLogicalName.Should().Be("contact");
	}

	[Fact]
	public void GetLogicalName_ReturnsCachedValueOnRepeatedCalls()
	{
		// Arrange
		var genericMethod = getLogicalNameMethod.MakeGenericMethod(typeof(Account));

		// Get access to the internal cache for verification
		var cacheField = entityLogicalNameCacheType.GetField(
			"LogicalNameCache",
			BindingFlags.NonPublic | BindingFlags.Static);

		var cache = cacheField?.GetValue(null) as ConcurrentDictionary<Type, string>;

		// Clear the cache before the test to ensure clean state
		// Note: This modifies shared state, but since Entity types are the same,
		// the cache would still contain the same values
		var initialCacheCount = cache?.Count ?? 0;

		// Act - call multiple times
		var result1 = genericMethod.Invoke(null, null) as string;
		var result2 = genericMethod.Invoke(null, null) as string;
		var result3 = genericMethod.Invoke(null, null) as string;

		// Assert
		result1.Should().Be(result2);
		result2.Should().Be(result3);

		// Verify the cache contains the type (caching is working)
		cache.Should().NotBeNull();
		cache.Should().ContainKey(typeof(Account));
	}

	[Fact]
	public void GetLogicalName_CacheReturnsSameInstanceOnRepeatedCalls()
	{
		// Arrange
		var genericMethod = getLogicalNameMethod.MakeGenericMethod(typeof(Account));

		// Act - call twice and get results
		var result1 = genericMethod.Invoke(null, null) as string;
		var result2 = genericMethod.Invoke(null, null) as string;

		// Assert - string interning means these should be the same reference
		// for cached values (ConcurrentDictionary returns the same instance)
		ReferenceEquals(result1, result2).Should().BeTrue(
			"cached logical names should return the same string instance");
	}
}
