namespace XrmPluginCore.SourceGenerator.Tests.Helpers;

/// <summary>
/// Provides reusable test fixtures for source generator testing.
/// </summary>
public static class TestFixtures
{
    /// <summary>
    /// Sample Account entity class with common attributes.
    /// </summary>
    public const string AccountEntity = @"
using System;
using System.ComponentModel;
using Microsoft.Xrm.Sdk;

namespace TestNamespace
{
    [EntityLogicalName(""account"")]
    public class Account : Entity
    {
        public const string EntityLogicalName = ""account"";

        public Account() : base(EntityLogicalName) { }

        [AttributeLogicalName(""name"")]
        public string Name
        {
            get => GetAttributeValue<string>(""name"");
            set => SetAttributeValue(""name"", value);
        }

        [AttributeLogicalName(""accountnumber"")]
        public string AccountNumber
        {
            get => GetAttributeValue<string>(""accountnumber"");
            set => SetAttributeValue(""accountnumber"", value);
        }

        [AttributeLogicalName(""revenue"")]
        public Money Revenue
        {
            get => GetAttributeValue<Money>(""revenue"");
            set => SetAttributeValue(""revenue"", value);
        }

        [AttributeLogicalName(""industrycode"")]
        public OptionSetValue IndustryCode
        {
            get => GetAttributeValue<OptionSetValue>(""industrycode"");
            set => SetAttributeValue(""industrycode"", value);
        }

        [AttributeLogicalName(""primarycontactid"")]
        public EntityReference PrimaryContactId
        {
            get => GetAttributeValue<EntityReference>(""primarycontactid"");
            set => SetAttributeValue(""primarycontactid"", value);
        }
    }
}";

    /// <summary>
    /// Sample Contact entity class with common attributes.
    /// </summary>
    public const string ContactEntity = @"
using System;
using System.ComponentModel;
using Microsoft.Xrm.Sdk;

namespace TestNamespace
{
    [EntityLogicalName(""contact"")]
    public class Contact : Entity
    {
        public const string EntityLogicalName = ""contact"";

        public Contact() : base(EntityLogicalName) { }

        [AttributeLogicalName(""firstname"")]
        public string FirstName
        {
            get => GetAttributeValue<string>(""firstname"");
            set => SetAttributeValue(""firstname"", value);
        }

        [AttributeLogicalName(""lastname"")]
        public string LastName
        {
            get => GetAttributeValue<string>(""lastname"");
            set => SetAttributeValue(""lastname"", value);
        }

        [AttributeLogicalName(""emailaddress1"")]
        public string EmailAddress
        {
            get => GetAttributeValue<string>(""emailaddress1"");
            set => SetAttributeValue(""emailaddress1"", value);
        }
    }
}";

    /// <summary>
    /// Plugin with PreImage only using WithPreImage.
    /// </summary>
    public static string GetPluginWithPreImage(string entityClass = "Account") => $@"
using XrmPluginCore;
using XrmPluginCore.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using TestNamespace;

namespace TestNamespace
{{
    public class TestPlugin : Plugin
    {{
        public TestPlugin()
        {{
            RegisterStep<{entityClass}, ITestService>(EventOperation.Update, ExecutionStage.PostOperation)
                .AddFilteredAttributes(x => x.{(entityClass == "Account" ? "Name" : "FirstName")})
                .WithPreImage(x => x.{(entityClass == "Account" ? "Name" : "FirstName")}, x => x.{(entityClass == "Account" ? "Revenue" : "EmailAddress")})
                .Execute<PreImage>((service, preImage) => service.Process(preImage));
        }}

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {{
            return services.AddScoped<ITestService, TestService>();
        }}
    }}

    public interface ITestService
    {{
        void Process(object image);
    }}

    public class TestService : ITestService
    {{
        public void Process(object image) {{ }}
    }}
}}";

