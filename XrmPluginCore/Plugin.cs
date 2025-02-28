
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;

using DG.XrmPluginCore.Plugins;
using DG.XrmPluginCore.Extensions;
using DG.XrmPluginCore.Abstractions;
using DG.XrmPluginCore.Abstractions.Models;
using DG.XrmPluginCore.Abstractions.Enums;

namespace DG.XrmPluginCore
{
    /// <summary>
    /// Base class for all Plugins.
    /// </summary>    
    public class Plugin : PluginBase, IPluginRegistrationHolder
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
                Action<LocalPluginContext> entityAction =
                    (from a in RegisteredEvents
                     where
                     (int)a.StepConfig.ExecutionStage == localcontext.PluginExecutionContext.Stage &&
                     a.StepConfig.EventOperation.ToString() == localcontext.PluginExecutionContext.MessageName &&
                     (string.IsNullOrWhiteSpace(a.StepConfig.EntityLogicalName) || a.StepConfig.EntityLogicalName == localcontext.PluginExecutionContext.PrimaryEntityName)

                     select a.Action).FirstOrDefault();

                if (entityAction != null)
                {
                    localcontext.Trace(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} is firing for Entity: {1}, Message: {2}",
                        ChildClassName,
                        localcontext.PluginExecutionContext.PrimaryEntityName,
                        localcontext.PluginExecutionContext.MessageName));

                    entityAction.Invoke(localcontext);
                }
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

            Entity entity;
            if (collection != null && collection.TryGetValue(name, out entity))
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

        #region PluginRegistration handling retrieval
        /// <summary>
        /// Get the plugin step configurations.
        /// </summary>
        /// <returns>List of steps</returns>
        public IEnumerable<PluginRegistration> PluginRegistrations()
        {
            var className = ChildClassName;
            foreach (var registration in RegisteredEvents)
            {
                var config = registration.StepConfig;

                yield return
                    new PluginRegistration(
                        new StepConfig(
                            className,
                            config.ExecutionStage,
                            config.EventOperation,
                            config.EntityLogicalName),
                        new ExtendedStepConfig(
                            config.Deployment,
                            config.ExecutionMode,
                            config.Name,
                            config.ExecutionOrder,
                            config.FilteredAttributes,
                            config.UserContext
                        ),
                        config.GetImages());
            }
        }

        protected PluginStepConfig<MessageEntity> RegisterPluginStep(
            string pluginMessage, EventOperation eventOperation, ExecutionStage executionStage, Action<LocalPluginContext> action)
        {
            var stepConfig = new PluginStepConfig<MessageEntity>(pluginMessage, eventOperation, executionStage);
            RegisteredEvents.Add(new EventRegistration(stepConfig, new Action<LocalPluginContext>(action)));
            return stepConfig;
        }

        protected PluginStepConfig<T> RegisterPluginStep<T>(
            EventOperation eventOperation, ExecutionStage executionStage, Action<LocalPluginContext> action)
            where T : Entity
        {
            var stepConfig = new PluginStepConfig<T>(eventOperation, executionStage);
            RegisteredEvents.Add(new EventRegistration(stepConfig, new Action<LocalPluginContext>(action)));
            return stepConfig;
        }

        #endregion
    }
}