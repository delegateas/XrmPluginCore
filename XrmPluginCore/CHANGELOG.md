### v1.2.5 - 22 January 2026
* Fix: Avoid naming collisions on generated types when multiple plugins in the same assembly use Type-Safe Images
* Fix: Generate PreImage/PostImage types, even when handler signature does not match
* Fix: Refactored the image registration analyzer to always report XPC3002 for any AddImage usage, regardless of handler syntax (nameof, method reference, or lambda).
XPC3003 is now only reported for lambda invocation with the modern API.

### v1.2.4 - 3 December 2025
* ADD: Support for setting ExecutePrivilegeName
* Fix: Image wrappers now forward to the underlying strongly-typed entity type instead of the base Entity type

### v1.2.3 - 28 November 2025
* Fix: Generate PreImage/PostImage types even when handler signature doesn't match (fixes chicken-and-egg problem where types couldn't be used until they existed)
* Breaking: Reorganized diagnostic IDs by category (XPC1xxx=Info, XPC2xxx=Plugin structure, XPC3xxx=Style, XPC4xxx=Handler methods, XPC5xxx=Internal errors)

### v1.2.2 - 27 November 2025
* Fix: XPC4003 has been reduced to Warning to allow initial build to succeed
* Add: New rule XPC4006 (Error) to enforce handler signature correctness once generated types exist

### v1.2.1 - 27 November 2025
* Fix: Analyzer for XPC4005 to correctly identify lambda expressions in AddImage calls

### v1.2.0 - 27 November 2025
* Add: Type-Safe Images feature with compile-time enforcement via source generator
* Add: Source analyzer rules with hotfixes and documentation to help use the Type-Safe Images feature correctly

### v1.1.1 - 14 November 2025
* Add: IManagedIdentityService to service provider (#1)

### v1.1.0 - 8 October 2025
* Breaking: Change Plugin Step Configs to not use the EventOperation enum but instead use a string to allow for custom messages. RegisterStep supports both enum and string for ease of use.
* Fix: Remove the MessageEntity type since it isn't needed and muddies the waters

### v1.0.1 - 2 October 2025
* Refactor: Merge CustomAPI into Plugin base class for simplicity

### v1.0.0 - 30 September 2025
* Add: Support for XrmBedrock style plugins by wrapping the LocalPluginContext in IServiceProvider
* Add: IPluginExecutionContext extension methods to simplify getting the target and image data
* Add: Ability to overload the IServiceProvider before it is used to create the LocalPluginContext and local IServiceProvider
* Refactor: Update from .NET6 to .NET8 build target
* Refactor: Modifications to CustomAPI definitions to align better with data
* Refactor: Remove Delegate branding after company rebrand to Context&

### v0.0.7 - 5 May 2025
* Add icon to package
* Sign the assembly

### v0.0.6 - 1 May 2025
* Fix: Mark base classes (CustomApi and Plugin) as abstract

### v0.0.5 - 29 April 2025
* Add AsyncAutoDelete field to PluginConfig

### v0.0.4 - 29 April 2025
* Fix: String formatting had invalid indexes

### v0.0.3 - 29 April 2025
* Clean-up and move concrete classes to XrmPluginCore from XrmPluginCore.Abstractions

### v0.0.2 - 25 April 2025
* Fixes to project file so version and dependencies are picked up correctly

### v0.0.1 - 14 March 2025
* Initial release of XrmPluginCore.
