using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmPluginCore.Plugins
{
    public class PluginStepConfig : IPluginStepConfig
    {
        public bool AsyncAutoDelete { get; internal set; }

        public string EntityLogicalName { get; internal set; }

        public string Name { get; internal set; }

        public string EventOperation { get; internal set; }

        public ExecutionStage ExecutionStage { get; internal set; }

        public ExecutionMode ExecutionMode { get; internal set; }

        public int ExecutionOrder { get; internal set; }

        public Deployment Deployment { get; internal set; }

        public string FilteredAttributes { get; internal set; }

        public Guid? ImpersonatingUserId { get; internal set; }

        public IEnumerable<IImageSpecification> ImageSpecifications { get; internal set; }

        public PluginStepConfig()
        {
            
        }

        public PluginStepConfig(IPluginStepConfig pluginStepConfig)
        {
            EntityLogicalName = pluginStepConfig.EntityLogicalName;
            Name = pluginStepConfig.Name;
            EventOperation = pluginStepConfig.EventOperation;
            ExecutionStage = pluginStepConfig.ExecutionStage;
            ExecutionMode = pluginStepConfig.ExecutionMode;
            ExecutionOrder = pluginStepConfig.ExecutionOrder;
            Deployment = pluginStepConfig.Deployment;
            FilteredAttributes = pluginStepConfig.FilteredAttributes;
            ImpersonatingUserId = pluginStepConfig.ImpersonatingUserId;
            AsyncAutoDelete = pluginStepConfig.AsyncAutoDelete;
            ImageSpecifications = pluginStepConfig.ImageSpecifications.Select(i => new ImageSpecification(i));
        }
    }
}
