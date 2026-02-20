using System.Runtime.Serialization;

namespace XrmPluginCore.Tests.Context.BusinessDomain;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.24")]
[DataContract]
#pragma warning disable CS8981
public enum account_accountcategorycode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Preferred Customer", 1033)]
    PreferredCustomer = 1,

    [EnumMember]
    [OptionSetMetadata("Standard", 1033)]
    Standard = 2,
}
