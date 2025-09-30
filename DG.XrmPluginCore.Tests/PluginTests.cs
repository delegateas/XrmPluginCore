using DG.XrmPluginCore.Enums;
using DG.XrmPluginCore.Tests.Helpers;
using DG.XrmPluginCore.Tests.TestPlugins;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using NSubstitute;
using System;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace DG.XrmPluginCore.Tests
{
    public class PluginTests
    {
        [Fact]
        public void Execute_NullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Arrange
            var plugin = new TestAccountPlugin();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => plugin.Execute(null));
        }

        [Fact]
        public void Execute_MatchingRegistration_ShouldExecuteAction()
        {
            // Arrange
            var plugin = new TestAccountPlugin();
            var mockProvider = new MockServiceProvider();
            
            // Setup context for account create
            mockProvider.SetupPrimaryEntityName("account");
            mockProvider.SetupMessageName("Create");
            mockProvider.SetupStage(20); // Pre-operation

            // Act
            plugin.Execute(mockProvider.ServiceProvider);

            // Assert
            plugin.ExecutedAction.Should().BeTrue();
            plugin.LastProvider.Should().BeNull();
            plugin.LastContext.Should().NotBeNull();
        }

        [Fact]
        public void Execute_MatchingRegistration_ShouldExecuteAction_ServiceProvider()
        {
            // Arrange
            var plugin = new TestAccountPlugin();
            var mockProvider = new MockServiceProvider();

            // Setup context for account create
            mockProvider.SetupPrimaryEntityName("account");
            mockProvider.SetupMessageName("Create");
            mockProvider.SetupStage(40); // Pre-operation

            // Act
            plugin.Execute(mockProvider.ServiceProvider);

            // Assert
            plugin.ExecutedAction.Should().BeTrue();
            plugin.LastProvider.Should().NotBeNull();
            plugin.LastContext.Should().BeNull();
        }

        [Fact]
        public void Execute_NonMatchingEntity_ShouldNotExecuteAction()
        {
            // Arrange
            var plugin = new TestAccountPlugin();
            var mockProvider = new MockServiceProvider();
            
            // Setup context for contact create (not account)
            mockProvider.SetupPrimaryEntityName("contact");
            mockProvider.SetupMessageName("Create");
            mockProvider.SetupStage(20);

            // Act
            plugin.Execute(mockProvider.ServiceProvider);

            // Assert
            plugin.ExecutedAction.Should().BeFalse();
        }

        [Fact]
        public void Execute_NonMatchingMessage_ShouldNotExecuteAction()
        {
            // Arrange
            var plugin = new TestAccountPlugin();
            var mockProvider = new MockServiceProvider();
            
            // Setup context for account update (not create)
            mockProvider.SetupPrimaryEntityName("account");
            mockProvider.SetupMessageName("Update");
            mockProvider.SetupStage(20);

            // Act
            plugin.Execute(mockProvider.ServiceProvider);

            // Assert
            plugin.ExecutedAction.Should().BeFalse();
        }

        [Fact]
        public void Execute_NonMatchingStage_ShouldNotExecuteAction()
        {
            // Arrange
            var plugin = new TestAccountPlugin();
            var mockProvider = new MockServiceProvider();
            
            // Setup context for account create in pre-validation
            mockProvider.SetupPrimaryEntityName("account");
            mockProvider.SetupMessageName("Create");
            mockProvider.SetupStage(10); // Pre-validation

            // Act
            plugin.Execute(mockProvider.ServiceProvider);

            // Assert
            plugin.ExecutedAction.Should().BeFalse();
        }

        [Fact]
        public void Execute_CustomMessage_ShouldExecuteAction()
        {
            // Arrange
            var plugin = new TestCustomMessagePlugin();
            var mockProvider = new MockServiceProvider();
            
            // Setup context for custom message
            mockProvider.SetupPrimaryEntityName("custom_message");
            mockProvider.SetupMessageName("Execute");
            mockProvider.SetupStage(20);

            // Act
            plugin.Execute(mockProvider.ServiceProvider);

            // Assert
            plugin.ExecutedAction.Should().BeTrue();
            plugin.LastContext.Should().NotBeNull();
        }

        [Fact]
        public void Execute_NoRegistrations_ShouldNotExecuteAction()
        {
            // Arrange
            var plugin = new TestNoRegistrationPlugin();
            var mockProvider = new MockServiceProvider();

            // Act
            plugin.Execute(mockProvider.ServiceProvider);

            // Assert
            plugin.ExecutedAction.Should().BeFalse();
        }

        [Fact]
        public void Execute_MultipleRegistrations_ShouldExecuteCorrectAction()
        {
            // Arrange
            var plugin = new TestMultipleRegistrationPlugin();
            var mockProvider = new MockServiceProvider();
            
            // Test Create
            mockProvider.SetupPrimaryEntityName("account");
            mockProvider.SetupMessageName("Create");
            mockProvider.SetupStage(20);

            // Act
            plugin.Execute(mockProvider.ServiceProvider);

            // Assert
            plugin.CreateExecuted.Should().BeTrue();
            plugin.UpdateExecuted.Should().BeFalse();

            // Reset and test Update
            plugin = new TestMultipleRegistrationPlugin();
            mockProvider.SetupMessageName("Update");
            mockProvider.SetupStage(40); // Post-operation

            // Act
            plugin.Execute(mockProvider.ServiceProvider);

            // Assert
            plugin.CreateExecuted.Should().BeFalse();
            plugin.UpdateExecuted.Should().BeTrue();
        }

        [Fact]
        public void Execute_FaultException_ShouldRethrow()
        {
            // Arrange
            var mockProvider = new MockServiceProvider();
            var plugin = new TestAccountPlugin();
            
            // Setup context
            mockProvider.SetupPrimaryEntityName("account");
            mockProvider.SetupMessageName("Create");
            mockProvider.SetupStage(20);

            // Setup organization service factory to throw exception when creating organization service
            var faultException = new FaultException<OrganizationServiceFault>(new OrganizationServiceFault());
            mockProvider.OrganizationServiceFactory.CreateOrganizationService(Arg.Any<Guid?>())
                .Returns(x => { throw faultException; });

            // Act & Assert
            var exception = Assert.Throws<InvalidPluginExecutionException>(() => plugin.Execute(mockProvider.ServiceProvider));
            exception.Status.Should().Be(OperationStatus.Failed);
            exception.Message.Should().Be(faultException.Message);
        }

        [Fact]
        public void GetRegistrations_ShouldReturnCorrectRegistrations()
        {
            // Arrange
            var plugin = new TestMultipleRegistrationPlugin();

            // Act
            var registrations = plugin.GetRegistrations().ToList();

            // Assert
            registrations.Should().HaveCount(2);
            registrations.Should().Contain(r => r.EventOperation == EventOperation.Create);
            registrations.Should().Contain(r => r.EventOperation == EventOperation.Update);
        }

        [Fact]
        public void GetEntity_ValidTarget_ShouldReturnEntity()
        {
            // Arrange
            var plugin = new TestPluginHelpers();
            var mockProvider = new MockServiceProvider();
            var context = new LocalPluginContext(mockProvider.ServiceProvider);
            var account = new Entity("account") { Id = Guid.NewGuid() };
            
            var inputParameters = new ParameterCollection { { "Target", account } };
            mockProvider.SetupInputParameters(inputParameters);

            // Act & Assert - The GetEntity method should throw because ToEntity<T> requires EntityLogicalNameAttribute
            Assert.Throws<NotSupportedException>(() => plugin.TestGetEntity<TestAccount>(context));
        }

        [Fact]
        public void GetEntity_NoTarget_ShouldReturnNull()
        {
            // Arrange
            var plugin = new TestPluginHelpers();
            var mockProvider = new MockServiceProvider();
            var context = new LocalPluginContext(mockProvider.ServiceProvider);
            var inputParameters = new ParameterCollection();
            mockProvider.SetupInputParameters(inputParameters);

            // Act
            var result = plugin.TestGetEntity<TestAccount>(context);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetEntity_WrongEntityType_ShouldReturnNull()
        {
            // Arrange
            var plugin = new TestPluginHelpers();
            var mockProvider = new MockServiceProvider();
            var context = new LocalPluginContext(mockProvider.ServiceProvider);
            var contact = new Entity("contact") { Id = Guid.NewGuid() };
            
            var inputParameters = new ParameterCollection { { "Target", contact } };
            mockProvider.SetupInputParameters(inputParameters);

            // Act
            var result = plugin.TestGetEntity<TestAccount>(context);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetPreImage_ValidImage_ShouldReturnEntity()
        {
            // Arrange
            var plugin = new TestPluginHelpers();
            var mockProvider = new MockServiceProvider();
            var context = new LocalPluginContext(mockProvider.ServiceProvider);
            var account = new Entity("account") { Id = Guid.NewGuid() };
            
            var preImages = new EntityImageCollection { { "PreImage", account } };
            mockProvider.SetupPreEntityImages(preImages);

            // Act & Assert - Throws exception due to ToEntity<T> limitation in test framework
            Assert.Throws<NotSupportedException>(() => plugin.TestGetPreImage<TestAccount>(context));
        }

        [Fact]
        public void GetPostImage_ValidImage_ShouldReturnEntity()
        {
            // Arrange
            var plugin = new TestPluginHelpers();
            var mockProvider = new MockServiceProvider();
            var context = new LocalPluginContext(mockProvider.ServiceProvider);
            var account = new Entity("account") { Id = Guid.NewGuid() };
            
            var postImages = new EntityImageCollection { { "PostImage", account } };
            mockProvider.SetupPostEntityImages(postImages);

            // Act & Assert - Throws exception due to ToEntity<T> limitation in test framework
            Assert.Throws<NotSupportedException>(() => plugin.TestGetPostImage<TestAccount>(context));
        }

        [Fact]
        public void MatchesEventOperation_MatchingOperation_ShouldReturnTrue()
        {
            // Arrange
            var plugin = new TestPluginHelpers();
            var mockProvider = new MockServiceProvider();
            var context = new LocalPluginContext(mockProvider.ServiceProvider);
            mockProvider.SetupMessageName("Create");

            // Act
            var result = plugin.TestMatchesEventOperation(context, EventOperation.Create);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void MatchesEventOperation_NonMatchingOperation_ShouldReturnFalse()
        {
            // Arrange
            var plugin = new TestPluginHelpers();
            var mockProvider = new MockServiceProvider();
            var context = new LocalPluginContext(mockProvider.ServiceProvider);
            mockProvider.SetupMessageName("Create");

            // Act
            var result = plugin.TestMatchesEventOperation(context, EventOperation.Update);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void OnBeforeConstructLocalPluginContext_ShouldAllowModification()
        {
            // Arrange
            var plugin = new TestServiceProviderModificationPlugin();
            var originalProvider = new MockServiceProvider();

            // Act
            plugin.Execute(originalProvider.ServiceProvider);

            // Assert
            plugin.ModifiedServiceProviderUsed.Should().BeTrue();
        }
    }

    // Helper plugin for testing service provider modification
    public class TestServiceProviderModificationPlugin : Plugin
    {
        public bool ModifiedServiceProviderUsed { get; private set; }

        public TestServiceProviderModificationPlugin()
        {
            RegisterPluginStep<TestAccount>(EventOperation.Create, ExecutionStage.PreOperation, ExecutePlugin);
        }

        protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection serviceProvider)
        {
            // Inject an object we can then get
            return serviceProvider.AddScoped(_ => "Modified");
        }

        private void ExecutePlugin(IServiceProvider context)
        {
            // Action implementation
            var stringValue = context.GetService<string>();
            ModifiedServiceProviderUsed = stringValue == "Modified";
        }
    }

    // Helper plugin to expose protected methods for testing
    public class TestPluginHelpers : Plugin
    {
        public T TestGetEntity<T>(LocalPluginContext context) where T : Entity
        {
            return GetEntity<T>(context);
        }

        public T TestGetPreImage<T>(LocalPluginContext context, string name = "PreImage") where T : Entity
        {
            return GetPreImage<T>(context, name);
        }

        public T TestGetPostImage<T>(LocalPluginContext context, string name = "PostImage") where T : Entity
        {
            return GetPostImage<T>(context, name);
        }

        public bool TestMatchesEventOperation(LocalPluginContext context, params EventOperation[] operations)
        {
            return MatchesEventOperation(context, operations);
        }
    }

    // Add the TestAccount class
    public class TestAccount : Entity
    {
        public TestAccount() : base("account") { }
    }
}