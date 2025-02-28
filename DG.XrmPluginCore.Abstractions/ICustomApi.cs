using DG.XrmPluginCore.Abstractions.Models;

namespace DG.XrmPluginCore.Abstractions
{
    public interface ICustomApi
    {
        CustomApiRegistration GetCustomAPIRegistration();
    }
}
