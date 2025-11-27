## Release 1.2.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
XPC1000 | XrmPluginCore.SourceGenerator | Info     | XPC1000 Generated type-safe wrapper classes
XPC3001 | XrmPluginCore.SourceGenerator | Warning  | XPC3001 Prefer nameof over string literal for handler method
XPC4000 | XrmPluginCore.SourceGenerator | Warning  | XPC4000 Failed to resolve symbol
XPC4001 | XrmPluginCore.SourceGenerator | Warning  | XPC4001 No parameterless constructor found
XPC4002 | XrmPluginCore.SourceGenerator | Error    | XPC4002 Handler method not found
XPC4003 | XrmPluginCore.SourceGenerator | Error    | XPC4003 Handler signature does not match registered images
XPC4004 | XrmPluginCore.SourceGenerator | Warning  | XPC4004 Image registration without method reference
XPC5000 | XrmPluginCore.SourceGenerator | Error    | XPC5000 Failed to generate wrapper classes

## Release 1.2.1

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
XPC4005 | XrmPluginCore.SourceGenerator | Info     | XPC4005 Consider using modern image registration API

## Release 1.2.2

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
XPC4006 | XrmPluginCore.SourceGenerator | Error    | XPC4006 Handler signature mismatch (generated types exist)

### Changed Rules

Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
--------|--------------|--------------|--------------|--------------|-------|-------
XPC4003 | XrmPluginCore.SourceGenerator | Warning | XrmPluginCore.SourceGenerator | Error | Handler signature does not match registered images (generated types don't exist)
