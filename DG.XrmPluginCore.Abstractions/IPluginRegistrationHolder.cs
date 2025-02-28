using DG.XrmPluginCore.Abstractions.Models;
using System.Collections.Generic;

namespace DG.XrmPluginCore.Abstractions
{
    public interface IPluginRegistrationHolder
    {
        IEnumerable<PluginRegistration> PluginRegistrations();
    }
}
