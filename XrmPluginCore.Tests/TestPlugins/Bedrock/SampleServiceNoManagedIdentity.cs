using Microsoft.Xrm.Sdk;

namespace XrmPluginCore.Tests.TestPlugins.Bedrock
{
    public class SampleServiceNoManagedIdentity : ISampleService
    {
        public bool HandleCreateCalled { get; private set; }
        public IPluginExecutionContext PluginContext { get; }
        public IOrganizationService OrganizationService { get; }

        public SampleServiceNoManagedIdentity(IPluginExecutionContext pluginContext, IOrganizationServiceFactory organizationServiceFactory)
        {
            PluginContext = pluginContext;
            OrganizationService = organizationServiceFactory.CreateOrganizationService(pluginContext.UserId);
        }

        public void HandleCreate()
        {
            HandleCreateCalled = true;
        }
    }
}
