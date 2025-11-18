# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

XrmPluginCore is a NuGet library that provides base functionality for developing plugins and custom APIs in Microsoft Dynamics 365/Dataverse. It streamlines plugin development through dependency injection, context wrappers, and automatic registration utilities.

The project consists of:
- **XrmPluginCore**: Main implementation library
- **XrmPluginCore.Abstractions**: Interfaces and enums used for plugin/custom API registration
- **XrmPluginCore.Tests**: Unit and integration tests

## Build & Test Commands

```bash
# Restore dependencies
dotnet restore

# Build the solution (Release configuration)
dotnet build --configuration Release --no-restore

# Run all tests
dotnet test --configuration Release --no-build --verbosity normal

# Run tests for a specific framework
dotnet test --configuration Release --framework net8.0

# Pack NuGet packages locally
./scripts/Pack-Local.ps1
# OR
dotnet pack --configuration Release --no-build --output ./nupkg
```

## Architecture

### Core Plugin Execution Flow

1. **Plugin Base Class** (`XrmPluginCore/Plugin.cs`): All plugins inherit from this class
   - Implements `IPlugin.Execute(IServiceProvider)` from Dynamics SDK
   - Builds a local scoped service provider for each execution using DI
   - Matches incoming context against registered plugin steps/custom APIs
   - Invokes the appropriate registered action

2. **Registration Pattern**: Plugins register their steps in the constructor using fluent builders:
   - `RegisterStep<TEntity, TService>(EventOperation, ExecutionStage, Action<TService>)` - Modern DI-based approach
   - `RegisterPluginStep<T>(EventOperation, ExecutionStage, Action<LocalPluginContext>)` - Legacy approach (deprecated)
   - `RegisterAPI<TService>(string name, Action<TService>)` - For Custom APIs

3. **Service Provider Pattern**:
   - `ExtendedServiceProvider` wraps the Dynamics SDK's IServiceProvider
   - `ServiceProviderExtensions.BuildServiceProvider()` creates a scoped DI container per execution
   - Built-in services injected: IPluginExecutionContext, IOrganizationServiceFactory, ITracingService (as ExtendedTracingService), ILogger
   - Custom services registered via `OnBeforeBuildServiceProvider()` override

4. **Configuration Builders**:
   - `PluginStepConfigBuilder<T>`: Fluent API for configuring plugin step metadata (filtered attributes, images, deployment, etc.)
   - `CustomApiConfigBuilder`: Fluent API for configuring Custom API metadata (binding type, allowed custom processing steps, parameters, etc.)
   - These builders produce `IPluginStepConfig` and `ICustomApiConfig` consumed by XrmSync for automatic registration

### Project Structure

**XrmPluginCore/** (Main library)
- `Plugin.cs` - Base class for all plugins/custom APIs
- `LocalPluginContext.cs` - Legacy context wrapper (provides OrganizationService, TracingService, etc.)
- `ExtendedTracingService.cs` - Enhanced ITracingService with helper methods
- `ExtendedServiceProvider.cs` - Wrapper for IServiceProvider with DI support
- `Plugins/` - Plugin step registration infrastructure (PluginStepConfigBuilder, ImageSpecification, etc.)
- `CustomApis/` - Custom API registration infrastructure (CustomApiConfigBuilder, RequestParameter, ResponseProperty)
- `Extensions/` - Extension methods for context, service provider, etc.

**XrmPluginCore.Abstractions/** (Shared contracts)
- `Enums/` - EventOperation, ExecutionStage, ExecutionMode, ImageType, CustomApiParameterType, etc.
- `Interfaces/Plugin/` - IPluginStepConfig, IImageSpecification (used by registration tools)
- `Interfaces/CustomApi/` - ICustomApiConfig, IRequestParameter, IResponseProperty
- `IPluginDefinition.cs` - Interface for retrieving plugin step configurations
- `ICustomApiDefinition.cs` - Interface for retrieving custom API configuration

### Dependency Injection

Override `OnBeforeBuildServiceProvider()` in your base plugin class to register services:

```csharp
protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
{
    return services
        .AddScoped<IMyService, MyService>()
        .AddSingleton<IConfiguration, Configuration>();
}
```

Services are scoped to the plugin execution and disposed automatically.

### Multi-Targeting

The library targets both .NET Framework 4.6.2 and .NET 8 to support:
- Traditional on-premise Dynamics 365 deployments (net462)
- Modern Dataverse environments (net8)

Different SDK packages are used per framework:
- net462: `Microsoft.CrmSdk.CoreAssemblies` 9.0.2.59
- net8: `Microsoft.PowerPlatform.Dataverse.Client` 1.2.3

## Key Patterns

### Event Operation Registration

Previously used `EventOperation` enum, but now accepts strings to support custom messages:
```csharp
// Standard operation using enum
RegisterStep<Account, IMyService>(EventOperation.Update, ExecutionStage.PostOperation, s => s.DoSomething())

// Custom message using string
RegisterStep<MyEntity>("custom_CustomMessage", ExecutionStage.PostOperation, s => s.DoSomething())
```

### Plugin Step Images

Images are configured through the builder:
```csharp
RegisterStep<Account, IAccountService>(EventOperation.Update, ExecutionStage.PostOperation, s => s.Process())
    .AddFilteredAttributes(x => x.Name, x => x.AccountNumber)
    .AddPreImage("PreImage", x => x.Name, x => x.Revenue)
    .AddPostImage("PostImage", x => x.Name, x => x.Revenue);
```

### Custom APIs

Custom APIs use a single registration per class:
```csharp
public class MyCustomApi : BasePlugin
{
    public MyCustomApi()
    {
        RegisterAPI<IMyService>("custom_MyApiName", service => service.Execute())
            .WithBindingType(BindingType.Entity)
            .WithBoundEntityLogicalName("account")
            .AddRequestParameter("InputParam", CustomApiParameterType.String, isOptional: false)
            .AddResponseProperty("OutputValue", CustomApiParameterType.Integer);
    }
}
```

## Deployment

Plugins are deployed to Dynamics 365 as ILMerged assemblies containing all dependencies. Use ILRepack to merge:
- XrmPluginCore.dll
- XrmPluginCore.Abstractions.dll
- Microsoft.Extensions.DependencyInjection.dll
- Microsoft.Extensions.DependencyInjection.Abstractions.dll
- Microsoft.Bcl.AsyncInterfaces.dll

Registration is automated using XrmSync (https://github.com/delegateas/XrmSync), which reads the IPluginDefinition and ICustomApiDefinition interfaces.

## Versioning

Version numbers are managed through CHANGELOG.md files:
- `XrmPluginCore/CHANGELOG.md` for the main library
- `XrmPluginCore.Abstractions/CHANGELOG.md` for abstractions

The `Set-VersionFromChangelog.ps1` script updates .csproj files from CHANGELOG during CI/CD.
