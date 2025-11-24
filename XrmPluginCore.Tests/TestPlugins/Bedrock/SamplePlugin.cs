using XrmPluginCore.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace XrmPluginCore.Tests.TestPlugins.Bedrock
{
    public abstract class PluginBase : Plugin
    {
        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<ISampleService, SampleService>();
        }
    }

    public class SamplePlugin : PluginBase
    {
        internal ISampleService SampleService { get; private set; }

        public SamplePlugin()
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
