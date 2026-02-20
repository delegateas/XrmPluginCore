using FluentAssertions;
using XrmPluginCore.SourceGenerator.Analyzers;
using XrmPluginCore.SourceGenerator.CodeFixes;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.DiagnosticTests;

/// <summary>
/// Tests for CreateHandlerMethodCodeFixProvider that creates missing handler methods
/// and adds using directives for generated image namespaces.
/// </summary>
public class CreateHandlerMethodCodeFixProviderTests : CodeFixTestBase
{
	[Fact]
	public async Task Should_Create_Method_And_Add_Using_For_PreImage()
	{
		// Arrange - Method doesn't exist on interface, WithPreImage registered
		const string source = """
			using System;
			using System.ComponentModel;
			using Microsoft.Xrm.Sdk;
			using XrmPluginCore;
			using XrmPluginCore.Enums;
			using Microsoft.Extensions.DependencyInjection;
			using XrmPluginCore.Tests.Context.BusinessDomain;

			namespace TestNamespace
			{
			    public class TestPlugin : Plugin
			    {
			        public TestPlugin()
			        {
			            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation,
			                "HandleUpdate")
			                .AddFilteredAttributes(x => x.Name)
			                .WithPreImage(x => x.Name, x => x.Revenue);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			        {
			            return services.AddScoped<ITestService, TestService>();
			        }
			    }

			    public interface ITestService
			    {
			    }

			    public class TestService : ITestService
			    {
			    }
			}
			""";

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert - Method created with PreImage parameter
		fixedSource.Should().Contain("void HandleUpdate(PreImage preImage)");

		// Assert - Using directive added
		fixedSource.Should().Contain("using TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;");
	}

	[Fact]
	public async Task Should_Create_Method_And_Add_Using_For_Both_Images()
	{
		// Arrange - Method doesn't exist on interface, both images registered
		const string source = """
			using System;
			using System.ComponentModel;
			using Microsoft.Xrm.Sdk;
			using XrmPluginCore;
			using XrmPluginCore.Enums;
			using Microsoft.Extensions.DependencyInjection;
			using XrmPluginCore.Tests.Context.BusinessDomain;

			namespace TestNamespace
			{
			    public class TestPlugin : Plugin
			    {
			        public TestPlugin()
			        {
			            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation,
			                "HandleUpdate")
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
			    }

			    public class TestService : ITestService
			    {
			    }
			}
			""";

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert - Method created with both image parameters
		fixedSource.Should().Contain("void HandleUpdate(PreImage preImage, PostImage postImage)");

		// Assert - Using directive added
		fixedSource.Should().Contain("using TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;");
	}

	[Fact]
	public async Task Should_Create_Method_Without_Using_When_No_Images()
	{
		// Arrange - Method doesn't exist on interface, no images registered
		const string source = """
			using System;
			using System.ComponentModel;
			using Microsoft.Xrm.Sdk;
			using XrmPluginCore;
			using XrmPluginCore.Enums;
			using Microsoft.Extensions.DependencyInjection;
			using XrmPluginCore.Tests.Context.BusinessDomain;

			namespace TestNamespace
			{
			    public class TestPlugin : Plugin
			    {
			        public TestPlugin()
			        {
			            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation,
			                "HandleUpdate")
			                .AddFilteredAttributes(x => x.Name);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			        {
			            return services.AddScoped<ITestService, TestService>();
			        }
			    }

			    public interface ITestService
			    {
			    }

			    public class TestService : ITestService
			    {
			    }
			}
			""";

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert - Method created without image parameters
		fixedSource.Should().Contain("void HandleUpdate()");

		// Assert - No image namespace using directive added
		fixedSource.Should().NotContain("using TestNamespace.PluginRegistrations");
	}

	[Fact]
	public async Task Should_Avoid_Ambiguous_Usings()
	{
		// Arrange - Using directive already exists for Update registration, method missing for Delete
		const string source = """
			using System;
			using System.ComponentModel;
			using Microsoft.Xrm.Sdk;
			using XrmPluginCore;
			using XrmPluginCore.Enums;
			using Microsoft.Extensions.DependencyInjection;
			using XrmPluginCore.Tests.Context.BusinessDomain;
			using TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;

			namespace TestNamespace
			{
			    public class TestPlugin : Plugin
			    {
			        public TestPlugin()
			        {
			            RegisterStep<Account, ITestService>(EventOperation.Update, ExecutionStage.PostOperation,
			                nameof(ITestService.HandleUpdate))
			                .AddFilteredAttributes(x => x.Name)
			                .WithPreImage(x => x.Name, x => x.Revenue);

						RegisterStep<Account, ITestService>(EventOperation.Delete, ExecutionStage.PostOperation,
							"HandleDelete")
							.AddFilteredAttributes(x => x.Name)
							.WithPreImage(x => x.Name, x => x.Revenue);
			        }

			        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services)
			        {
			            return services.AddScoped<ITestService, TestService>();
			        }
			    }

			    public interface ITestService
			    {
			        void HandleUpdate(PreImage preImage);
			    }

			    public class TestService : ITestService
			    {
			        public void HandleUpdate(PreImage preImage) { }
			    }
			}
			""";

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert - New method created with qualified type
		fixedSource.Should().Contain("void HandleDelete(AccountDeletePostOperation.PreImage preImage)");

		// Assert - Existing PreImage references qualified with alias
		CountOccurrences(fixedSource, "AccountUpdatePostOperation.PreImage preImage").Should().BeGreaterOrEqualTo(1);

		// Assert - Aliased usings
		CountOccurrences(fixedSource, "using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;")
			.Should().Be(1, "the existing using should be converted to aliased form");
		CountOccurrences(fixedSource, "using AccountDeletePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePostOperation;")
			.Should().Be(1, "the new using should be added in aliased form");
	}

	private static int CountOccurrences(string source, string search)
	{
		var count = 0;
		var index = 0;
		while ((index = source.IndexOf(search, index, StringComparison.Ordinal)) != -1)
		{
			count++;
			index += search.Length;
		}
		return count;
	}

	private static Task<string> ApplyCodeFixAsync(string source)
		=> ApplyCodeFixAsync(source, new HandlerMethodNotFoundAnalyzer(), new CreateHandlerMethodCodeFixProvider(),
			DiagnosticDescriptors.HandlerMethodNotFound.Id);
}
