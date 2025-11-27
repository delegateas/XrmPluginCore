using System;

namespace XrmPluginCore
{
    /// <summary>
    /// Interface for generated action wrappers that create plugin execution delegates.
    /// </summary>
    public interface IActionWrapper
    {
        /// <summary>
        /// Creates the action delegate that invokes the service method with appropriate images.
        /// </summary>
        Action<IExtendedServiceProvider> CreateAction();
    }
}