    /// <summary>
    /// Plugin with PostImage only using WithPostImage.
    /// </summary>
    public static string GetPluginWithPostImage(string entityClass = "Account") => $@"
using XrmPluginCore;
using XrmPluginCore.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using TestNamespace;

namespace TestNamespace
{{
    public class TestPlugin : Plugin
    {{
        public TestPlugin()
        {{
            RegisterStep<{entityClass}, ITestService>(EventOperation.Update, ExecutionStage.PostOperation)
                .AddFilteredAttributes(x => x.{(entityClass == "Account" ? "Name" : "FirstName")})
                .WithPostImage(x => x.{(entityClass == "Account" ? "Name" : "FirstName")}, x => x.{(entityClass == "Account" ? "AccountNumber" : "LastName")})
                .Execute<PostImage>((service, postImage) => service.Process(postImage));
        }}

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {{
            return services.AddScoped<ITestService, TestService>();
        }}
    }}

    public interface ITestService
    {{
        void Process(object image);
    }}

    public class TestService : ITestService
    {{
        public void Process(object image) {{ }}
    }}
}}";

    /// <summary>
    /// Plugin with both PreImage and PostImage using WithPreImage and WithPostImage.
    /// </summary>
    public static string GetPluginWithBothImages(string entityClass = "Account") => $@"
using XrmPluginCore;
using XrmPluginCore.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using TestNamespace;

namespace TestNamespace
{{
    public class TestPlugin : Plugin
    {{
        public TestPlugin()
        {{
            RegisterStep<{entityClass}, ITestService>(EventOperation.Update, ExecutionStage.PostOperation)
                .AddFilteredAttributes(x => x.Name)
                .WithPreImage(x => x.Name, x => x.{(entityClass == "Account" ? "Revenue" : "EmailAddress")})
                .WithPostImage(x => x.Name, x => x.{(entityClass == "Account" ? "AccountNumber" : "LastName")})
                .Execute<PreImage, PostImage>((service, preImage, postImage) => service.Process(preImage, postImage));
        }}

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {{
            return services.AddScoped<ITestService, TestService>();
        }}
    }}

    public interface ITestService
    {{
        void Process(object preImage, object postImage);
    }}

    public class TestService : ITestService
    {{
        public void Process(object preImage, object postImage) {{ }}
    }}
}}";

    /// <summary>
    /// Plugin using old AddImage API for backward compatibility testing.
    /// </summary>
    public static string GetPluginWithOldImageApi(string entityClass = "Account") => $@"
using XrmPluginCore;
using XrmPluginCore.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using TestNamespace;

namespace TestNamespace
{{
    public class TestPlugin : Plugin
    {{
        public TestPlugin()
        {{
            RegisterStep<{entityClass}, ITestService>(EventOperation.Update, ExecutionStage.PostOperation, service => service.Process())
                .AddFilteredAttributes(x => x.Name)
                .AddImage(ImageType.PreImage, x => x.Name, x => x.{(entityClass == "Account" ? "Revenue" : "EmailAddress")});
        }}

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {{
            return services.AddScoped<ITestService, TestService>();
        }}
    }}

    public interface ITestService
    {{
        void Process();
    }}

    public class TestService : ITestService
    {{
        public void Process() {{ }}
    }}
}}";

