using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using Xunit;

namespace XrmPluginCore.Tests;

public class ExtendedServiceProviderTests
{
	[Fact]
	public void ExtendedServiceProvider_ImplementsIDisposable()
	{
		// Arrange & Act - verify at compile-time that IExtendedServiceProvider inherits from IDisposable
		IExtendedServiceProvider provider = new ExtendedServiceProvider(Substitute.For<IServiceProvider>());

		// Assert
		provider.Should().BeAssignableTo<IDisposable>();
	}

	[Fact]
	public void Dispose_WithDisposableWrappedProvider_DisposesWrappedProvider()
	{
		// Arrange
		var disposableProvider = Substitute.For<IServiceProvider, IDisposable>();
		var extendedProvider = new ExtendedServiceProvider(disposableProvider);

		// Act
		extendedProvider.Dispose();

		// Assert
		((IDisposable)disposableProvider).Received(1).Dispose();
	}

	[Fact]
	public void Dispose_WithNonDisposableWrappedProvider_DoesNotThrow()
	{
		// Arrange
		var nonDisposableProvider = Substitute.For<IServiceProvider>();
		var extendedProvider = new ExtendedServiceProvider(nonDisposableProvider);

		// Act
		Action act = () => extendedProvider.Dispose();

		// Assert
		act.Should().NotThrow();
	}

	[Fact]
	public void GetService_DelegatesToWrappedProvider()
	{
		// Arrange
		var expectedService = "test-service";
		var wrappedProvider = Substitute.For<IServiceProvider>();
		wrappedProvider.GetService(typeof(string)).Returns(expectedService);
		var extendedProvider = new ExtendedServiceProvider(wrappedProvider);

		// Act
		var result = extendedProvider.GetService(typeof(string));

		// Assert
		result.Should().Be(expectedService);
		wrappedProvider.Received(1).GetService(typeof(string));
	}

	[Fact]
	public void Dispose_WithServiceProviderFromDI_DisposesCorrectly()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddScoped<IDisposableService, DisposableService>();
		var builtProvider = services.BuildServiceProvider();
		var extendedProvider = new ExtendedServiceProvider(builtProvider);

		// Get a service to ensure scope is active
		var service = extendedProvider.GetService(typeof(IDisposableService)) as DisposableService;

		// Act
		extendedProvider.Dispose();

		// Assert - the service provider should be disposed (attempting to get services after dispose throws)
		Action act = () => builtProvider.GetService<IDisposableService>();
		act.Should().Throw<ObjectDisposedException>();
	}

	private interface IDisposableService : IDisposable { }

	private class DisposableService : IDisposableService
	{
		public bool IsDisposed { get; private set; }
		public void Dispose() => IsDisposed = true;
	}
}
