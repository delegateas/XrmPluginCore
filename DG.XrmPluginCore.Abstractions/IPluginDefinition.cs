﻿using DG.XrmPluginCore.Interfaces.Plugin;
using System.Collections.Generic;

namespace DG.XrmPluginCore
{
    public interface IPluginDefinition
    {
        /// <summary>
        /// Get an enumerable of all plugin registrations in the assembly.
        /// </summary>
        IEnumerable<IPluginStepConfig> GetRegistrations();
    }
}
