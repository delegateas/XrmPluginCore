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

	private static Task<string> ApplyCodeFixAsync(string source)
		=> ApplyCodeFixAsync(source, new HandlerMethodNotFoundAnalyzer(), new CreateHandlerMethodCodeFixProvider(),
			DiagnosticDescriptors.HandlerMethodNotFound.Id);
}
