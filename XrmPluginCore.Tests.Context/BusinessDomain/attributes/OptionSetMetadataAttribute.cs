namespace XrmPluginCore.Tests.Context.BusinessDomain;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.24")]
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class OptionSetMetadataAttribute : Attribute
{
    public string Label { get; }

    public int Lcid { get; }

    public OptionSetMetadataAttribute(string label, int lcid)
    {
        Label = label;
        Lcid = lcid;
    }
}
