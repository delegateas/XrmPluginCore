using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace XrmPluginCore.Tests.Context.BusinessDomain;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.20")]
public class DataverseContext : OrganizationServiceContext
{
    public DataverseContext(IOrganizationService service)
        : base(service)
    {
    }

    public IQueryable<Account> AccountSet
    {
        get { return CreateQuery<Account>(); }
    }

    public IQueryable<ActivityParty> ActivityPartySet
    {
        get { return CreateQuery<ActivityParty>(); }
    }

    public IQueryable<Contact> ContactSet
    {
        get { return CreateQuery<Contact>(); }
    }
}