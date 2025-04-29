using DG.XrmPluginCore.Enums;
using DG.XrmPluginCore.Models.Plugin;
using System;
using System.Collections.Generic;

namespace DG.XrmPluginCore.Plugins
{
    public interface IPluginStepConfig
    {
        string EntityLogicalName { get; }
        EventOperation EventOperation { get; }
        ExecutionStage ExecutionStage { get; }

        string Name { get; }
        Deployment Deployment { get; }
        ExecutionMode ExecutionMode { get; }
        int ExecutionOrder { get; }
        string FilteredAttributes { get; }
        Guid? UserContext { get; }

        IEnumerable<ImageSpecification> GetImages();
    }
}
