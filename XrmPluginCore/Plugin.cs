using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using XrmPluginCore.CustomApis;
using XrmPluginCore.Enums;
using XrmPluginCore.Extensions;
using XrmPluginCore.Interfaces.CustomApi;
using XrmPluginCore.Interfaces.Plugin;
using XrmPluginCore.Plugins;

namespace XrmPluginCore
{
    /// <summary>
    /// Base class for all Plugins.
    /// </summary>
    public abstract class Plugin : IPlugin, IPluginDefinition, ICustomApiDefinition
    {
        private string ChildClassName { get; }
        private string ChildClassShortName { get; }
        private List<PluginStepRegistration> RegisteredPluginSteps { get; } = new List<PluginStepRegistration>();
        private CustomApiRegistration RegisteredCustomApi { get; set; }

        protected Plugin()
        {
            var type = GetType();
            ChildClassName = type.ToString();
            ChildClassShortName = type.Name;
        }

        /// <summary>
        /// Called to allow derived classes to modify the service collection before it is used
        /// </summary>
        protected virtual IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services;
        }

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
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            // Build a local service provider
            var localServiceProvider = serviceProvider.BuildServiceProvider(OnBeforeBuildServiceProvider);

            try
            {
                localServiceProvider.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.Execute()", ChildClassName));
				var context = localServiceProvider.GetService<IPluginExecutionContext>()
					?? throw new Exception("Unable to get Plugin Execution Context");

				// Find the matching registration to determine if we need to register IPluginContext
				var matchingRegistration = GetMatchingRegistration(context);
				var pluginAction = matchingRegistration?.Action;

                // If action is null but we have a handler method name, try to discover generated action wrapper
                if (pluginAction == null && matchingRegistration?.HandlerMethodName != null)
                {
                    pluginAction = DiscoverGeneratedAction(matchingRegistration);
                }

                if (pluginAction == null)
                {
                    // If we have a handler method but no generated wrapper, provide a clear error
                    if (matchingRegistration?.HandlerMethodName != null)
                    {
                        throw new InvalidPluginExecutionException(
                            OperationStatus.Failed,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Plugin step registration for Entity: {0}, Message: {1} in {2} uses method reference " +
                                "'{3}' but no generated ActionWrapper was found. Ensure the source generator is running.",
                                context.PrimaryEntityName,
                                context.MessageName,
                                ChildClassName,
                                matchingRegistration.HandlerMethodName
                            ));
                    }

                    localServiceProvider.Trace(string.Format(
                        CultureInfo.InvariantCulture,
                        "No registered event found for Entity: {0}, Message: {1} in {2}",
                        context.PrimaryEntityName,
                        context.MessageName,
                        ChildClassName
                    ));

                    return;
                }

