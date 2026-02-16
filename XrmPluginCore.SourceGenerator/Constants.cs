namespace XrmPluginCore.SourceGenerator;

/// <summary>
/// Constants used throughout the source generator
/// </summary>
internal static class Constants
{
	// Plugin framework constants
	public const string PluginBaseClassName = "Plugin";
	public const string PluginNamespace = "XrmPluginCore";
	public const string LogicalNameAttributeName = "AttributeLogicalNameAttribute";

	// Method names
	public const string RegisterStepMethodName = "RegisterStep";
	public const string WithPreImageMethodName = "WithPreImage";
	public const string WithPostImageMethodName = "WithPostImage";
	public const string AddImageMethodName = "AddImage";

	// Image types
	public const string PreImageTypeName = "PreImage";
	public const string PostImageTypeName = "PostImage";

	// Diagnostic property keys (passed from analyzers to code fix providers)
	public const string PropertyServiceType = "ServiceType";
	public const string PropertyMethodName = "MethodName";
	public const string PropertyHasPreImage = "HasPreImage";
	public const string PropertyHasPostImage = "HasPostImage";
	public const string PropertyImageNamespace = "ImageNamespace";
	public const string PropertyHasArguments = "HasArguments";
}
