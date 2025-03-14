using DG.XrmPluginCore.Abstractions.Enums;
using DG.XrmPluginCore.Abstractions.Models.Plugin;
using DG.XrmPluginCore.Extensions;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace DG.XrmPluginCore.Plugins
{
    /// <summary>
    /// Made by Delegate A/S
    /// Class to encapsulate the various configurations that can be made
    /// to a plugin step.
    /// </summary>
    public class PluginStepConfig<T> : IPluginStepConfig where T : Entity
    {
        public string EntityLogicalName { get; }
        public EventOperation EventOperation { get; }
        public ExecutionStage ExecutionStage { get; }

        public string Name { get; private set; }
        public Deployment Deployment { get; private set; }
        public ExecutionMode ExecutionMode { get; private set; }
        public int ExecutionOrder { get; private set; }
        public Guid? UserContext { get; private set; }

        public string FilteredAttributes
        {
            get
            {
                if (FilteredAttributesCollection.Count == 0) return null;
                return string.Join(",", FilteredAttributesCollection).ToLower();
            }
        }

        public Collection<PluginStepImage> Images { get; set; } = new Collection<PluginStepImage>();

        public Collection<string> FilteredAttributesCollection { get; set; } = new Collection<string>();

        public PluginStepConfig(EventOperation eventOperation, ExecutionStage executionStage)
            : this(Activator.CreateInstance<T>().LogicalName, eventOperation, executionStage)
        {
        }

        public PluginStepConfig(string entityLogicalName, EventOperation eventOperation, ExecutionStage executionStage)
        {
            EntityLogicalName = entityLogicalName;
            EventOperation = eventOperation;
            ExecutionStage = executionStage;
            Deployment = Deployment.ServerOnly;
            ExecutionMode = ExecutionMode.Synchronous;
            ExecutionOrder = 1;
            UserContext = null;
        }

        public PluginStepConfig<T> SetDeployment(Deployment deployment)
        {
            Deployment = deployment;
            return this;
        }

        public PluginStepConfig<T> SetExecutionMode(ExecutionMode executionMode)
        {
            ExecutionMode = executionMode;
            return this;
        }

        public PluginStepConfig<T> SetName(string name)
        {
            Name = name;
            return this;
        }

        public PluginStepConfig<T> SetExecutionOrder(int executionOrder)
        {
            ExecutionOrder = executionOrder;
            return this;
        }

        public PluginStepConfig<T> SetUserContext(Guid userContext)
        {
            UserContext = userContext;
            return this;
        }

        private PluginStepConfig<T> AddFilteredAttribute(Expression<Func<T, object>> lambda)
        {
            FilteredAttributesCollection.Add(lambda.GetMemberName());
            return this;
        }

        public PluginStepConfig<T> AddFilteredAttributes(params Expression<Func<T, object>>[] lambdas)
        {
            foreach (var lambda in lambdas)
            {
                AddFilteredAttribute(lambda);
            }

            return this;
        }

        public PluginStepConfig<T> AddFilteredAttributes(params string[] attributes)
        {
            foreach (var attribute in attributes)
            {
                FilteredAttributesCollection.Add(attribute);
            }
            return this;
        }

        public PluginStepConfig<T> AddImage(ImageType imageType)
        {
            return AddImage(imageType.ToString(), imageType.ToString(), imageType);
        }

        public PluginStepConfig<T> AddImage(string name, string entityAlias, ImageType imageType)
        {
            return AddImage(name, entityAlias, imageType, (string[])null);
        }

        public PluginStepConfig<T> AddImage(ImageType imageType, params string[] attributes)
        {
            return AddImage(imageType.ToString(), imageType.ToString(), imageType, attributes);
        }

        public PluginStepConfig<T> AddImage(ImageType imageType, params Expression<Func<T, object>>[] attributes)
        {
            return AddImage(imageType.ToString(), imageType.ToString(), imageType, attributes);
        }

        public PluginStepConfig<T> AddImage(string name, string entityAlias, ImageType imageType, params string[] attributes)
        {
            Images.Add(new PluginStepImage(name, entityAlias, imageType, attributes));
            
            return this;
        }

        public PluginStepConfig<T> AddImage(string name, string entityAlias, ImageType imageType, params Expression<Func<T, object>>[] attributes)
        {
            Images.Add(PluginStepImage.Create(name, entityAlias, imageType, attributes));

            return this;
        }

        public IEnumerable<ImageSpecification> GetImages()
        {
            foreach (var image in Images)
            {
                yield return new ImageSpecification(image.ImageName, image.EntityAlias, image.ImageType, image.Attributes);
            }
        }
    }
}