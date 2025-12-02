using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;
using NSubstitute;
using System;
using XrmPluginCore.Enums;

namespace XrmPluginCore.Tests.Helpers
{
	public class MockServiceProvider
	{
		public IServiceProvider ServiceProvider { get; private set; }
		public IPluginExecutionContext PluginExecutionContext { get; private set; }
		public ITracingService TracingService { get; private set; }
		public IOrganizationServiceFactory OrganizationServiceFactory { get; private set; }
		public IOrganizationService OrganizationService { get; private set; }
		public IOrganizationService OrganizationAdminService { get; private set; }
		public ILogger PluginTelemetryLogger { get; private set; }

		public MockServiceProvider()
		{
			SetupMocks();
		}

		private void SetupMocks()
		{
			PluginExecutionContext = Substitute.For<IPluginExecutionContext>();
			TracingService = Substitute.For<ITracingService>();
			OrganizationServiceFactory = Substitute.For<IOrganizationServiceFactory>();
			OrganizationService = Substitute.For<IOrganizationService>();
			OrganizationAdminService = Substitute.For<IOrganizationService>();
			PluginTelemetryLogger = Substitute.For<ILogger>();

			// Setup default values
			PluginExecutionContext.UserId.Returns(Guid.NewGuid());
			PluginExecutionContext.CorrelationId.Returns(Guid.NewGuid());
			PluginExecutionContext.InitiatingUserId.Returns(Guid.NewGuid());
			PluginExecutionContext.MessageName.Returns("Create");
			PluginExecutionContext.PrimaryEntityName.Returns("account");
			PluginExecutionContext.Stage.Returns((int)ExecutionStage.PreOperation); // Pre-operation

			// Setup organization service factory
			OrganizationServiceFactory.CreateOrganizationService(Arg.Any<Guid?>()).Returns(callInfo =>
			{
				var userId = callInfo.Arg<Guid?>();
				return userId.HasValue ? OrganizationService : OrganizationAdminService;
			});

			// Setup service provider to return mocked services
			var serviceCollection = new ServiceCollection();
			serviceCollection.AddScoped(_ => PluginExecutionContext);
			serviceCollection.AddScoped(_ => TracingService);
			serviceCollection.AddScoped(_ => PluginTelemetryLogger);
			serviceCollection.AddScoped(_ => OrganizationServiceFactory);
			serviceCollection.AddScoped(_ => Substitute.For<IManagedIdentityService>());
			ServiceProvider = serviceCollection.BuildServiceProvider();
		}

		public void SetupInputParameters(ParameterCollection inputParameters)
		{
			PluginExecutionContext.InputParameters.Returns(inputParameters);
		}

		public void SetupOutputParameters(ParameterCollection outputParameters)
		{
			PluginExecutionContext.OutputParameters.Returns(outputParameters);
		}

		public void SetupPreEntityImages(EntityImageCollection preImages)
		{
			PluginExecutionContext.PreEntityImages.Returns(preImages);
		}

		public void SetupPostEntityImages(EntityImageCollection postImages)
		{
			PluginExecutionContext.PostEntityImages.Returns(postImages);
		}

		public void SetupPrimaryEntityName(string entityName)
		{
			PluginExecutionContext.PrimaryEntityName.Returns(entityName);
		}

		public void SetupMessageName(string messageName)
		{
			PluginExecutionContext.MessageName.Returns(messageName);
		}

		public void SetupStage(int stage)
		{
			PluginExecutionContext.Stage.Returns(stage);
		}

		public void SetupUserId(Guid userId)
		{
			PluginExecutionContext.UserId.Returns(userId);
		}
	}
}
