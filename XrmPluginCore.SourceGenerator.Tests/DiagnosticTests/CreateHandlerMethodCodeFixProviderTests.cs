using FluentAssertions;
using XrmPluginCore.SourceGenerator.Analyzers;
using XrmPluginCore.SourceGenerator.CodeFixes;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
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

		// Assert - Method created with alias-qualified PreImage parameter
		fixedSource.Should().Contain("void HandleUpdate(AccountUpdatePostOperation.PreImage preImage)");

		// Assert - Aliased using directive added
		fixedSource.Should().Contain("using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;");
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

		// Assert - Method created with both alias-qualified image parameters
		fixedSource.Should().Contain("void HandleUpdate(AccountUpdatePostOperation.PreImage preImage, AccountUpdatePostOperation.PostImage postImage)");

		// Assert - Aliased using directive added
		fixedSource.Should().Contain("using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;");
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
	public async Task Should_Use_Aliased_Usings_For_Multiple_Registrations()
	{
		// Arrange - A plain using already exists for the Update registration; the Delete handler is
		// missing from the interface. Creating Delete must requalify the existing Update reference to
		// its alias (resolved via the semantic model) and add the Delete using in aliased form.
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

			namespace TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation
			{
			    public sealed class PreImage { }
			    public sealed class PostImage { }
			}

			namespace TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePostOperation
			{
			    public sealed class PreImage { }
			    public sealed class PostImage { }
			}
			""";

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert - New method created with its own alias-qualified type
		fixedSource.Should().Contain("void HandleDelete(AccountDeletePostOperation.PreImage preImage)");

		// Assert - Existing bare PreImage references requalified with the Update alias
		CountOccurrences(fixedSource, "AccountUpdatePostOperation.PreImage preImage").Should().BeGreaterOrEqualTo(1);

		// Assert - Each namespace aliased exactly once
		CountOccurrences(fixedSource, "using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;")
			.Should().Be(1, "the existing plain using should be converted to aliased form");
		CountOccurrences(fixedSource, "using AccountDeletePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePostOperation;")
			.Should().Be(1, "the new using should be added in aliased form");

		AssertNoAmbiguousReferences(fixedSource);
	}

	[Fact]
	public async Task FixAll_Should_Create_Multiple_Methods_On_Same_Interface()
	{
		// Arrange - Two same-service registrations, both missing their handler methods.
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
			    }

			    public class TestService : ITestService
			    {
			    }
			}

			namespace TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation
			{
			    public sealed class PreImage { }
			    public sealed class PostImage { }
			}

			namespace TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePostOperation
			{
			    public sealed class PreImage { }
			    public sealed class PostImage { }
			}
			""";

		// Act - Apply the consolidating FixAll across all diagnostics in one pass
		var fixedSource = await ApplyFixAllAsync(source);

		// Assert - Both methods created with their own alias-qualified parameter
		fixedSource.Should().Contain("void HandleUpdate(AccountUpdatePostOperation.PreImage preImage)");
		fixedSource.Should().Contain("void HandleDelete(AccountDeletePostOperation.PreImage preImage)");

		// Assert - One aliased using per distinct namespace (no duplicates)
		CountOccurrences(fixedSource, "using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;").Should().Be(1);
		CountOccurrences(fixedSource, "using AccountDeletePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePostOperation;").Should().Be(1);

		AssertNoAmbiguousReferences(fixedSource);
	}

	private static void AssertNoAmbiguousReferences(string source)
	{
		var compilation = CompilationHelper.CreateCompilation(source);
		var ambiguous = compilation.GetDiagnostics()
			.Where(d => d.Id == "CS0104")
			.Select(d => d.GetMessage())
			.ToList();
		ambiguous.Should().BeEmpty("the fixed source should not contain ambiguous references");
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

	private static Task<string> ApplyFixAllAsync(string source)
		=> ApplyFixAllAsync(source, new HandlerMethodNotFoundAnalyzer(), new CreateHandlerMethodCodeFixProvider(),
			nameof(CreateHandlerMethodCodeFixProvider),
			DiagnosticDescriptors.HandlerMethodNotFound.Id);
}
