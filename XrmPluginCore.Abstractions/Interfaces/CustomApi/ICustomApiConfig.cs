using XrmPluginCore.Enums;
using System;
using System.Collections.Generic;

namespace XrmPluginCore.Interfaces.CustomApi
{
    public interface ICustomApiConfig
    {
        /// <summary>
        /// The type of custom processing step allowed
        /// </summary>
        AllowedCustomProcessingStepType AllowedCustomProcessingStepType { get; }

        /// <summary>
        /// The binding type of the custom API
        /// </summary>
        BindingType BindingType { get; }

        /// <summary>
        /// The logical name of the entity bound to the custom API
        /// </summary>
        string BoundEntityLogicalName { get; }

        /// <summary>
        /// Localized description for custom API instances
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Localized display name for custom API instances
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Indicates if the custom API is enabled as a workflow action
        /// </summary>
        bool EnabledForWorkflow { get; }

        /// <summary>
        /// Name of the privilege that allows execution of the custom API
        /// </summary>
        string ExecutePrivilegeName { get; }

        /// <summary>
        /// For internal use only.
        /// </summary>
        bool IsCustomizable { get; }

        /// <summary>
        /// Indicates if the custom API is a function (GET is supported) or not (POST is supported)
        /// </summary>
        bool IsFunction { get; }

        /// <summary>
        /// Indicates if the custom API is private (hidden from metadata and documentation)
        /// </summary>
        bool IsPrivate { get; }

        /// <summary>
        /// The primary name of the custom API
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Owner Id
        /// </summary>
        Guid? OwnerId { get; }

        /// <summary>
        /// Owner Id Type
        /// </summary>
        OwnerType? OwnerType { get; }

        /// <summary>
        /// Unique name for the custom API
        /// </summary>
        string UniqueName { get; }

        /// <summary>
        /// Enumeration of request parameters for the custom API
        /// </summary>
        IEnumerable<IRequestParameter> RequestParameters { get; }

        /// <summary>
        /// Enumeration of response properties for the custom API
        /// </summary>
        IEnumerable<IResponseProperty> ResponseProperties { get; }
    }
}