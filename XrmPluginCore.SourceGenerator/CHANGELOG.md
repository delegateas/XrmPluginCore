### v1.2.3 - 28 November 2025
* Fix: Generate PreImage/PostImage types even when handler signature doesn't match (fixes chicken-and-egg problem where types couldn't be used until they existed)

### v1.2.2 - 27 November 2025
* Fix: XPC4003 has been reduced to Warning to allow initial build to succeed
* Add: New rule XPC4006 (Error) to enforce handler signature correctness once generated types exist

### v1.2.1 - 27 November 2025
* Fix: Analyzer for XPC4005 to correctly identify lambda expressions in AddImage calls

### v1.2.0 - 27 November 2025
Initial release of XrmPluginCore SourceGenerator
* Add: Type-Safe Images feature with compile-time enforcement via source generator
* Add: Source analyzer rules with hotfixes and documentation to help use the Type-Safe Images feature correctly
