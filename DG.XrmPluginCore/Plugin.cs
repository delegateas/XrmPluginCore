
using DG.XrmPluginCore.Enums;
using DG.XrmPluginCore.Extensions;
using DG.XrmPluginCore.Interfaces.Plugin;
using DG.XrmPluginCore.Plugins;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DG.XrmPluginCore
{
    /// <summary>
    /// Base class for all Plugins.
    /// </summary>
    public abstract class Plugin : AbstractPlugin, IPluginDefinition
    {
        /// <summary>
        /// Gets the List of events that the plug-in should fire for.
        /// </summary>
        private Collection<EventRegistration> RegisteredEvents { get; } = new Collection<EventRegistration>();

        protected override Action<IServiceProvider> GetAction(IPluginExecutionContext context)
        {
            // Iterate over all of the expected registered events to ensure that the plugin
            // has been invoked by an expected event
            // For any given plug-in event at an instance in time, we would expect at most 1 result to match.
            return
                RegisteredEvents
                .FirstOrDefault(a => a.ConfigBuilder?.Matches(context) == true)?
                .Action;
        }

        /// <summary>
        /// Get the plugin step configurations.
        /// </summary>
        /// <returns>List of steps</returns>
        public IEnumerable<IPluginStepConfig> GetRegistrations()
            => RegisteredEvents.Select(registration => registration.ConfigBuilder.Build());

        protected PluginStepConfigBuilder<MessageEntity> RegisterPluginStep(
            string pluginMessage, EventOperation eventOperation, ExecutionStage executionStage, Action<LocalPluginContext> action)
        {
            var builder = new PluginStepConfigBuilder<MessageEntity>(pluginMessage, eventOperation, executionStage);
            RegisteredEvents.Add(new EventRegistration(builder, action));
            return builder;
        }

        protected PluginStepConfigBuilder<T> RegisterPluginStep<T>(
            EventOperation eventOperation, ExecutionStage executionStage, Action<LocalPluginContext> action)
            where T : Entity
        {
            var builder = new PluginStepConfigBuilder<T>(eventOperation, executionStage);
            RegisteredEvents.Add(new EventRegistration(builder, action));
            return builder;
        }

        protected PluginStepConfigBuilder<T> RegisterPluginStep<T>(
            EventOperation eventOperation, ExecutionStage executionStage, Action<IServiceProvider> action)
            where T : Entity
        {
            var builder = new PluginStepConfigBuilder<T>(eventOperation, executionStage);
            RegisteredEvents.Add(new EventRegistration(builder, action));
            return builder;
        }

        #region Additional helper methods

        protected static bool MatchesEventOperation(LocalPluginContext context, params EventOperation[] operations)
        {
            return MatchesEventOperation(context.PluginExecutionContext, operations);
        }

        protected static bool MatchesEventOperation(IPluginExecutionContext context, params EventOperation[] operations)
        {
            var operation = context.MessageName.ToEventOperation();
            return operations.Any(o => o == operation);
        }

        protected static T GetEntity<T>(LocalPluginContext localPluginContext) where T : Entity
        {
            var context = localPluginContext.PluginExecutionContext;
            var trace = localPluginContext.TracingService;

            return GetEntity<T>(context, trace);
        }

        protected static T GetEntity<T>(IPluginExecutionContext context, ITracingService trace) where T : Entity
        {
            var logicalName = (Activator.CreateInstance<T>()).LogicalName;

            if (!context.InputParameters.Contains("Target"))
            {
                trace.Trace("Context does not contain 'Target'");
                return null;
            }

            var target = context.InputParameters["Target"];

            if (target is Entity entity)
            {
                if (logicalName != entity.LogicalName)
                {
                    trace.Trace("'Entity' is not of specified type: {0} vs. {1}",
                        entity.LogicalName, logicalName);
                    return null;
                }

                return entity.ToEntity<T>();
            }

            var typeName = target.GetType().Name;
            trace.Trace("'Target' is not an Entity. It's of type: {0}", typeName);
            return null;
        }

        protected static T GetImage<T>(LocalPluginContext context, ImageType imageType, string name) where T : Entity
        {
            EntityImageCollection collection = null;
            if (imageType == ImageType.PreImage)
            {
                collection = context.PluginExecutionContext.PreEntityImages;
            }
            else if (imageType == ImageType.PostImage)
            {
                collection = context.PluginExecutionContext.PostEntityImages;
            }

            if (collection != null && collection.TryGetValue(name, out var entity))
            {
                return entity.ToEntity<T>();
            }
            else
            {
                return null;
            }
        }

        protected static T GetImage<T>(LocalPluginContext context, ImageType imageType) where T : Entity
        {
            return GetImage<T>(context, imageType, imageType.ToString());
        }

        protected static T GetPreImage<T>(LocalPluginContext context, string name = "PreImage") where T : Entity
        {
            return GetImage<T>(context, ImageType.PreImage, name);
        }

        protected static T GetPostImage<T>(LocalPluginContext context, string name = "PostImage") where T : Entity
        {
            return GetImage<T>(context, ImageType.PostImage, name);
        }

        #endregion
    }
}
