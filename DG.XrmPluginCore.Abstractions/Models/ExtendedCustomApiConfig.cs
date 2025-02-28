using System;
using System.Collections.Generic;
using System.Text;

namespace DG.XrmPluginCore.Abstractions.Models
{
    public class ExtendedCustomApiConfig
    {
        public string PluginType { get; }
        public string OwnerId { get; }
        public string OwnerType { get; }
        public bool IsCustomizable { get; }
        public bool IsPrivate { get; }
        public string ExecutePrivilegeName { get; }
        public string Description { get; }

        public ExtendedCustomApiConfig() { }

        public ExtendedCustomApiConfig(string pluginType, string ownerId, string ownerType, bool isCustomizable, bool isPrivate, string executePrivilegeName, string description)
        {
            PluginType = pluginType;
            OwnerId = ownerId;
            OwnerType = ownerType;
            IsCustomizable = isCustomizable;
            IsPrivate = isPrivate;
            ExecutePrivilegeName = executePrivilegeName;
            Description = description;
        }
    }
}
