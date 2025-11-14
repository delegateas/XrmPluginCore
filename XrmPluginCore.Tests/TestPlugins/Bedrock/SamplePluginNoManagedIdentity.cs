using XrmPluginCore.Enums;
using XrmPluginCore.Tests;
using Microsoft.Extensions.DependencyInjection;
using XrmPluginCore;

namespace XrmPluginCore.Tests.TestPlugins.Bedrock
{
    public abstract class PluginBaseNoMangedIdentity : Plugin
    {
        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<ISampleService, SampleServiceNoManagedIdentity>();
        }
    }

    public class SamplePluginNoManagedIdentity : PluginBase
    {
        internal ISampleService SampleService { get; private set; }

        public SamplePluginNoManagedIdentity()
        {
            RegisterStep<TestAccount, ISampleService>(
                EventOperation.Create,
                ExecutionStage.PreOperation,
                s =>
                {
                    // We only do this for testing purposes
                    SampleService = s;

                    // Call the service
                    s.HandleCreate();
                });
        }
    }
}
