### v1.2.5 - 22 January 2026
* Fix: Avoid naming collisions on generated types when multiple plugins in the same assembly use Type-Safe Images
* Fix: Generate PreImage/PostImage types, even when handler signature does not match
* Fix: Refactored the image registration analyzer to always report XPC3002 for any AddImage usage, regardless of handler syntax (nameof, method reference, or lambda).
XPC3003 is now only reported for lambda invocation with the modern API.

### v1.2.4 - 3 December 2025
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
Initial release of XrmPluginCore SourceGenerator
* Add: Type-Safe Images feature with compile-time enforcement via source generator
* Add: Source analyzer rules with hotfixes and documentation to help use the Type-Safe Images feature correctly
