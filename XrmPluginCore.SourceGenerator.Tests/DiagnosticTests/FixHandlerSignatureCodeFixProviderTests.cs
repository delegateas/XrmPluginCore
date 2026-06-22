using FluentAssertions;
using XrmPluginCore.SourceGenerator.Analyzers;
using XrmPluginCore.SourceGenerator.CodeFixes;
using XrmPluginCore.SourceGenerator.Tests.Helpers;
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

		// Assert - Signature fixed (alias-qualified)
		fixedSource.Should().Contain("void HandleUpdate(AccountUpdatePostOperation.PreImage preImage)");

		// Assert - Aliased using directive added
		fixedSource.Should().Contain("using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;");
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

		// Assert - Signature fixed (alias-qualified)
		fixedSource.Should().Contain("void HandleUpdate(AccountUpdatePostOperation.PostImage postImage)");

		// Assert - Aliased using directive added
		fixedSource.Should().Contain("using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;");
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

		// Assert - Signature fixed with both image parameters (alias-qualified)
		fixedSource.Should().Contain("void HandleUpdate(AccountUpdatePostOperation.PreImage preImage, AccountUpdatePostOperation.PostImage postImage)");

		// Assert - Aliased using directive added
		fixedSource.Should().Contain("using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;");
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

		// Assert - Signature fixed (alias-qualified)
		fixedSource.Should().Contain("void HandleUpdate(AccountUpdatePostOperation.PreImage preImage)");

		// Assert - The pre-existing plain using is converted to the aliased form (not duplicated)
		CountOccurrences(fixedSource, "using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;")
			.Should().Be(1, "the using directive should be converted to aliased form exactly once");
		CountOccurrences(fixedSource, "using TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;")
			.Should().Be(0, "the plain using should no longer be present");
	}

	[Fact]
	public async Task Should_Use_Aliased_Usings_For_Multiple_Registrations()
	{
		// Arrange - A plain using already exists for the Update registration; the Delete handler is
		// missing its image parameter. Fixing Delete must requalify the existing Update reference to
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

		// Assert - Both signatures alias-qualified (interface + implementation)
		CountOccurrences(fixedSource, "void HandleUpdate(AccountUpdatePostOperation.PreImage preImage)").Should().Be(2, "both the interface and implementation are updated");
		CountOccurrences(fixedSource, "void HandleDelete(AccountDeletePostOperation.PreImage preImage)").Should().Be(2, "both the interface and implementation are updated");

		// Assert - Each namespace aliased exactly once
		CountOccurrences(fixedSource, "using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;")
			.Should().Be(1, "the existing plain using should be converted to aliased form exactly once");
		CountOccurrences(fixedSource, "using AccountDeletePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePostOperation;")
			.Should().Be(1, "the new using should be added in aliased form exactly once");

		// Assert - Result compiles without ambiguous-reference (CS0104) errors
		AssertNoAmbiguousReferences(fixedSource);
	}

	[Fact]
	public async Task Should_Not_Duplicate_Already_Aliased_Using()
	{
		// Arrange - The aliased using is already present, but the handler still needs its parameter.
		const string source = """
			using System;
			using System.ComponentModel;
			using Microsoft.Xrm.Sdk;
			using XrmPluginCore;
			using XrmPluginCore.Enums;
			using Microsoft.Extensions.DependencyInjection;
			using XrmPluginCore.Tests.Context.BusinessDomain;
			using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;

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

			namespace TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation
			{
			    public sealed class PreImage { }
			    public sealed class PostImage { }
			}
			""";

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert - Signature fixed with the alias-qualified parameter
		CountOccurrences(fixedSource, "void HandleUpdate(AccountUpdatePostOperation.PreImage preImage)").Should().Be(2);

		// Assert - The already-present aliased using is not duplicated
		CountOccurrences(fixedSource, "using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;")
			.Should().Be(1, "the existing aliased using should not be duplicated");

		AssertNoAmbiguousReferences(fixedSource);
	}

	[Fact]
	public async Task Should_Realias_CleanedUp_Plain_Using()
	{
		// Arrange - The Update registration is fully aliased and correct (no diagnostic). A developer
		// has "cleaned up" the Delete import back to a plain using, and the Delete handler is missing
		// its parameter. After the fix, every image using must be aliased exactly once.
		const string source = """
			using System;
			using System.ComponentModel;
			using Microsoft.Xrm.Sdk;
			using XrmPluginCore;
			using XrmPluginCore.Enums;
			using Microsoft.Extensions.DependencyInjection;
			using XrmPluginCore.Tests.Context.BusinessDomain;
			using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;
			using TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePostOperation;

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
			        void HandleUpdate(AccountUpdatePostOperation.PreImage preImage);
					void HandleDelete();
			    }

			    public class TestService : ITestService
			    {
			        public void HandleUpdate(AccountUpdatePostOperation.PreImage preImage) { }
					public void HandleDelete() { }
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

		// Assert - The stale plain Delete using is converted to aliased form
		CountOccurrences(fixedSource, "using TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePostOperation;")
			.Should().Be(0, "the plain using should be re-aliased");

		// Assert - Every image namespace is aliased exactly once
		CountOccurrences(fixedSource, "using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;")
			.Should().Be(1);
		CountOccurrences(fixedSource, "using AccountDeletePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePostOperation;")
			.Should().Be(1);

		// Assert - Delete signature fixed, Update signature preserved
		CountOccurrences(fixedSource, "void HandleDelete(AccountDeletePostOperation.PreImage preImage)").Should().Be(2);
		CountOccurrences(fixedSource, "void HandleUpdate(AccountUpdatePostOperation.PreImage preImage)").Should().Be(2);

		AssertNoAmbiguousReferences(fixedSource);
	}

	[Fact]
	public async Task Should_Requalify_Each_Reference_Under_Multiple_Plain_Usings()
	{
		// Arrange - Two plain image usings are present at once. The Update handler references a bare
		// PreImage and the Delete handler a bare PostImage (each generated namespace only exposes the
		// image type its registration declared, so each bare reference still binds uniquely). The
		// Create handler is missing its parameter and triggers the fix. The fix must requalify each
		// pre-existing reference to its OWN alias via the semantic model - the old break-on-first
		// logic would have mis-qualified one of them.
		const string source = """
			using System;
			using System.ComponentModel;
			using Microsoft.Xrm.Sdk;
			using XrmPluginCore;
			using XrmPluginCore.Enums;
			using Microsoft.Extensions.DependencyInjection;
			using XrmPluginCore.Tests.Context.BusinessDomain;
			using TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;
			using TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePostOperation;

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
							.WithPostImage(x => x.Name, x => x.AccountNumber);

						RegisterStep<Account, ITestService>(EventOperation.Create, ExecutionStage.PostOperation,
							nameof(ITestService.HandleCreate))
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
					void HandleDelete(PostImage postImage);
					void HandleCreate();
			    }

			    public class TestService : ITestService
			    {
			        public void HandleUpdate(PreImage preImage) { }
					public void HandleDelete(PostImage postImage) { }
					public void HandleCreate() { }
			    }
			}

			namespace TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation
			{
			    public sealed class PreImage { }
			}

			namespace TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePostOperation
			{
			    public sealed class PostImage { }
			}

			namespace TestNamespace.PluginRegistrations.TestPlugin.AccountCreatePostOperation
			{
			    public sealed class PreImage { }
			}
			""";

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert - Each pre-existing reference requalified to its OWN alias (never cross-qualified)
		CountOccurrences(fixedSource, "void HandleUpdate(AccountUpdatePostOperation.PreImage preImage)").Should().Be(2);
		CountOccurrences(fixedSource, "void HandleDelete(AccountDeletePostOperation.PostImage postImage)").Should().Be(2);

		// Assert - The triggering handler created with its own alias
		CountOccurrences(fixedSource, "void HandleCreate(AccountCreatePostOperation.PreImage preImage)").Should().Be(2);

		// Assert - Each namespace aliased exactly once
		CountOccurrences(fixedSource, "using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;").Should().Be(1);
		CountOccurrences(fixedSource, "using AccountDeletePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePostOperation;").Should().Be(1);
		CountOccurrences(fixedSource, "using AccountCreatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountCreatePostOperation;").Should().Be(1);

		AssertNoAmbiguousReferences(fixedSource);
	}

	[Fact]
	public async Task FixAll_Should_Consolidate_Multiple_Registrations()
	{
		// Arrange - Two same-service registrations both trigger the diagnostic.
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
			        void HandleUpdate();
					void HandleDelete();
			    }

			    public class TestService : ITestService
			    {
			        public void HandleUpdate() { }
					public void HandleDelete() { }
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

		// Assert - Every signature alias-qualified (interface + implementation)
		CountOccurrences(fixedSource, "void HandleUpdate(AccountUpdatePostOperation.PreImage preImage)").Should().Be(2);
		CountOccurrences(fixedSource, "void HandleDelete(AccountDeletePostOperation.PreImage preImage)").Should().Be(2);

		// Assert - One aliased using per distinct namespace (no duplicates)
		CountOccurrences(fixedSource, "using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;").Should().Be(1);
		CountOccurrences(fixedSource, "using AccountDeletePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountDeletePostOperation;").Should().Be(1);

		AssertNoAmbiguousReferences(fixedSource);
	}

	[Fact]
	public async Task Should_Add_Standard_Alias_When_Namespace_Imported_Under_Different_Alias()
	{
		// Arrange - The image namespace is already imported, but under a NON-standard alias ("Foo").
		// The emitted parameter type uses the standard alias (the last namespace segment), so the fix
		// must still add the standard aliased using - otherwise the qualified type fails to resolve.
		const string source = """
			using System;
			using System.ComponentModel;
			using Microsoft.Xrm.Sdk;
			using XrmPluginCore;
			using XrmPluginCore.Enums;
			using Microsoft.Extensions.DependencyInjection;
			using XrmPluginCore.Tests.Context.BusinessDomain;
			using Foo = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;

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

			namespace TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation
			{
			    public sealed class PreImage { }
			    public sealed class PostImage { }
			}
			""";

		// Act
		var fixedSource = await ApplyCodeFixAsync(source);

		// Assert - Signature uses the standard alias
		CountOccurrences(fixedSource, "void HandleUpdate(AccountUpdatePostOperation.PreImage preImage)").Should().Be(2);

		// Assert - The standard aliased using is added even though "Foo" already imports the namespace
		CountOccurrences(fixedSource, "using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;")
			.Should().Be(1, "the standard alias must be present so the qualified parameter type resolves");

		// Assert - The pre-existing non-standard alias is left untouched
		CountOccurrences(fixedSource, "using Foo = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;")
			.Should().Be(1, "the existing alias should not be removed");

		// Assert - Compiles cleanly: no ambiguity AND no unresolved standard alias
		AssertCompilesWithoutReferenceErrors(fixedSource);
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

	private static void AssertCompilesWithoutReferenceErrors(string source)
	{
		var compilation = CompilationHelper.CreateCompilation(source);

		// CS0104 = ambiguous reference; CS0246/CS0234 = unresolved type / namespace member, which is
		// how a missing alias (qualified type whose alias was never added) surfaces.
		var errors = compilation.GetDiagnostics()
			.Where(d => d.Id is "CS0104" or "CS0246" or "CS0234")
			.Select(d => d.GetMessage())
			.ToList();
		errors.Should().BeEmpty("the fixed source should resolve all image references");
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

	private static Task<string> ApplyFixAllAsync(string source)
		=> ApplyFixAllAsync(source, new HandlerSignatureMismatchAnalyzer(), new FixHandlerSignatureCodeFixProvider(),
			nameof(FixHandlerSignatureCodeFixProvider),
			DiagnosticDescriptors.HandlerSignatureMismatch.Id, DiagnosticDescriptors.HandlerSignatureMismatchError.Id);
}