    /// <summary>
    /// Gets a complete compilable source with entity and plugin.
    /// </summary>
    public static string GetCompleteSource(string entitySource, string pluginSource)
    {
        // Determine which entity is being used by checking if pluginSource contains RegisterStep<Account or RegisterStep<Contact
        var usesAccount = pluginSource.Contains("RegisterStep<Account");
        var usesContact = pluginSource.Contains("RegisterStep<Contact");

        // Build using statements conditionally
        var usingStatements = new System.Text.StringBuilder();
        usingStatements.AppendLine("using System;");
        usingStatements.AppendLine("using System.ComponentModel;");
        usingStatements.AppendLine("using Microsoft.Xrm.Sdk;");
        usingStatements.AppendLine("using XrmPluginCore;");
        usingStatements.AppendLine("using XrmPluginCore.Enums;");
        usingStatements.AppendLine("using Microsoft.Extensions.DependencyInjection;");

        if (usesAccount)
        {
            usingStatements.AppendLine("using TestNamespace.PluginImages.TestPlugin.AccountUpdatePostOperation;");
        }
        if (usesContact)
        {
            usingStatements.AppendLine("using TestNamespace.PluginImages.TestPlugin.ContactUpdatePostOperation;");
        }

        // Properly combine sources by merging namespaces
        return $@"{usingStatements}
namespace TestNamespace
{{
    [Microsoft.Xrm.Sdk.Client.EntityLogicalName(""account"")]
    public class Account : Entity
    {{
        public const string EntityLogicalName = ""account"";

        public Account() : base(EntityLogicalName) {{ }}

        [AttributeLogicalName(""name"")]
        public string Name
        {{
            get => GetAttributeValue<string>(""name"");
            set => SetAttributeValue(""name"", value);
        }}

        [AttributeLogicalName(""accountnumber"")]
        public string AccountNumber
        {{
            get => GetAttributeValue<string>(""accountnumber"");
            set => SetAttributeValue(""accountnumber"", value);
        }}

        [AttributeLogicalName(""revenue"")]
        public Money Revenue
        {{
            get => GetAttributeValue<Money>(""revenue"");
            set => SetAttributeValue(""revenue"", value);
        }}

        [AttributeLogicalName(""industrycode"")]
        public OptionSetValue IndustryCode
        {{
            get => GetAttributeValue<OptionSetValue>(""industrycode"");
            set => SetAttributeValue(""industrycode"", value);
        }}

        [AttributeLogicalName(""primarycontactid"")]
        public EntityReference PrimaryContactId
        {{
            get => GetAttributeValue<EntityReference>(""primarycontactid"");
            set => SetAttributeValue(""primarycontactid"", value);
        }}
    }}

    [Microsoft.Xrm.Sdk.Client.EntityLogicalName(""contact"")]
    public class Contact : Entity
    {{
        public const string EntityLogicalName = ""contact"";

        public Contact() : base(EntityLogicalName) {{ }}

        [AttributeLogicalName(""firstname"")]
        public string FirstName
        {{
            get => GetAttributeValue<string>(""firstname"");
            set => SetAttributeValue(""firstname"", value);
        }}

        [AttributeLogicalName(""lastname"")]
        public string LastName
        {{
            get => GetAttributeValue<string>(""lastname"");
            set => SetAttributeValue(""lastname"", value);
        }}

        [AttributeLogicalName(""emailaddress1"")]
        public string EmailAddress
        {{
            get => GetAttributeValue<string>(""emailaddress1"");
            set => SetAttributeValue(""emailaddress1"", value);
        }}
    }}

    {StripNamespaceAndUsings(pluginSource)}
}}";
    }

    /// <summary>
    /// Removes namespace declaration and using statements from source code.
    /// </summary>
    private static string StripNamespaceAndUsings(string source)
    {
        var lines = source.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var result = new System.Text.StringBuilder();
        bool inNamespace = false;
        int braceCount = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip using statements
            if (trimmed.StartsWith("using "))
                continue;

            // Skip namespace declaration
            if (trimmed.StartsWith("namespace "))
            {
                inNamespace = true;
                continue;
            }

            // Skip opening brace of namespace
            if (inNamespace && trimmed == "{")
            {
                inNamespace = false;
                braceCount++;
                continue;
            }

            // Track braces
            braceCount += line.Count(c => c == '{');
            braceCount -= line.Count(c => c == '}');

            // Skip closing brace if it would close the namespace
            if (braceCount == 0 && trimmed == "}")
                continue;

            result.AppendLine(line);
        }

        return result.ToString();
    }
}
