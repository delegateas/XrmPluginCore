using Microsoft.Xrm.Sdk;

namespace XrmPluginCore
{
    /// <summary>
    /// Represents a type-safe wrapper around an entity image (PreImage or PostImage)
    /// with conversion capabilities to early-bound entity types.
    /// </summary>
    public interface IEntityImageWrapper
    {
        /// <summary>
        /// Converts the underlying entity to a strongly-typed early-bound entity.
        /// </summary>
        /// <typeparam name="T">The early-bound entity type</typeparam>
        /// <returns>A strongly-typed entity instance</returns>
        T ToEntity<T>() where T : Entity;

        /// <summary>
        /// Gets the underlying Entity object for direct attribute access or service operations.
        /// </summary>
        /// <returns>The underlying Entity instance</returns>
        Entity GetUnderlyingEntity();
    }
}
