using XrmPluginCore.Interfaces.CustomApi;

namespace XrmPluginCore
{
    public interface ICustomApiDefinition
    {
        ICustomApiConfig GetRegistration();
    }
}