                localServiceProvider.Trace(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} is firing for Entity: {1}, Message: {2}",
                    ChildClassName,
                    context.PrimaryEntityName,
                    context.MessageName
                    ));

                pluginAction.Invoke(localServiceProvider);
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                localServiceProvider.Trace(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", e.ToString()));
                throw new InvalidPluginExecutionException(OperationStatus.Failed, e.Message);
            }
            catch (NotImplementedException e)
            {
                localServiceProvider.Trace(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", e.ToString()));
                throw new InvalidPluginExecutionException(OperationStatus.Failed, e.Message);
            }
            finally
            {
                localServiceProvider.Trace(string.Format(CultureInfo.InvariantCulture, "Exiting {0}.Execute()", ChildClassName));
            }
        }

        /// <summary>
        /// Get the plugin step configurations.
        /// </summary>
        /// <returns>List of steps</returns>
        public IEnumerable<IPluginStepConfig> GetRegistrations()
            => RegisteredPluginSteps.Select(registration => registration.ConfigBuilder.Build());

        /// <summary>
        /// Get the CustomAPI configuration, if any.
        /// </summary>
        /// <returns>The registered CustomAPI, or null</returns>
        public ICustomApiConfig GetRegistration()
            => RegisteredCustomApi?.ConfigBuilder?.Build();

        /// <summary>
        /// Register a plugin step for the given entity type, event operation, and execution stage with the given action.<br/>
        /// The action will get passed a <see cref="LocalPluginContext"/>.
        /// </summary>
        /// <typeparam name="T">The entity type to register the plugin for</typeparam>
        /// <param name="eventOperation">The event operation to register the plugin for</param>
        /// <param name="executionStage">The execution stage of the plugin registration</param>
        /// <param name="action">The action to execute</param>
        /// <returns>The <see cref="PluginStepConfigBuilder{T}"/> to register filters and images</returns>
        [Obsolete("Use RegisterStep instead")]
        protected PluginStepConfigBuilder<T> RegisterPluginStep<T>(
            EventOperation eventOperation, ExecutionStage executionStage, Action<LocalPluginContext> action)
            where T : Entity
        {
            return RegisterStep<T>(eventOperation.ToString(), executionStage, sp => action(new LocalPluginContext(sp)));
        }

        /// <summary>
        /// Register a plugin step for the given entity type, event operation, and execution stage with the given action.<br/>
        /// The action will get passed a <see cref="LocalPluginContext"/>.<br/>
        /// <br/>
        /// <b>
        /// NOTE: It is strongly adviced to use the <see cref="RegisterPluginStep{T}(EventOperation, ExecutionStage, Action{LocalPluginContext})"/> method instead if possible.<br/>
        /// Only use this method if you are registering for a non-standard message.
        /// </b>
        /// </summary>
        /// <typeparam name="T">The entity type to register the plugin for</typeparam>
        /// <param name="eventOperation">The event operation to register the plugin for</param>
        /// <param name="executionStage">The execution stage of the plugin registration</param>
        /// <param name="action">The action to execute</param>
        /// <returns>The <see cref="PluginStepConfigBuilder{T}"/> to register filters and images</returns>
        [Obsolete("Use RegisterStep instead")]
        protected PluginStepConfigBuilder<T> RegisterPluginStep<T>(
            string eventOperation, ExecutionStage executionStage, Action<LocalPluginContext> action)
            where T : Entity
        {
            return RegisterStep<T>(eventOperation, executionStage, sp => action(new LocalPluginContext(sp)));
        }

        /// <summary>
        /// Register a plugin step for the given entity type, event operation, and execution stage with the given action.<br/>
        /// The action will get passed an instance of <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to register the plugin for</typeparam>
        /// <typeparam name="TService">The service type to pass to the action</typeparam>
        /// <param name="eventOperation">The event operation to register the plugin for</param>
        /// <param name="executionStage">The execution stage of the plugin registration</param>
        /// <param name="action">The action to execute</param>
        /// <returns>The <see cref="PluginStepConfigBuilder{T}"/> to register filters and images</returns>
        protected PluginStepConfigBuilder<TEntity> RegisterStep<TEntity, TService>(
            EventOperation eventOperation, ExecutionStage executionStage, Action<TService> action)
            where TEntity : Entity
        {
            return RegisterStep<TEntity>(eventOperation.ToString(), executionStage, sp => action(sp.GetRequiredService<TService>()));
        }

        /// <summary>
        /// Register a plugin step for the given entity type, event operation, and execution stage with the given action.<br/>
        /// The action will get passed an instance of <typeparamref name="TService"/>.
        /// <br/>
        /// <b>
        /// NOTE: It is strongly adviced to use the <see cref="RegisterStep{TEntity, TService}(EventOperation, ExecutionStage, Action{TService})"/> method instead if possible.<br/>
        /// Only use this method if you are registering for a non-standard message.
        /// </b>
        /// </summary>
        /// <typeparam name="TEntity">The entity type to register the plugin for</typeparam>
        /// <typeparam name="TService">The service type to pass to the action</typeparam>
        /// <param name="eventOperation">The event operation to register the plugin for</param>
        /// <param name="executionStage">The execution stage of the plugin registration</param>
        /// <param name="action">The action to execute</param>
        /// <returns>The <see cref="PluginStepConfigBuilder{T}"/> to register filters and images</returns>
        protected PluginStepConfigBuilder<TEntity> RegisterStep<TEntity, TService>(
            string eventOperation, ExecutionStage executionStage, Action<TService> action)
            where TEntity : Entity
        {
            return RegisterStep<TEntity>(eventOperation, executionStage, sp => action(sp.GetRequiredService<TService>()));
        }

        /// <summary>
        /// Register a plugin step for the given entity type, event operation, and execution stage with the given action.<br/>
        /// The action will get passed a <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The entity type to register the plugin for</typeparam>
        /// <param name="eventOperation">The event operation to register the plugin for</param>
        /// <param name="executionStage">The execution stage of the plugin registration</param>
        /// <param name="action">The action to execute</param>
        /// <returns>The <see cref="PluginStepConfigBuilder{T}"/> to register filters and images</returns>
        protected PluginStepConfigBuilder<T> RegisterStep<T>(
            EventOperation eventOperation, ExecutionStage executionStage, Action<IExtendedServiceProvider> action)
            where T : Entity
        {
            return RegisterStep<T>(eventOperation.ToString(), executionStage, action);
        }

        /// <summary>
        /// Register a plugin step for the given entity type, event operation, and execution stage with the given action.<br/>
        /// The action will get passed a <see cref="IServiceProvider"/>.
        /// <br/>
        /// <b>
        /// NOTE: It is strongly adviced to use the <see cref="RegisterStep{T}(EventOperation, ExecutionStage, Action{IExtendedServiceProvider})"/> method instead if possible.<br/>
        /// Only use this method if you are registering for a non-standard message.
        /// </b>
        /// </summary>
        /// <typeparam name="T">The entity type to register the plugin for</typeparam>
        /// <param name="eventOperation">The event operation to register the plugin for</param>
        /// <param name="executionStage">The execution stage of the plugin registration</param>
        /// <param name="action">The action to execute</param>
        /// <returns>The <see cref="PluginStepConfigBuilder{T}"/> to register filters and images</returns>
        protected PluginStepConfigBuilder<T> RegisterStep<T>(
            string eventOperation, ExecutionStage executionStage, Action<IExtendedServiceProvider> action)
            where T : Entity
        {
            var builder = new PluginStepConfigBuilder<T>(eventOperation, executionStage);
            var registration = new PluginStepRegistration(builder, action)
            {
                // Store metadata for convention-based type-safe wrapper discovery
                EntityTypeName = typeof(T).Name,
                EventOperation = eventOperation,
                ExecutionStage = executionStage.ToString(),
                PluginClassName = ChildClassShortName
            };
            RegisteredPluginSteps.Add(registration);
            return builder;
        }

        /// <summary>
        /// Register a plugin step for the given entity type with a handler method name.
        /// The source generator will emit an ActionWrapper that calls the specified method.
        /// Use WithPreImage/WithPostImage to add images - the method signature must match.
        /// <br/>
        /// Use <c>nameof(IService.MethodName)</c> for compile-time safety.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to register the plugin for</typeparam>
        /// <typeparam name="TService">The service type that contains the handler method</typeparam>
        /// <param name="eventOperation">The event operation to register the plugin for</param>
        /// <param name="executionStage">The execution stage of the plugin registration</param>
        /// <param name="handlerMethodName">The name of the handler method (use nameof(IService.MethodName))</param>
        /// <returns>A <see cref="PluginStepConfigBuilder{TEntity}"/> for configuring images and filtered attributes</returns>
        protected PluginStepConfigBuilder<TEntity> RegisterStep<TEntity, TService>(
            EventOperation eventOperation,
            ExecutionStage executionStage,
            string handlerMethodName)
            where TEntity : Entity
        {
            return RegisterStep<TEntity, TService>(eventOperation.ToString(), executionStage, handlerMethodName);
        }

        /// <summary>
        /// Register a plugin step for the given entity type with a handler method name.
        /// The source generator will emit an ActionWrapper that calls the specified method.
        /// Use WithPreImage/WithPostImage to add images - the method signature must match.
        /// <br/>
        /// Use <c>nameof(IService.MethodName)</c> for compile-time safety.
        /// <br/>
        /// <b>
        /// NOTE: It is strongly advised to use the <see cref="RegisterStep{TEntity, TService}(EventOperation, ExecutionStage, string)"/> method instead if possible.<br/>
        /// Only use this method if you are registering for a non-standard message.
        /// </b>
        /// </summary>
        /// <typeparam name="TEntity">The entity type to register the plugin for</typeparam>
        /// <typeparam name="TService">The service type that contains the handler method</typeparam>
        /// <param name="eventOperation">The event operation to register the plugin for</param>
        /// <param name="executionStage">The execution stage of the plugin registration</param>
        /// <param name="handlerMethodName">The name of the handler method (use nameof(IService.MethodName))</param>
        /// <returns>A <see cref="PluginStepConfigBuilder{TEntity}"/> for configuring images and filtered attributes</returns>
        protected PluginStepConfigBuilder<TEntity> RegisterStep<TEntity, TService>(
            string eventOperation,
            ExecutionStage executionStage,
            string handlerMethodName)
            where TEntity : Entity
        {
            var builder = new PluginStepConfigBuilder<TEntity>(eventOperation, executionStage);

            var registration = new PluginStepRegistration(builder, null)
            {
                EntityTypeName = typeof(TEntity).Name,
                EventOperation = eventOperation,
                ExecutionStage = executionStage.ToString(),
                PluginClassName = ChildClassShortName,
                ServiceTypeName = typeof(TService).Name,
                ServiceTypeFullName = typeof(TService).FullName,
                HandlerMethodName = handlerMethodName
            };

            RegisteredPluginSteps.Add(registration);
            return builder;
        }

        private Action<IExtendedServiceProvider> DiscoverGeneratedAction(PluginStepRegistration registration)
        {
            // Build the wrapper type name using naming convention
            // Format: {Namespace}.PluginRegistrations.{PluginClassName}.{Entity}{Operation}{Stage}.ActionWrapper
            var wrapperTypeName = $"{GetType().Namespace}.PluginRegistrations.{registration.PluginClassName}." +
                                  $"{registration.EntityTypeName}{registration.EventOperation}{registration.ExecutionStage}.ActionWrapper";

            var wrapperType = GetType().Assembly.GetType(wrapperTypeName);
            if (wrapperType == null)
                return null;

            // Use interface instead of reflection
            var wrapper = (IActionWrapper)Activator.CreateInstance(wrapperType);
            return wrapper.CreateAction();
        }

        /// <summary>
        /// <para>
        /// Register a CustomAPI with the given name and action.<br/>
        /// Returns the config builder for specifying additional settings
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">If called multiple times in the same class</exception>
        protected CustomApiConfigBuilder RegisterCustomAPI(string name, Action<LocalPluginContext> action)
        {
            return RegisterAPI(name, sp => action(new LocalPluginContext(sp)));
        }

        /// <summary>
        /// <para>
        /// Register a CustomAPI with the given name and action.<br/>
        /// Returns the config builder for specifying additional settings
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">If called multiple times in the same class</exception>
        protected CustomApiConfigBuilder RegisterAPI<TService>(string name, Action<TService> action)
        {
            return RegisterAPI(name, sp => action(sp.GetRequiredService<TService>()));
        }

        /// <summary>
        /// <para>
        /// Register a CustomAPI with the given name and action.<br/>
        /// Returns the config builder for specifying additional settings
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">If called multiple times in the same class</exception>
        protected CustomApiConfigBuilder RegisterAPI(string name, Action<IExtendedServiceProvider> action)
        {
            if (RegisteredCustomApi != null)
            {
                throw new InvalidOperationException("You cannot register multiple CustomAPIs in the same class");
            }

            var configBuilder = new CustomApiConfigBuilder(name);
            RegisteredCustomApi = new CustomApiRegistration(configBuilder, action);

            return configBuilder;
        }

        private PluginStepRegistration GetMatchingRegistration(IPluginExecutionContext context)
        {
            // Iterate over all of the expected registered events to ensure that the plugin
            // has been invoked by an expected event
            // For any given plug-in event at an instance in time, we would expect at most 1 result to match.
            var pluginStepRegistration = RegisteredPluginSteps.FirstOrDefault(a => a.ConfigBuilder?.Matches(context) == true);

            // If no plugin step found and we have a CustomAPI, return a registration with that action
            if (pluginStepRegistration == null && RegisteredCustomApi != null)
            {
                return new PluginStepRegistration(null, RegisteredCustomApi.Action);
            }

            return pluginStepRegistration;
        }
    }
}
