using DG.XrmPluginCore.Enums;
using System;
using System.Collections.Generic;

namespace DG.XrmPluginCore.Interfaces.Plugin
{
    public interface IPluginStepConfig
    {
        string EntityLogicalName { get; }
        string Name { get; }

        EventOperation EventOperation { get; }
        ExecutionStage ExecutionStage { get; }
        ExecutionMode ExecutionMode { get; }
        int ExecutionOrder { get; }

        Deployment Deployment { get; }

        string FilteredAttributes { get; }

        Guid? ImpersonatingUserId { get; }

        IEnumerable<IImageSpecification> ImageSpecifications { get; }
    }
}
