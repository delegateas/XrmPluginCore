# XrmPluginCore
![XrmPluginCore NuGet Version](https://img.shields.io/nuget/v/XrmPluginCore?label=XrmPluginCore%20NuGet)
![XrmPluginCore.Abstractions NuGet Version](https://img.shields.io/nuget/v/XrmPluginCore.Abstractions?label=Abstractions%20NuGet)
![.NET Framework 4.6.2](https://img.shields.io/badge/.NET-4.6.2-blue)
![.NET 8](https://img.shields.io/badge/.NET-8-blue)

XrmPluginCore provides base functionality for developing plugins and custom APIs in Dynamics 365. It includes context wrappers and registration utilities to streamline the development process.

## Features

- **Dependency Injection**: Modern DI-based plugin architecture with built-in service registration
- **Simple Plugin Creation**: Streamlined workflow for creating and registering plugins
- **Context Wrappers**: Simplify access to plugin execution context
- **Registration Utilities**: Easily register plugins and custom APIs
- **Type-Safe Images**: Compile-time type safety for PreImages and PostImages via source generators
- **Compatibility**: Supports .NET Framework 4.6.2 and .NET 8

## Usage

### Creating a Plugin

Create a service interface and concrete implementation:

```csharp
namespace Some.Namespace {
    interface IMyService {
        void DoSomething();
    }

    public class MyService : IMyService {
        private readonly IOrganizationService _service;
        private readonly IPluginExecutionContext _context;

        public MyService(IOrganizationServiceFactory serviceFactory, IPluginExecutionContext pluginExecutionContext) {
            // Store references to the services if needed
            _service = _serviceFactory.CreateOrganizationService(pluginExecutionContext.UserId);
            _context = pluginExecutionContext;
        });

        public void DoSomething() {
            // Implementation here
            var rand = new Random();

            var newAcc = new Account(_context.PrimaryEntityId) {
                Fax = rand.Next().ToString()
            };

            _service.Update(newAcc);
        }
    }
}
```

Create a base-plugin class that registers the service. Only do this once per assembly, and register all your custom services here:
```csharp
using XrmPluginCore;

namespace Some.Namespace {
    public class BasePlugin : Plugin {
        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {
            return services.AddScoped<IMyService, MyService>();
        }
    }
}
```

Finally, create your plugin class that inherits from the base-plugin, and use dependency injection to get your service:

```csharp
using System;
using XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore;
using XrmPluginCore.Enums;

namespace Some.Namespace {
    public class AccountChainPostPlugin : BasePlugin {
        public AccountChainPostPlugin() {
            RegisterStep<Account, IMyService>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                s => s.DoSomething())
                .AddFilteredAttributes(x => x.Fax);
        }
    }
}
```

### Type-Safe Images

XrmPluginCore includes a source generator that creates type-safe wrapper classes for your plugin images (PreImage/PostImage), giving you compile-time safety and IntelliSense support.

#### Quick Start

```csharp
using XrmPluginCore;
using XrmPluginCore.Enums;
using MyPlugin.PluginRegistrations.AccountUpdatePlugin.AccountUpdatePostOperation;

namespace MyPlugin {
    public class AccountUpdatePlugin : Plugin {
        public AccountUpdatePlugin() {
            // Type-safe API: method reference enables source generator validation
            RegisterStep<Account, IAccountService>(
				EventOperation.Update,
				ExecutionStage.PostOperation,
				service => service.Process)
                .AddFilteredAttributes(x => x.Name, x => x.AccountNumber)
                .WithPreImage(x => x.Name, x => x.Revenue);
                // Source generator validates that Process accepts PreImage parameter
        }
    }

    public interface IAccountService {
        void Process(PreImage preImage);
    }

    public class AccountService : IAccountService {
        public void Process(PreImage preImage) {
            // Type-safe access to pre-image attributes!
            var oldName = preImage.Name;        // IntelliSense works!
            var oldRevenue = preImage.Revenue;  // Type-safe Money access
        }
    }
}
```

**Benefits of type-safe images:**
- **Compile-time enforcement** - Source generator diagnostics ensure handler signature matches registered images
- **IntelliSense support** - Auto-completion for available attributes
- **Null safety** - Proper handling of missing attributes
- **No boilerplate** - Just add a `using` statement for the generated namespace

#### Working with Both Images

```csharp
using MyPlugin.PluginRegistrations.AccountUpdatePlugin.AccountUpdatePostOperation;

public class AccountUpdatePlugin : Plugin {
    public AccountUpdatePlugin() {
        RegisterStep<Account, IAccountService>(
				EventOperation.Update,
				ExecutionStage.PostOperation,
				service => service.Process)
            .AddFilteredAttributes(x => x.Name, x => x.AccountNumber)
            .WithPreImage(x => x.Name, x => x.Revenue)
            .WithPostImage(x => x.Name, x => x.AccountNumber);
            // Handler method must accept both PreImage AND PostImage!
    }
}

public class AccountService : IAccountService {
    public void Process(PreImage preImage, PostImage postImage) {
        var oldRevenue = preImage.Revenue;    // Type-safe pre-image access
        var newAccountNum = postImage.Accountnumber;  // Type-safe post-image access
    }
}
```

**Generated Namespace Convention:**
```
{YourNamespace}.PluginRegistrations.{PluginClassName}.{Entity}{Operation}{Stage}
```
Example: `MyPlugin.PluginRegistrations.AccountUpdatePlugin.AccountUpdatePostOperation`

Inside this namespace you'll find simple class names: `PreImage`, `PostImage`, and `ActionWrapper`

#### XML Documentation (Optional)

The source generator can include XML documentation from your entity properties in the generated wrapper classes, providing IntelliSense tooltips for image attributes.

To enable this feature, add `GenerateDocumentationFile` to your entity project's `.csproj`:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);CS1591</NoWarn> <!-- Suppress missing XML doc warnings -->
</PropertyGroup>
```

When enabled, generated properties will include the original XML documentation:

```csharp
/// <summary>
/// <para>Type an ID number or code for the account...</para>
/// <para>Display Name: Account Number</para>
/// </summary>
public string AccountNumber => Entity.AccountNumber;
```

**Note:** This is optional. Without it, wrapper properties work normally but won't have XML documentation tooltips.

### Using the LocalPluginContext wrapper (Legacy)

**NOTE**: This is only supported for legacy DAXIF/XrmFramework style plugins. It is recommended to use dependency injection based plugins instead.

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

### Injected Services
The following services are available for injection into your plugin or custom API classes:

| Service | Description |
|---------|-------------|
| [IExtendedTracingService](XrmPluginCore/IExtendedTracingService.cs) | Extension to ITracingService with additional helper methods. |
| [ILogger 🔗](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/application-insights-ilogger) | The Plugin Telemetry Service logger interface. |
| [IManagedIdentityService 🔗](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.imanagedidentityservice) | Service to obtain access tokens for Azure resources using Managed Identity. |
| [IOrganizationServiceFactory 🔗](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservicefactory) | Represents a factory for creating IOrganizationService instances. |
| [IPluginExecutionContext 🔗](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.ipluginexecutioncontext) | The plugin execution context provides information about the current plugin execution, including input and output parameters, the message name, and the stage of execution. |
| [IPluginExecutionContext2 🔗](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.ipluginexecutioncontext2) | Extension to IPluginExecutionContext with additional properties and methods. |
| [IPluginExecutionContext3 🔗](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.ipluginexecutioncontext3) | Extension to IPluginExecutionContext2 with additional properties and methods. |
| [IPluginExecutionContext4 🔗](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.ipluginexecutioncontext4) | Extension to IPluginExecutionContext3 with additional properties and methods. |
| [IPluginExecutionContext5 🔗](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.ipluginexecutioncontext5) | Extension to IPluginExecutionContext4 with additional properties and methods. |
| [IPluginExecutionContext6 🔗](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.ipluginexecutioncontext6) | Extension to IPluginExecutionContext5 with additional properties and methods. |
| [IPluginExecutionContext7 🔗](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.ipluginexecutioncontext7) | Extension to IPluginExecutionContext6 with additional properties and methods. |
| [ITracingService 🔗](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.itracingservice) | The tracing service interface. The actual class is the [ExtendedTracingService](https://github.com/delegateas/XrmPluginCore/blob/main/XrmPluginCore/ExtendedTracingService.cs) wrapping the built-in `ITracingService` |

*Note:* Links marked with 🔗 point to official Microsoft documentation.

To register additional services, override the `OnBeforeBuildServiceProvider` method in your plugin or custom API class.

### Registering a Plugin

Use [XrmSync](https://github.com/delegateas/XrmSync) to automatically register relevant plugin steps and images.

#### Including dependent assemblies

XrmPluginCore and XrmSync does not currently support [Dependent Assemblies](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/build-and-package). If your plugin depends on other assemblies, you can use ILRepack or a similar tool to merge the assemblies into a single DLL before deploying.

> [!NOTE]
> Microsoft does not officially support ILMerged assemblies for Dynamics 365 plugins.

To ensure XrmPluginCore, and it's dependencies are included, you can use the following settings for ILRepack:
```xml
<Target Name="ILRepack" AfterTargets="Build">
  <ItemGroup>
    <InputAssemblies Include="$(TargetPath)" />
    <InputAssemblies Include="$(TargetDir)Microsoft.Bcl.AsyncInterfaces.dll" />
    <InputAssemblies Include="$(TargetDir)Microsoft.Extensions.DependencyInjection.Abstractions.dll" />
    <InputAssemblies Include="$(TargetDir)Microsoft.Extensions.DependencyInjection.dll" />
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
