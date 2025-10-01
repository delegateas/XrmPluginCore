# XrmPluginCore
![XrmPluginCore NuGet Version](https://img.shields.io/nuget/v/XrmPluginCore?label=XrmPluginCore%20NuGet) ![XrmPluginCore.Abstractions NuGet Version](https://img.shields.io/nuget/v/XrmPluginCore.Abstractions?label=Abstractions%20NuGet)

XrmPluginCore provides base functionality for developing plugins and custom APIs in Dynamics 365. It includes context wrappers and registration utilities to streamline the development process.

## Features

- **Context Wrappers**: Simplify access to plugin execution context.
- **Registration Utilities**: Easily register plugins and custom APIs.
- **Compatibility**: Supports .NET Standard 2.0, .NET Framework 4.6.2, and .NET 8.

## Usage

### Creating a Plugin

1. Create a new class that inherits from `Plugin`.
2. Register the plugin using the `RegisterPlugin` helper methods.
3. Implement the function in the custom action

#### Using the IServiceProvider wrapper

```csharp
namespace Some.Namespace {
    using System;
    using XrmFramework.BusinessDomain.ServiceContext;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    public class AccountChainPostPlugin : Plugin {

        public AccountChainPostPlugin() {
            RegisterStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ExecutePlugin)
                .AddFilteredAttributes(x => x.Fax);

        }

        protected void ExecutePlugin(IServiceProvider serviceProvider) {
            var serviceFactory = serviceProvider.GetService<IOrganizationServiceFactory>();
            var executionContext = serviceProvider.GetService<IPluginExecutionContext>();
            var service = serviceFactory.CreateOrganizationService(executionContext.UserId);

            var rand = new Random();

            var newAcc = new Account(localContext.PluginExecutionContext.PrimaryEntityId) {
                Fax = rand.Next().ToString()
            };
            service.Update(newAcc);
        }
    }
}
```

**NOTE** It is recommended to use dependency injection based plugins instead of blindly using the IServiceProvider. See the example in [XrmBedrock](https://github.com/delegateas/XrmBedrock), or the sample plugin in the test project for more information.

#### Using the LocalPluginContext wrapper

```csharp
namespace Some.Namespace {
    using System;
    using XrmFramework.BusinessDomain.ServiceContext;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    public class AccountChainPostPlugin : Plugin {

        public AccountChainPostPlugin() {
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ExecutePlugin)
                .AddFilteredAttributes(x => x.Fax);

        }

        protected void ExecutePlugin(LocalPluginContext localContext) {
            if (localContext == null) {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var rand = new Random();

            var newAcc = new Account(localContext.PluginExecutionContext.PrimaryEntityId) {
                Fax = rand.Next().ToString()
            };
            service.Update(newAcc);
        }
    }
}
```

### Registering a Plugin

Use [XrmSync](https://github.com/delegateas/XrmSync) to automatically register relevant plugin steps and images.

#### Including dependent assemblies

XrmPluginCore and XrmSync does not currently support [Dependent Assemblies](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/build-and-package). If your plugin depends on other assemblies, you can use ILRepack or a similar tool to merge the assemblies into a single DLL before deploying.

To ensure XrmPluginCore, and it's dependencies are included, you can use the following settings for ILRepack:
```xml
<Target Name="ILRepack" AfterTargets="Build">
  <ItemGroup>
    <InputAssemblies Include="$(TargetPath)" />
    <InputAssemblies Include="$(TargetDir)Microsoft.Bcl.AsyncInterfaces.dll" />
    <InputAssemblies Include="$(TargetDir)Microsoft.Extensions.DependencyInjection.Abstractions.dll" />
    <InputAssemblies Include="$(TargetDir)Microsoft.Extensions.DependencyInjection.dll" />
    <InputAssemblies Include="$(TargetDir)Microsoft.Extensions.Logging.Abstractions.dll" />
    <InputAssemblies Include="$(TargetDir)XrmPluginCore.Abstractions.dll" />
    <InputAssemblies Include="$(TargetDir)XrmPluginCore.dll" />
  </ItemGroup>
  <Exec Command="$(PkgILRepack)\tools\ILRepack.exe /parallel /keyfile:[yourkey].snk /lib:$(TargetDir) /out:$(TargetDir)ILMerged.$(TargetFileName) @(InputAssemblies -> '%(Identity)', ' ')" />
</Target>
```

## Contributing

Contributions are welcome! Please submit a pull request or open an issue to discuss your ideas.

## License

This project is licensed under the MIT License.
