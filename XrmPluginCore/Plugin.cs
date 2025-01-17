
namespace DG.XrmPluginCore
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.ServiceModel;
    using System.Linq.Expressions;
    using Microsoft.Xrm.Sdk;

    // StepConfig           : className, ExecutionStage, EventOperation, LogicalName
    // ExtendedStepConfig   : Deployment, ExecutionMode, Name, ExecutionOrder, FilteredAttributes, UserContext
    // ImageTuple           : Name, EntityAlias, ImageType, Attributes
    using StepConfig = System.Tuple<string, int, string, string>;
    using ExtendedStepConfig = System.Tuple<int, int, string, int, string, string>;
    using ImageTuple = System.Tuple<string, string, int, string>;

    /// <summary>
    /// Base class for all Plugins.
    /// </summary>    
    public class Plugin : IPlugin
    {
        protected class LocalPluginContext
        {
            internal IServiceProvider ServiceProvider
            {
                get;

                private set;
            }

            internal IOrganizationService OrganizationService
            {
                get;

                private set;
            }

            // Delegate A/S added:
            internal IOrganizationService OrganizationAdminService
            {
                get;

                private set;
            }

            internal IPluginExecutionContext PluginExecutionContext
            {
                get;

                private set;
            }

            internal ITracingService TracingService
            {
                get;

                private set;
            }

            private LocalPluginContext()
            {
            }

            internal LocalPluginContext(IServiceProvider serviceProvider)
            {
                if (serviceProvider == null)
                {
                    throw new ArgumentNullException("serviceProvider");
                }

                // Obtain the execution context service from the service provider.
                PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                // Obtain the tracing service from the service provider.
                TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                // Obtain the Organization Service factory service from the service provider
                IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

                // Use the factory to generate the Organization Service.
                OrganizationService = factory.CreateOrganizationService(PluginExecutionContext.UserId);

                // Delegate A/S added: Use the factory to generate the Organization Admin Service.
                OrganizationAdminService = factory.CreateOrganizationService(null);
            }

            internal void Trace(string message)
            {
                if (string.IsNullOrWhiteSpace(message) || TracingService == null)
                {
                    return;
                }

                if (PluginExecutionContext == null)
                {
                    TracingService.Trace(message);
                }
                else
                {
                    TracingService.Trace(
                        "{0}, Correlation Id: {1}, Initiating User: {2}",
                        message,
                        PluginExecutionContext.CorrelationId,
                        PluginExecutionContext.InitiatingUserId);
                }
            }
        }

        private Collection<Tuple<int, string, string, Action<LocalPluginContext>>> registeredEvents;

        /// <summary>
        /// Gets the List of events that the plug-in should fire for. Each List
        /// Item is a <see cref="Tuple"/> containing the Pipeline Stage, Message and (optionally) the Primary Entity. 
        /// In addition, the fourth parameter provide the delegate to invoke on a matching registration.
        /// </summary>
        protected Collection<Tuple<int, string, string, Action<LocalPluginContext>>> RegisteredEvents
        {
            get
            {
                if (registeredEvents == null)
                {
                    registeredEvents = new Collection<Tuple<int, string, string, Action<LocalPluginContext>>>();
                }

                return registeredEvents;
            }
        }

        /// <summary>
        /// Gets or sets the name of the child class.
        /// </summary>
        /// <value>The name of the child class.</value>
        protected string ChildClassName
        {
            get;

            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="childClassName">The <see cref="" cred="Type"/> of the derived class.</param>
        internal Plugin(Type childClassName)
        {
            ChildClassName = childClassName.ToString();
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
                throw new ArgumentNullException("serviceProvider");
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
                     a.Item1 == localcontext.PluginExecutionContext.Stage &&
                     a.Item2 == localcontext.PluginExecutionContext.MessageName &&
                     (string.IsNullOrWhiteSpace(a.Item3) ? true : a.Item3 == localcontext.PluginExecutionContext.PrimaryEntityName)

                     select a.Item4).FirstOrDefault();

                if (entityAction != null)
                {
                    localcontext.Trace(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} is firing for Entity: {1}, Message: {2}",
                        ChildClassName,
                        localcontext.PluginExecutionContext.PrimaryEntityName,
                        localcontext.PluginExecutionContext.MessageName));

                    entityAction.Invoke(localcontext);

                    // now exit - if the derived plug-in has incorrectly registered overlapping event registrations,
                    // guard against multiple executions.
                    return;
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


        // Delegate A/S added:
        /// <summary>
        /// The methods exposes the RegisteredEvents as a collection of tuples
        /// containing:
        /// - The full assembly name of the class containing the RegisteredEvents
        /// - The Pipeline Stage
        /// - The Event Operation
        /// - Logical Entity Name (or empty for all)
        /// This will allow to instantiate each plug-in and iterate through the 
        /// PluginProcessingSteps in order to sync the code repository with 
        /// MS CRM without have to use any extra layer to perform this operation
        /// </summary>
        /// <returns></returns>
        /// 

        public IEnumerable<StepConfig> PluginProcessingSteps()
        {
            var className = ChildClassName;
            foreach (var events in RegisteredEvents)
            {
                yield return new StepConfig
                    (className, events.Item1, events.Item2, events.Item3);
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


        #region PluginStepConfig retrieval
        /// <summary>
        /// Made by Delegate A/S
        /// Get the plugin step configurations.
        /// </summary>
        /// <returns>List of steps</returns>
        public IEnumerable<Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>> PluginProcessingStepConfigs()
        {
            var className = ChildClassName;
            foreach (var config in PluginStepConfigs)
            {
                yield return
                    new Tuple<StepConfig, ExtendedStepConfig, IEnumerable<ImageTuple>>(
                        new StepConfig(className, config._ExecutionStage, config._EventOperation, config._LogicalName),
                        new ExtendedStepConfig(config._Deployment, config._ExecutionMode, config._Name, config._ExecutionOrder, config._FilteredAttributes, config._UserContext.ToString()),
                        config.GetImages());
            }
        }


        protected PluginStepConfig<T> RegisterPluginStep<T>(
            EventOperation eventOperation, ExecutionStage executionStage, Action<LocalPluginContext> action)
            where T : Entity
        {
            PluginStepConfig<T> stepConfig = new PluginStepConfig<T>(eventOperation, executionStage);
            PluginStepConfigs.Add(stepConfig);

            RegisteredEvents.Add(
                new Tuple<int, string, string, Action<LocalPluginContext>>(
                    stepConfig._ExecutionStage,
                    stepConfig._EventOperation,
                    stepConfig._LogicalName,
                    new Action<LocalPluginContext>(action)));

            return stepConfig;
        }


        private Collection<IPluginStepConfig> pluginConfigs;
        private Collection<IPluginStepConfig> PluginStepConfigs
        {
            get
            {
                if (pluginConfigs == null)
                {
                    pluginConfigs = new Collection<IPluginStepConfig>();
                }
                return pluginConfigs;
            }
        }
        #endregion

    }


    #region PluginStepConfig made by Delegate A/S
    public static class HelperPlugin
    {
        public static EventOperation ToEventOperation(this string x)
        {
            return (EventOperation)Enum.Parse(typeof(EventOperation), x);
        }
    }

    interface IPluginStepConfig
    {
        string _LogicalName { get; }
        string _EventOperation { get; }
        int _ExecutionStage { get; }

        string _Name { get; }
        int _Deployment { get; }
        int _ExecutionMode { get; }
        int _ExecutionOrder { get; }
        string _FilteredAttributes { get; }
        Guid _UserContext { get; }
        IEnumerable<ImageTuple> GetImages();
    }

    /// <summary>
    /// Made by Delegate A/S
    /// Class to encapsulate the various configurations that can be made 
    /// to a plugin step.
    /// </summary>
    public class PluginStepConfig<T> : IPluginStepConfig where T : Entity
    {
        public string _LogicalName { get; private set; }
        public string _EventOperation { get; private set; }
        public int _ExecutionStage { get; private set; }

        public string _Name { get; private set; }
        public int _Deployment { get; private set; }
        public int _ExecutionMode { get; private set; }
        public int _ExecutionOrder { get; private set; }
        public Guid _UserContext { get; private set; }

        public Collection<PluginStepImage> _Images = new Collection<PluginStepImage>();
        public Collection<string> _FilteredAttributesCollection = new Collection<string>();

        public string _FilteredAttributes
        {
            get
            {
                if (_FilteredAttributesCollection.Count == 0) return null;
                return string.Join(",", _FilteredAttributesCollection).ToLower();
            }
        }


        public PluginStepConfig(EventOperation eventOperation, ExecutionStage executionStage)
        {
            _LogicalName = Activator.CreateInstance<T>().LogicalName;
            _EventOperation = eventOperation.ToString();
            _ExecutionStage = (int)executionStage;
            _Deployment = (int)Deployment.ServerOnly;
            _ExecutionMode = (int)ExecutionMode.Synchronous;
            _ExecutionOrder = 1;
            _UserContext = Guid.Empty;
        }

        private PluginStepConfig<T> AddFilteredAttribute(Expression<Func<T, object>> lambda)
        {
            _FilteredAttributesCollection.Add(GetMemberName(lambda));
            return this;
        }

        public PluginStepConfig<T> AddFilteredAttributes(params Expression<Func<T, object>>[] lambdas)
        {
            foreach (var lambda in lambdas) AddFilteredAttribute(lambda);
            return this;
        }

        public PluginStepConfig<T> SetDeployment(Deployment deployment)
        {
            _Deployment = (int)deployment;
            return this;
        }

        public PluginStepConfig<T> SetExecutionMode(ExecutionMode executionMode)
        {
            _ExecutionMode = (int)executionMode;
            return this;
        }

        public PluginStepConfig<T> SetName(string name)
        {
            _Name = name;
            return this;
        }

        public PluginStepConfig<T> SetExecutionOrder(int executionOrder)
        {
            _ExecutionOrder = executionOrder;
            return this;
        }

        public PluginStepConfig<T> SetUserContext(Guid userContext)
        {
            _UserContext = userContext;
            return this;
        }

        public PluginStepConfig<T> AddImage(ImageType imageType)
        {
            return AddImage(imageType, null);
        }

        public PluginStepConfig<T> AddImage(ImageType imageType, params Expression<Func<T, object>>[] attributes)
        {
            return AddImage(imageType.ToString(), imageType.ToString(), imageType, attributes);
        }

        public PluginStepConfig<T> AddImage(string name, string entityAlias, ImageType imageType)
        {
            return AddImage(name, entityAlias, imageType, null);
        }

        public PluginStepConfig<T> AddImage(string name, string entityAlias, ImageType imageType, params Expression<Func<T, object>>[] attributes)
        {
            _Images.Add(new PluginStepImage(name, entityAlias, imageType, attributes));
            return this;
        }

        public IEnumerable<ImageTuple> GetImages()
        {
            foreach (var image in _Images)
            {
                yield return new ImageTuple(image.Name, image.EntityAlias, image.ImageType, image.Attributes);
            }
        }

        /// <summary>
        /// Container for information about images attached to steps
        /// </summary>
        public class PluginStepImage
        {
            public string Name { get; private set; }
            public string EntityAlias { get; private set; }
            public int ImageType { get; private set; }
            public string Attributes { get; private set; }

            public PluginStepImage(string name, string entityAlias, ImageType imageType, Expression<Func<T, object>>[] attributes)
            {
                Name = name;
                EntityAlias = entityAlias;
                ImageType = (int)imageType;

                if (attributes != null && attributes.Length > 0)
                {
                    Attributes = string.Join(",", attributes.Select(x => GetMemberName(x))).ToLower();
                }
                else
                {
                    Attributes = null;
                }
            }
        }


        private static string GetMemberName(Expression<Func<T, object>> lambda)
        {
            MemberExpression body = lambda.Body as MemberExpression;

            if (body == null)
            {
                UnaryExpression ubody = (UnaryExpression)lambda.Body;
                body = ubody.Operand as MemberExpression;
            }

            return body.Member.Name;
        }
    }

    class AnyEntity : Entity
    {
        public AnyEntity() : base("") { }
    }

    /**
     * Enums to help setup plugin steps
     */

    public enum ExecutionMode
    {
        Synchronous = 0,
        Asynchronous = 1,
    }

    public enum ExecutionStage
    {
        PreValidation = 10,
        PreOperation = 20,
        PostOperation = 40,
    }

    public enum Deployment
    {
        ServerOnly = 0,
        MicrosoftDynamicsCRMClientforOutlookOnly = 1,
        Both = 2,
    }

    // EventOperation based on CRM 2016
    public enum EventOperation
    {
        AddItem,
        AddListMembers,
        AddMember,
        AddMembers,
        AddPrincipalToQueue,
        AddPrivileges,
        AddProductToKit,
        AddRecurrence,
        AddToQueue,
        AddUserToRecordTeam,
        ApplyRecordCreationAndUpdateRule,
        Assign,
        AssignUserRoles,
        Associate,
        BackgroundSend,
        Book,
        CalculatePrice,
        Cancel,
        CheckIncoming,
        CheckPromote,
        Clone,
        CloneProduct,
        Close,
        CopyDynamicListToStatic,
        CopySystemForm,
        Create,
        CreateException,
        CreateInstance,
        CreateKnowledgeArticleTranslation,
        CreateKnowledgeArticleVersion,
        Delete,
        DeleteOpenInstances,
        DeliverIncoming,
        DeliverPromote,
        DetachFromQueue,
        Disassociate,
        Execute,
        ExecuteById,
        Export,
        ExportAll,
        ExportCompressed,
        ExportCompressedAll,
        GenerateSocialProfile,
        GetDefaultPriceLevel,
        GrantAccess,
        Handle,
        Import,
        ImportAll,
        ImportCompressedAll,
        ImportCompressedWithProgress,
        ImportWithProgress,
        LockInvoicePricing,
        LockSalesOrderPricing,
        Lose,
        Merge,
        ModifyAccess,
        PickFromQueue,
        Publish,
        PublishAll,
        PublishTheme,
        QualifyLead,
        Recalculate,
        ReleaseToQueue,
        RemoveFromQueue,
        RemoveItem,
        RemoveMember,
        RemoveMembers,
        RemovePrivilege,
        RemoveProductFromKit,
        RemoveRelated,
        RemoveUserFromRecordTeam,
        RemoveUserRoles,
        ReplacePrivileges,
        Reschedule,
        Retrieve,
        RetrieveExchangeRate,
        RetrieveFilteredForms,
        RetrieveMultiple,
        RetrievePersonalWall,
        RetrievePrincipalAccess,
        RetrieveRecordWall,
        RetrieveSharedPrincipalsAndAccess,
        RetrieveUnpublished,
        RetrieveUnpublishedMultiple,
        RetrieveUserQueues,
        RevokeAccess,
        Route,
        RouteTo,
        Send,
        SendFromTemplate,
        SetLocLabels,
        SetRelated,
        SetState,
        SetStateDynamicEntity,
        TriggerServiceEndpointCheck,
        UnlockInvoicePricing,
        UnlockSalesOrderPricing,
        Update,
        ValidateRecurrenceRule,
        Win
    }

    public enum ImageType
    {
        PreImage = 0,
        PostImage = 1,
        Both = 2,
    }
    #endregion
}