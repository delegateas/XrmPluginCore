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

	// Custom API method names
	public const string RegisterApiMethodName = "RegisterAPI";
	public const string AddRequestParameterMethodName = "AddRequestParameter";
	public const string AddResponsePropertyMethodName = "AddResponseProperty";

	// Custom API generated class name suffixes (combined with the sanitized API name)
	public const string RequestClassSuffix = "Request";
	public const string ResponseClassSuffix = "Response";
	public const string ActionWrapperClassSuffix = "ActionWrapper";

	// Image types (concrete generated wrapper class names)
	public const string PreImageTypeName = "PreImage";
	public const string PostImageTypeName = "PostImage";

	// Shared image interfaces (declared in XrmPluginCore)
	public const string PluginImageInterfaceName = "IPluginImage";
	public const string PreImageInterfaceName = "IPluginPreImage";
	public const string PostImageInterfaceName = "IPluginPostImage";

	// Diagnostic property keys (passed from analyzers to code fix providers)
	public const string PropertyServiceType = "ServiceType";
	public const string PropertyMethodName = "MethodName";
	public const string PropertyHasPreImage = "HasPreImage";
	public const string PropertyHasPostImage = "HasPostImage";
	public const string PropertyImageNamespace = "ImageNamespace";
	public const string PropertyHasArguments = "HasArguments";

	// Diagnostic property keys for Custom API handler fixes
	public const string PropertyRequestTypeName = "RequestTypeName";
	public const string PropertyResponseTypeName = "ResponseTypeName";
	public const string PropertyHasRequest = "HasRequest";
	public const string PropertyHasResponse = "HasResponse";
}
