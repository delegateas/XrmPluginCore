using Microsoft.Xrm.Sdk;

namespace XrmPluginCore;

public interface IEntityImageWrapper<T> where T : Entity
{
    T Entity { get; }
}
