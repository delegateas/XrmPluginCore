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