using FluentAssertions;
using XrmPluginCore.SourceGenerator.Analyzers;
using XrmPluginCore.SourceGenerator.CodeFixes;
using Xunit;

namespace XrmPluginCore.SourceGenerator.Tests.DiagnosticTests;

/// <summary>
/// Tests for FixHandlerSignatureCodeFixProvider that fixes handler method signatures
/// and adds using directives for generated image namespaces.
/// </summary>
public class FixHandlerSignatureCodeFixProviderTests : CodeFixTestBase
{
	[Fact]
	public async Task Should_Fix_Signature_And_Add_Using_For_PreImage()
	{
		// Arrange - Handler has no params, but WithPreImage is registered
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
			                nameof(ITestService.HandleUpdate))
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
			        void HandleUpdate();
			    }

			    public class TestService : ITestService
			    {
			        public void HandleUpdate() { }
			    }
			}
			""";

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert - Signature fixed
		fixedSource.Should().Contain("void HandleUpdate(PreImage preImage)");

		// Assert - Using directive added
		fixedSource.Should().Contain("using TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;");
	}

	[Fact]
	public async Task Should_Fix_Signature_And_Add_Using_For_PostImage()
	{
		// Arrange - Handler has no params, but WithPostImage is registered
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
			                nameof(ITestService.HandleUpdate))
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
			        void HandleUpdate();
			    }

			    public class TestService : ITestService
			    {
			        public void HandleUpdate() { }
			    }
			}
			""";

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert - Signature fixed
		fixedSource.Should().Contain("void HandleUpdate(PostImage postImage)");

		// Assert - Using directive added
		fixedSource.Should().Contain("using TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;");
	}

	[Fact]
	public async Task Should_Fix_Signature_And_Add_Using_For_Both_Images()
	{
		// Arrange - Handler has no params, but both images are registered
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
			                nameof(ITestService.HandleUpdate))
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
			        void HandleUpdate();
			    }

			    public class TestService : ITestService
			    {
			        public void HandleUpdate() { }
			    }
			}
			""";

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert - Signature fixed with both image parameters
		fixedSource.Should().Contain("void HandleUpdate(PreImage preImage, PostImage postImage)");

		// Assert - Using directive added
		fixedSource.Should().Contain("using TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;");
	}

	[Fact]
	public async Task Should_Not_Duplicate_Using_When_Already_Present()
	{
		// Arrange - Using directive already exists, handler has wrong signature
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

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert - Signature fixed
		fixedSource.Should().Contain("void HandleUpdate(PreImage preImage)");

		// Assert - Using directive not duplicated (count occurrences)
		var usingDirective = "using TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;";
		var count = CountOccurrences(fixedSource, usingDirective);
		count.Should().Be(1, "the using directive should not be duplicated");
	}

	[Fact]
	public async Task Should_Avoid_Ambiguous_Usings()
	{
		// Arrange - Using directive already exists for Update registration
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
							nameof(ITestService.HandleDelete))
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
					void HandleDelete();
			    }

			    public class TestService : ITestService
			    {
			        public void HandleUpdate(PreImage preImage) { }
					public void HandleDelete() { }
			    }
			}
			""";

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert - Signature fixed
		CountOccurrences(fixedSource, "void HandleUpdate(AccountUpdatePostOperation.PreImage preImage)").Should().Be(2, "both the interface and implementation are updated");
		CountOccurrences(fixedSource, "void HandleDelete(AccountDeletePostOperation.PreImage preImage)").Should().Be(2, "both the interface and implementation are updated");

		// Assert - Using directive not duplicated (count occurrences)
		CountOccurrences(fixedSource, "using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;")
			.Should().Be(1, "the using directive should be de-ambiguified");
		CountOccurrences(fixedSource, "using AccountDeletePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePostOperation;")
			.Should().Be(1, "the using directive should be de-ambiguified");
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
		=> ApplyCodeFixAsync(source, new HandlerSignatureMismatchAnalyzer(), new FixHandlerSignatureCodeFixProvider(),
			DiagnosticDescriptors.HandlerSignatureMismatch.Id, DiagnosticDescriptors.HandlerSignatureMismatchError.Id);
}
