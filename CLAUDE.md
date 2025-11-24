# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

XrmPluginCore is a NuGet library that provides base functionality for developing plugins and custom APIs in Microsoft Dynamics 365/Dataverse. It streamlines plugin development through dependency injection, context wrappers, and automatic registration utilities.

The project consists of:
- **XrmPluginCore**: Main implementation library
- **XrmPluginCore.Abstractions**: Interfaces and enums used for plugin/custom API registration
- **XrmPluginCore.SourceGenerator**: Compile-time source generator for type-safe filtered attributes
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
   - `RegisterStep<TEntity, TService>(EventOperation, ExecutionStage, Action<TService>)` - Standard DI-based approach with optional type-safe wrappers
   - `RegisterPluginStep<T>(EventOperation, ExecutionStage, Action<LocalPluginContext>)` - Legacy approach (deprecated)
   - `RegisterAPI<TService>(string name, Action<TService>)` - For Custom APIs

   When `AddFilteredAttributes()` or `AddImage()` are used, the source generator automatically creates wrapper classes that are discovered at runtime by naming convention.

3. **Service Provider Pattern**:
   - `ExtendedServiceProvider` wraps the Dynamics SDK's IServiceProvider
   - `ServiceProviderExtensions.BuildServiceProvider()` creates a scoped DI container per execution
   - Built-in services injected: IPluginExecutionContext, IOrganizationServiceFactory, ITracingService (as ExtendedTracingService), ILogger
   - Type-safe registrations automatically register generated wrapper classes (Target, PreImage, PostImage) directly in DI
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

