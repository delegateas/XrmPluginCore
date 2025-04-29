
using DG.XrmPluginCore.Enums;
using DG.XrmPluginCore.Extensions;
using DG.XrmPluginCore.Interfaces.Plugin;
using DG.XrmPluginCore.Plugins;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.ServiceModel;

namespace DG.XrmPluginCore
{
    /// <summary>
    /// Base class for all Plugins.
    /// </summary>
    public class Plugin : PluginBase, IPluginDefinition
    {
        /// <summary>
        /// Gets the List of events that the plug-in should fire for.
        /// </summary>
        protected Collection<EventRegistration> RegisteredEvents { get; } = new Collection<EventRegistration>();

        /// <summary>
        /// Executes the plug-in.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <remarks>
        /// For improved performance, Microsoft Dynamics CRM caches plug-in instances. 
        /// The plug-in's Execute method should be written to be stateless as the constructor 
        /// is not called for every invocation of the plug-in. Also, multiple system threads 
        /// could execute the plug-in at the same time. All per invocation state information 
        /// is stored in the context. This means that you should not use global variables in plug-ins.
        /// </remarks>
        public override void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            // Construct the Local plug-in context.
            LocalPluginContext localcontext = new LocalPluginContext(serviceProvider);

            localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.Execute()", ChildClassName));

            try
            {
                // Iterate over all of the expected registered events to ensure that the plugin
                // has been invoked by an expected event
                // For any given plug-in event at an instance in time, we would expect at most 1 result to match.
                var entityAction =
                    RegisteredEvents
                    .FirstOrDefault(a => a.ConfigBuilder?.Matches(localcontext.PluginExecutionContext) == true)?
                    .Action;

                if (entityAction == null)
                {
                    localcontext.Trace(string.Format(
                        CultureInfo.InvariantCulture,
                        "No registered event found for Entity: {0}, Message: {2} in {3}",
                        localcontext.PluginExecutionContext.PrimaryEntityName,
                        localcontext.PluginExecutionContext.MessageName,
                        ChildClassName
                    ));

                    return;
                }

                localcontext.Trace(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} is firing for Entity: {1}, Message: {2}",
                    ChildClassName,
                    localcontext.PluginExecutionContext.PrimaryEntityName,
                    localcontext.PluginExecutionContext.MessageName
                    ));

                entityAction.Invoke(localcontext);
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", e.ToString()));

                // Handle the exception.
                throw;
            }
            finally
            {
                localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Exiting {0}.Execute()", ChildClassName));
            }
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

            var logicalName = (Activator.CreateInstance<T>()).LogicalName;

            if (!context.InputParameters.Contains("Target"))
            {
                trace.Trace("Context does not contain 'Target'");
                return null;
            }

            if (!(context.InputParameters["Target"] is Entity))
            {
                var typeName = context.InputParameters["Target"].GetType().Name;
                trace.Trace("'Target' is not an Entity. It's of type: {0}", typeName);
                return null;
            }

            var entity = (Entity)context.InputParameters["Target"];

            if (logicalName != entity.LogicalName)
            {
                trace.Trace("'Entity' is not of specified type: {0} vs. {1}",
                    entity.LogicalName, logicalName);
                return null;
            }

            return entity.ToEntity<T>();
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
