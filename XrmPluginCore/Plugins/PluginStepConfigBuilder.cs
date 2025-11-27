using XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Linq;
using XrmPluginCore.Interfaces.Plugin;
using XrmPluginCore.Extensions;

namespace XrmPluginCore.Plugins
{
    /// <summary>
    /// Class to help build the <see cref="IPluginStepConfig"/> for a specific entity type.<br/>
    /// Should be initialized using one of the Plugin.RegisterStep methods.
    /// </summary>
    public class PluginStepConfigBuilder<T> : IPluginStepConfigBuilder where T : Entity
    {
        public string EntityLogicalName { get; }
        public string EventOperation { get; }
        public ExecutionStage ExecutionStage { get; }

        public string Name { get; private set; }
        public Deployment Deployment { get; private set; }
        public ExecutionMode ExecutionMode { get; private set; }
        public int ExecutionOrder { get; private set; }
        public Guid? ImpersonatingUserId { get; private set; }
        public bool AsyncAutoDelete { get; private set; }

        public string FilteredAttributes
        {
            get
            {
                if (FilteredAttributesCollection.Count == 0) return null;
                return string.Join(",", FilteredAttributesCollection).ToLower();
            }
        }

        public Collection<PluginStepImage> Images { get; } = new Collection<PluginStepImage>();

        public Collection<string> FilteredAttributesCollection { get; } = new Collection<string>();

        internal PluginStepConfigBuilder(string eventOperation, ExecutionStage executionStage)
            : this(Activator.CreateInstance<T>().LogicalName, eventOperation, executionStage)
        {
        }

        internal PluginStepConfigBuilder(string entityLogicalName, string eventOperation, ExecutionStage executionStage)
        {
            EntityLogicalName = entityLogicalName;
            EventOperation = eventOperation;
            ExecutionStage = executionStage;
            Deployment = Deployment.ServerOnly;
            ExecutionMode = ExecutionMode.Synchronous;
            ExecutionOrder = 1;
            ImpersonatingUserId = null;
        }

        public IPluginStepConfig Build() =>
            new PluginStepConfig
            {
                Name = Name,
                EntityLogicalName = EntityLogicalName,
                EventOperation = EventOperation,
                ExecutionStage = ExecutionStage,
                ExecutionMode = ExecutionMode,
                ExecutionOrder = ExecutionOrder,
                Deployment = Deployment,
                ImpersonatingUserId = ImpersonatingUserId,
                FilteredAttributes = FilteredAttributes,
                AsyncAutoDelete = AsyncAutoDelete,
                ImageSpecifications = Images.Select(image => new ImageSpecification(image)),
            };

        public bool Matches(IPluginExecutionContext pluginExecutionContext)
        {
            return (int)ExecutionStage == pluginExecutionContext.Stage &&
                EventOperation == pluginExecutionContext.MessageName &&
                (string.IsNullOrWhiteSpace(EntityLogicalName) || EntityLogicalName == pluginExecutionContext.PrimaryEntityName);
        }

        public PluginStepConfigBuilder<T> SetAsyncAutoDelete(bool asyncAutoDelete)
        {
            AsyncAutoDelete = asyncAutoDelete;
            return this;
        }

        public PluginStepConfigBuilder<T> SetDeployment(Deployment deployment)
        {
            Deployment = deployment;
            return this;
        }

        public PluginStepConfigBuilder<T> SetExecutionMode(ExecutionMode executionMode)
        {
            ExecutionMode = executionMode;
            return this;
        }

        public PluginStepConfigBuilder<T> SetName(string name)
        {
            Name = name;
            return this;
        }

        public PluginStepConfigBuilder<T> SetExecutionOrder(int executionOrder)
        {
            ExecutionOrder = executionOrder;
            return this;
        }

        public PluginStepConfigBuilder<T> SetUserContext(Guid userContext)
        {
            ImpersonatingUserId = userContext;
            return this;
        }

        private PluginStepConfigBuilder<T> AddFilteredAttribute(Expression<Func<T, object>> lambda)
        {
            FilteredAttributesCollection.Add(lambda.GetMemberName());
            return this;
        }

        public PluginStepConfigBuilder<T> AddFilteredAttributes(params Expression<Func<T, object>>[] lambdas)
        {
            foreach (var lambda in lambdas)
            {
                AddFilteredAttribute(lambda);
            }

            return this;
        }

        public PluginStepConfigBuilder<T> AddFilteredAttributes(params string[] attributes)
        {
            foreach (var attribute in attributes)
            {
                FilteredAttributesCollection.Add(attribute);
            }
            return this;
        }

        public PluginStepConfigBuilder<T> AddImage(ImageType imageType)
        {
            return AddImage(imageType.ToString(), imageType.ToString(), imageType);
        }

        public PluginStepConfigBuilder<T> AddImage(string name, string entityAlias, ImageType imageType)
        {
            return AddImage(name, entityAlias, imageType, (string[])null);
        }

        public PluginStepConfigBuilder<T> AddImage(ImageType imageType, params string[] attributes)
        {
            return AddImage(imageType.ToString(), imageType.ToString(), imageType, attributes);
        }

        public PluginStepConfigBuilder<T> AddImage(ImageType imageType, params Expression<Func<T, object>>[] attributes)
        {
            return AddImage(imageType.ToString(), imageType.ToString(), imageType, attributes);
        }

        public PluginStepConfigBuilder<T> AddImage(string name, string entityAlias, ImageType imageType, params string[] attributes)
        {
            Images.Add(new PluginStepImage(name, entityAlias, imageType, attributes));

            return this;
        }

        public PluginStepConfigBuilder<T> AddImage(string name, string entityAlias, ImageType imageType, params Expression<Func<T, object>>[] attributes)
        {
            Images.Add(PluginStepImage.Create(name, entityAlias, imageType, attributes));

            return this;
        }

        /// <summary>
        /// Add a PreImage with the specified attributes.
        /// The source generator will create a type-safe PreImage wrapper.
        /// </summary>
        public PluginStepConfigBuilder<T> WithPreImage(params Expression<Func<T, object>>[] attributes)
        {
            return AddImage(ImageType.PreImage, attributes);
        }

        /// <summary>
        /// Add a PostImage with the specified attributes.
        /// The source generator will create a type-safe PostImage wrapper.
        /// </summary>
        public PluginStepConfigBuilder<T> WithPostImage(params Expression<Func<T, object>>[] attributes)
        {
            return AddImage(ImageType.PostImage, attributes);
        }
    }
}
