using System;

namespace XrmPluginCore
{
    public class ExtendedServiceProvider : IExtendedServiceProvider
    {
        private readonly IServiceProvider _wrappedServiceProvider;

        public ExtendedServiceProvider(IServiceProvider wrappedServiceProvider)
        {
            _wrappedServiceProvider = wrappedServiceProvider;
        }

        public object GetService(Type serviceType) => _wrappedServiceProvider.GetService(serviceType);
    }
}
