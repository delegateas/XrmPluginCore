using DG.XrmPluginCore.Enums;
using System;
using System.Collections.Generic;

namespace DG.XrmPluginCore.Interfaces.Plugin
{
    public interface IPluginStepConfig
    {
        string EntityLogicalName { get; }

        /// <summary>
        /// Name of SdkMessage processing step.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Event operation that the SDK message processing step is registered for.
        /// </summary>
        EventOperation EventOperation { get; }

        /// <summary>
        /// Stage in the execution pipeline that the SDK message processing step is in.
        /// </summary>
        ExecutionStage ExecutionStage { get; }

        /// <summary>
        /// Run-time mode of execution, for example, synchronous or asynchronous.
        /// </summary>
        ExecutionMode ExecutionMode { get; }

        /// <summary>
        /// Processing order within the stage.
        /// </summary>
        int ExecutionOrder { get; }

        /// <summary>
        /// Deployment that the SDK message processing step should be executed on; server, client, or both.
        /// </summary>
        Deployment Deployment { get; }

        /// <summary>
        /// Comma-separated list of attributes. If at least one of these attributes is modified, the plug-in should execute.
        /// </summary>
        string FilteredAttributes { get; }

        /// <summary>
        /// Unique identifier of the user to impersonate context when step is executed.
        /// </summary>
        Guid? ImpersonatingUserId { get; }

        /// <summary>
        /// Indicates whether the asynchronous system job is automatically deleted on completion.
        /// </summary>
        bool AsyncAutoDelete { get; }

        /// <summary>
        /// List of pre and post image specifications for the step.
        /// </summary>
        IEnumerable<IImageSpecification> ImageSpecifications { get; }
    }
}
