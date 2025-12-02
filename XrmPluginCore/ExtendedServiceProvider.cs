using System;

namespace XrmPluginCore;

public sealed class ExtendedServiceProvider(IServiceProvider wrappedServiceProvider) : IExtendedServiceProvider
{
	public object GetService(Type serviceType) => wrappedServiceProvider.GetService(serviceType);

	public void Dispose()
	{
		if (wrappedServiceProvider is IDisposable disposable)
			disposable.Dispose();
	}
}
