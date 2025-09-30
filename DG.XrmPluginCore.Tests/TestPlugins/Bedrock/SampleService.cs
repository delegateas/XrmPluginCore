using Microsoft.Xrm.Sdk;

namespace DG.XrmPluginCore.Tests.TestPlugins.Bedrock
{
    public interface ISampleService
    {
        void HandleCreate();
    }

    public class SampleService : ISampleService
    {
        public bool HandleCreateCalled { get; private set; }
        public IPluginExecutionContext PluginContext { get; }
        public IOrganizationService OrganizationService { get; }

        public SampleService(IPluginExecutionContext pluginContext, IOrganizationServiceFactory organizationServiceFactory)
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
