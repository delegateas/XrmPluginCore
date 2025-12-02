using XrmPluginCore.Tests.Context.BusinessDomain;

namespace XrmPluginCore.SourceGenerator.Tests.Helpers;

/// <summary>
/// Provides reusable test fixtures for source generator testing.
/// </summary>
public static class TestFixtures
{
	/// <summary>
	/// Plugin with PreImage only.
	/// </summary>
	public static string GetPluginWithPreImage(string entityClass = "Account")
	{
		List<string> imageProperties = [];
		if (entityClass == "Account")
		{
			imageProperties.AddRange([
				nameof(Account.Name),
				nameof(Account.Revenue),
				nameof(Account.IndustryCode)
			]);
		}
		else
		{
			imageProperties.AddRange([
				nameof(Contact.FirstName),
				nameof(Contact.EMailAddress1),
				nameof(Contact.AccountId)
			]);
		}

		var preImageProps = $"{string.Join(", ", imageProperties.Select(i => $"x => x.{i}"))}";
		var filtered = entityClass == "Account" ? nameof(Account.Name) : nameof(Contact.FirstName);

		return $@"
using XrmPluginCore;
using XrmPluginCore.Abstractions;
using XrmPluginCore.Enums;
using Microsoft.Extensions.DependencyInjection;
using TestNamespace;
using TestNamespace.PluginRegistrations.TestPlugin.{entityClass}UpdatePostOperation;

namespace TestNamespace
{{
    public class TestPlugin : Plugin
    {{
        public TestPlugin()
        {{
            RegisterStep<{entityClass}, ITestService>(EventOperation.Update, ExecutionStage.PostOperation,
                nameof(ITestService.HandleAccountUpdate))
                .AddFilteredAttributes(x => x.{filtered})
                .WithPreImage({preImageProps});
        }}

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
        {{
            return services.AddScoped<ITestService, TestService>();
        }}
    }}

    public interface ITestService
    {{
        void HandleAccountUpdate(PreImage preImage);
    }}

    public class TestService : ITestService
    {{
        public void HandleAccountUpdate(PreImage preImage) {{ }}
    }}
}}";
	}

	/// <summary>
	/// Plugin with PostImage.
	/// </summary>
	public const string PluginWithPostImage = """

		using XrmPluginCore;
		using XrmPluginCore.Abstractions;
		using XrmPluginCore.Enums;
		using Microsoft.Extensions.DependencyInjection;
		using TestNamespace;
		using TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;

		namespace TestNamespace
		{
		    public class TestPlugin : Plugin
		    {
		        public TestPlugin()
		        {
		            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation,
		                nameof(ITestService.HandleAccountUpdate))
		                .AddFilteredAttributes(x => x.Name)
		                .WithPostImage(x => x.Name, x => x.AccountNumber);
		        }

		        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
		        {
		            return services.AddScoped<ITestService, TestService>();
		        }
		    }

		    public interface ITestService
		    {
		        void HandleAccountUpdate(PostImage postImage);
		    }

		    public class TestService : ITestService
		    {
		        public void HandleAccountUpdate(PostImage postImage) { }
		    }
		}
		""";

	/// <summary>
	/// Plugin with both PreImage and PostImage using WithPreImage and WithPostImage.
	/// </summary>
	public const string PluginWithBothImages = """

		using XrmPluginCore;
		using XrmPluginCore.Abstractions;
		using XrmPluginCore.Enums;
		using Microsoft.Extensions.DependencyInjection;
		using TestNamespace;
		using TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;

		namespace TestNamespace
		{
		    public class TestPlugin : Plugin
		    {
		        public TestPlugin()
		        {
		            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation,
		                nameof(ITestService.HandleAccountUpdate))
		                .AddFilteredAttributes(x => x.Name)
		                .WithPreImage(x => x.Name, x => x.Revenue)
		                .WithPostImage(x => x.Name, x => x.AccountNumber);
		        }

		        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
		        {
		            return services.AddScoped<ITestService, TestService>();
		        }
		    }

		    public interface ITestService
		    {
		        void HandleAccountUpdate(PreImage preImage, PostImage postImage);
		    }

		    public class TestService : ITestService
		    {
		        public void HandleAccountUpdate(PreImage preImage, PostImage postImage) { }
		    }
		}
		""";

	/// <summary>
	/// Plugin with handler method reference but without any images.
	/// Tests that ActionWrapper is generated even when no images are registered.
	/// </summary>
	public static string GetPluginWithoutImages(string action = "nameof(ITestService.HandleUpdate)") =>
		$$"""
				using XrmPluginCore;
				using XrmPluginCore.Abstractions;
				using XrmPluginCore.Enums;
				using Microsoft.Extensions.DependencyInjection;
				using TestNamespace;
				using TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;

				namespace TestNamespace
				{
				    public class TestPlugin : Plugin
				    {
				        public TestPlugin()
				        {
				            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation,
				                {{action}})
				                .AddFilteredAttributes(x => x.Name);
				        }

				        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
				        {
				            return services.AddScoped<ITestService, TestService>();
				        }
				    }

				    public interface ITestService
				    {
				        void HandleUpdate();
				    }

				    public class TestService : ITestService
				    {
				        public void HandleUpdate() { }
				    }
				}
		""";

	/// <summary>
	/// Plugin using old AddImage API for backward compatibility testing.
	/// </summary>
	public const string PluginWithLegacyAddImage =
		"""
		using XrmPluginCore;
		using XrmPluginCore.Abstractions;
		using Microsoft.Extensions.DependencyInjection;
		using TestNamespace;

		namespace TestNamespace
		{
		    public class TestPlugin : Plugin
		    {
		        public TestPlugin()
		        {
		            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation, service => service.HandleAccountUpdate())
		                .AddFilteredAttributes(x => x.Name)
		                .AddImage(ImageType.PreImage, x => x.Name, x => x.Revenue);
		        }

		        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
		        {
		            return services.AddScoped<ITestService, TestService>();
		        }
		    }

		    public interface ITestService
		    {
		        void HandleAccountUpdate();
		    }

		    public class TestService : ITestService
		    {
		        public void HandleAccountUpdate() { }
		    }
		}
		""";

	/// <summary>
	/// Gets a complete compilable source with entity and plugin.
	/// </summary>
	public static string GetCompleteSource(string pluginSource)
	{
		var entityName = pluginSource.Contains("RegisterStep<Account") ? "Account" : "Contact";
		var operation = pluginSource.Contains("EventOperation.Delete") ? "Delete" : "Update";

		return $$"""
			using System;
			using System.ComponentModel;
			using Microsoft.Xrm.Sdk;
			using XrmPluginCore;
			using XrmPluginCore.Enums;
			using Microsoft.Extensions.DependencyInjection;
			using XrmPluginCore.Tests.Context.BusinessDomain;
			using TestNamespace.PluginRegistrations.TestPlugin.{{entityName}}{{operation}}PostOperation;

			namespace TestNamespace
			{
				{{StripNamespaceAndUsings(pluginSource)}}
			}
			""";
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
