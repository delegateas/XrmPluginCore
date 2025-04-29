using DG.XrmPluginCore.Interfaces.CustomApi;

namespace DG.XrmPluginCore
{
    public interface ICustomApiDefinition
    {
        ICustomApiConfig GetRegistration();
    }
}
