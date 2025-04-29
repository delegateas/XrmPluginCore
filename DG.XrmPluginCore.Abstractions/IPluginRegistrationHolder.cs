using DG.XrmPluginCore.Models.Plugin;
using System.Collections.Generic;

namespace DG.XrmPluginCore.Abstractions
{
    public interface IPluginRegistrationHolder
    {
        /// <summary>
        /// Get an enumerable of all plugin registrations in the assembly.
        /// </summary>
        IEnumerable<Registration> GetRegistrations();
    }
}
