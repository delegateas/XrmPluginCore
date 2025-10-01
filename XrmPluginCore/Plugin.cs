using XrmPluginCore;
using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using XrmPluginCore.Extensions;
using XrmPluginCore.Plugins;

namespace XrmPluginCore
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
            return RegisterStep(pluginMessage, eventOperation, executionStage, sp => action(new LocalPluginContext(sp)));
        }

        protected PluginStepConfigBuilder<MessageEntity> RegisterStep(
            string pluginMessage, EventOperation eventOperation, ExecutionStage executionStage, Action<IServiceProvider> action)
        {
            var builder = new PluginStepConfigBuilder<MessageEntity>(pluginMessage, eventOperation, executionStage);
            RegisteredEvents.Add(new EventRegistration(builder, action));
            return builder;
        }

        protected PluginStepConfigBuilder<T> RegisterPluginStep<T>(
            EventOperation eventOperation, ExecutionStage executionStage, Action<LocalPluginContext> action)
            where T : Entity
        {
            return RegisterStep<T>(eventOperation, executionStage, sp => action(new LocalPluginContext(sp)));
        }

        protected PluginStepConfigBuilder<T> RegisterStep<T, TService>(
            EventOperation eventOperation, ExecutionStage executionStage, Action<TService> action)
            where T : Entity
        {
            return RegisterStep<T>(eventOperation, executionStage, sp => action(sp.GetRequiredService<TService>()));
        }

        protected PluginStepConfigBuilder<T> RegisterStep<T>(
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

        protected static T GetEntity<T>(LocalPluginContext context) where T : Entity
        {
            return context.PluginExecutionContext.GetEntity<T>(context.TracingService);
        }

        protected static T GetImage<T>(LocalPluginContext context, ImageType imageType, string name) where T : Entity
        {
            return context.PluginExecutionContext.GetImage<T>(imageType, name);
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
