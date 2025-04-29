# XrmPluginCore ![XrmPluginCore NuGet Version](https://img.shields.io/nuget/v/Delegate.XrmPluginCore?label=XrmPluginCore%20NuGet) ![XrmPluginCore.Abstractions NuGet Version](https://img.shields.io/nuget/v/Delegate.XrmPluginCore.Abstractions?label=Abstractions%20NuGet)


XrmPluginCore provides base functionality for developing plugins and custom APIs in Dynamics 365. It includes context wrappers and registration utilities to streamline the development process.

## Features

- **Context Wrappers**: Simplify access to plugin execution context.
- **Registration Utilities**: Easily register plugins and custom APIs.
- **Compatibility**: Supports .NET Standard 2.0, .NET Framework 4.6.2, and .NET 6.

## Usage

### Creating a Plugin

1. Create a new class that inherits from `Plugin`.
2. Register the plugin using the `RegisterPlugin` helper methods.
3. Implement the function in the custom action

```csharp
namespace DG.Some.Namespace {
    using System;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using DG.XrmPluginCore;
    using DG.XrmPluginCore.Enums;

    public class AccountChainPostPlugin : Plugin {

        public AccountChainPostPlugin() {
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute)
                .AddFilteredAttributes(x => x.Fax);

        }

        protected void Execute(LocalPluginContext localContext) {
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

Use DAXIF to automatically register relevant plugin steps and images.

## Contributing

Contributions are welcome! Please submit a pull request or open an issue to discuss your ideas.

## License

This project is licensed under the MIT License.