**XrmPluginCore.SourceGenerator/** (Compile-time code generation)
- `Generators/TargetEntityGenerator.cs` - Incremental source generator that scans for Plugin classes
- `Parsers/RegistrationParser.cs` - Extracts metadata from RegisterStep invocations
- `CodeGeneration/WrapperClassGenerator.cs` - Generates type-safe wrapper classes
- `Helpers/SyntaxHelper.cs` - Roslyn syntax tree analysis utilities
- `Models/PluginStepMetadata.cs` - Data models for storing registration metadata

### Type-Safe Images

The source generator provides compile-time type safety for plugin images (PreImage/PostImage) with **compile-time enforcement** that prevents developers from accidentally ignoring registered images.

#### API Design

Use `WithPreImage`/`WithPostImage` to register images. The `Execute` method signature is **enforced** by the compiler to accept the registered image types:

```csharp
// PreImage only - Execute MUST accept PreImage parameter
RegisterStep<Account, AccountService>(EventOperation.Update, ExecutionStage.PostOperation)
    .AddFilteredAttributes(x => x.Name, x => x.AccountNumber)
    .WithPreImage(x => x.Name, x => x.Revenue)
    .Execute<PreImage>((service, preImage) => service.HandleUpdate(preImage));

// PostImage only - Execute MUST accept PostImage parameter
RegisterStep<Account, AccountService>(EventOperation.Update, ExecutionStage.PostOperation)
    .AddFilteredAttributes(x => x.Name)
    .WithPostImage(x => x.Name, x => x.AccountNumber)
    .Execute<PostImage>((service, postImage) => service.HandleUpdate(postImage));

// Both images - Execute MUST accept both parameters
RegisterStep<Account, AccountService>(EventOperation.Update, ExecutionStage.PostOperation)
    .AddFilteredAttributes(x => x.Name, x => x.AccountNumber)
    .WithPreImage(x => x.Name, x => x.Revenue)
    .WithPostImage(x => x.Name, x => x.AccountNumber)
    .Execute<PreImage, PostImage>((service, pre, post) => service.HandleUpdate(pre, post));
```

**Key benefit**: If you register an image with `WithPreImage`, there is NO way to complete the registration without accepting the image in `Execute`. This prevents developers from accidentally ignoring registered images.

#### How It Works

1. **Compile-Time Analysis**: The source generator scans all classes that inherit from `Plugin` and finds `RegisterStep` calls that use `WithPreImage()` or `WithPostImage()`.

2. **Metadata Extraction**: For each registration, it extracts:
   - Plugin class name
   - Entity type (TEntity)
   - Event operation and execution stage
   - Filtered attributes from `AddFilteredAttributes()` calls
   - Pre/Post image attributes from `WithPreImage()`/`WithPostImage()` calls

3. **Code Generation**: Generates image wrapper classes in isolated namespaces:
   - Namespace: `{Namespace}.PluginImages.{PluginClassName}.{Entity}{Operation}{Stage}`
   - Classes: `PreImage`, `PostImage` (simple names, no prefixes)

4. **Runtime Execution**: When the plugin executes:
   - The `Execute` action is invoked with the service and image instances
   - Images are constructed using `Activator.CreateInstance(typeof(TImage), entity)` from the execution context
   - Services receive strongly-typed image wrappers as parameters

#### Example Usage

```csharp
using XrmPluginCore.Tests.TestPlugins.TypeSafe.PluginImages.AccountPlugin.AccountUpdatePostOperation;

public class AccountPlugin : Plugin
{
    public AccountPlugin()
    {
        // Type-safe API with compile-time enforcement
        RegisterStep<Account, AccountService>(EventOperation.Update, ExecutionStage.PostOperation)
            .AddFilteredAttributes(x => x.Name, x => x.AccountNumber)
            .WithPreImage(x => x.Name, x => x.Revenue)
            .WithPostImage(x => x.Name, x => x.AccountNumber)
            .Execute<PreImage, PostImage>((service, pre, post) => service.HandleUpdate(pre, post));
    }

    protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
    {
        return services.AddScoped<AccountService>();
    }
}

public class AccountService
{
    // Images are passed directly to the method - no DI injection needed
    public void HandleUpdate(PreImage preImage, PostImage postImage)
    {
        var previousName = preImage.Name;     // Type-safe, IntelliSense works
        var previousRevenue = preImage.Revenue;
        var newName = postImage.Name;
    }
}
```

#### Generated Code Example

The source generator creates wrapper classes in isolated namespaces:

```csharp
// Generated in: {Namespace}.PluginImages.AccountPlugin.AccountUpdatePostOperation
namespace YourNamespace.PluginImages.AccountPlugin.AccountUpdatePostOperation
{
    public class PreImage
    {
        private readonly Entity _entity;

        public PreImage(Entity entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string Name => _entity.GetAttributeValue<string>("name");
        public Money Revenue => _entity.GetAttributeValue<Money>("revenue");

        public T ToEntity<T>() where T : Entity => _entity.ToEntity<T>();
    }

    public class PostImage
    {
        private readonly Entity _entity;

        public PostImage(Entity entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string Name => _entity.GetAttributeValue<string>("name");
        public string Accountnumber => _entity.GetAttributeValue<string>("accountnumber");

        public T ToEntity<T>() where T : Entity => _entity.ToEntity<T>();
    }
}
```

#### Builder Pattern

The API uses a type-state builder pattern that enforces image acceptance at compile time:

- `RegisterStep<TEntity, TService>(op, stage)` → returns `PluginStepBuilder`
- `.WithPreImage(...)` → returns `PluginStepBuilderWithPreImage` (must call `Execute<TPreImage>`)
- `.WithPostImage(...)` → returns `PluginStepBuilderWithPostImage` (must call `Execute<TPostImage>`)
- `.WithPreImage(...).WithPostImage(...)` → returns `PluginStepBuilderWithBothImages` (must call `Execute<TPre, TPost>`)

#### Migration from AddImage

The old `AddImage` API is marked as `[Obsolete]`. Migrate to the new API:

```csharp
// Old API (obsolete, no enforcement)
RegisterStep<Account, AccountService>(EventOperation.Update, ExecutionStage.PostOperation,
    service => service.Process())
    .AddImage(ImageType.PreImage, x => x.Name, x => x.Revenue);

// New API (enforced at compile time)
RegisterStep<Account, AccountService>(EventOperation.Update, ExecutionStage.PostOperation)
    .WithPreImage(x => x.Name, x => x.Revenue)
    .Execute<PreImage>((service, preImage) => service.Process(preImage));
```

#### Benefits

- **Compile-time enforcement**: Cannot register an image without accepting it in Execute
- **Type safety**: Wrong image types cause compile errors
- **IntelliSense support**: Auto-completion for available image attributes
- **No runtime overhead**: Simple property accessors, no reflection at access time
- **Null safety**: Missing attributes return null instead of throwing exceptions
- **Namespace isolation**: Each step gets its own namespace, preventing naming conflicts

### Dependency Injection

XrmPluginCore supports three patterns for registering custom services:

#### Pattern 1: Direct Override (Simple, Single Plugin)

Override `OnBeforeBuildServiceProvider()` directly in your plugin class:

```csharp
public class MyPlugin : Plugin
{
    protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
    {
        return services.AddScoped<IMyService, MyService>();
    }
}
```

**Use when**: You have a single plugin class with unique services.

#### Pattern 2: Base Class (Inheritance-based Sharing)

Create a base plugin class that registers shared services, then inherit from it:

```csharp
public class BasePlugin : Plugin
{
    protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
    {
        return services
            .AddScoped<ISharedService, SharedService>()
            .AddScoped<ILogger, Logger>();
    }
}

public class AccountPlugin : BasePlugin { }
public class ContactPlugin : BasePlugin { }
```

**Use when**: Multiple plugins need the same services and share a common inheritance hierarchy.

#### Pattern 3: Extension Method (Composition-based Sharing)

Create static extension methods to encapsulate service registration logic:

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        return services
            .AddScoped<ISharedService, SharedService>()
            .AddScoped<ILogger, Logger>();
    }
}

public class AccountPlugin : Plugin
{
    protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
    {
        return services.AddSharedServices();
    }
}
```

**Use when**: You want to share service registration logic across plugins that may not share inheritance, or when you need to compose multiple service registration modules.

**Note**: Services are scoped to the plugin execution and disposed automatically.

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
