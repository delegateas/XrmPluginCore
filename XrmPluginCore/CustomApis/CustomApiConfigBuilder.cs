using XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using XrmPluginCore.Helpers;

namespace XrmPluginCore.CustomApis
{
	public class CustomApiConfigBuilder
	{
		private readonly Collection<RequestParameter> RequestParameters = new Collection<RequestParameter>();
		private readonly Collection<ResponseProperty> ResponseProperties = new Collection<ResponseProperty>();

		private CustomApiConfig Config { get; }

		/// <summary>
		/// The unique name of the Custom API being configured.
		/// </summary>
		public string Name => Config.Name;

		public CustomApiConfigBuilder(string name)
		{
			Config = new CustomApiConfig()
			{
				Name = name,
				DisplayName = name,
				UniqueName = name,
				IsFunction = false,
				EnabledForWorkflow = false,
				AllowedCustomProcessingStepType = 0, // None
				BindingType = 0, // Global
				BoundEntityLogicalName = "",
				IsCustomizable = false,
				IsPrivate = false,
				ExecutePrivilegeName = null,
				Description = $"CustomAPI Definition for {name}",
				OwnerId = null,
				OwnerType = null
			};
		}

		public CustomApiConfig Build()
		{
			return new CustomApiConfig
			{
				UniqueName = Config.UniqueName,
				Name = Config.Name,
				DisplayName = Config.DisplayName,
				IsFunction = Config.IsFunction,
				EnabledForWorkflow = Config.EnabledForWorkflow,
				AllowedCustomProcessingStepType = Config.AllowedCustomProcessingStepType,
				BindingType = Config.BindingType,
				BoundEntityLogicalName = Config.BoundEntityLogicalName,
				OwnerId = Config.OwnerId,
				OwnerType = Config.OwnerType,
				IsCustomizable = Config.IsCustomizable,
				IsPrivate = Config.IsPrivate,
				ExecutePrivilegeName = Config.ExecutePrivilegeName,
				Description = Config.Description,
				RequestParameters = RequestParameters.Select(r => new RequestParameter(r)),
				ResponseProperties = ResponseProperties.Select(r => new ResponseProperty(r))
			};
		}

		public CustomApiConfigBuilder AllowCustomProcessingStep(AllowedCustomProcessingStepType type)
		{
			Config.AllowedCustomProcessingStepType = type;
			return this;
		}

		public CustomApiConfigBuilder Bind<T>(BindingType bindingType) where T : Entity, new()
		{
			Config.BindingType = bindingType;
			Config.BoundEntityLogicalName = EntityLogicalNameCache.GetLogicalName<T>();
			return this;
		}

		public CustomApiConfigBuilder MakeFunction()
		{
			Config.IsFunction = true;
			return this;
		}
		public CustomApiConfigBuilder MakePrivate()
		{
			Config.IsPrivate = true;
			return this;
		}

		public CustomApiConfigBuilder EnableForWorkFlow()
		{
			Config.EnabledForWorkflow = true;
			return this;
		}

		public CustomApiConfigBuilder EnableCustomization()
		{
			Config.IsCustomizable = true;
			return this;
		}

		public CustomApiConfigBuilder SetDescription(string description)
		{
			Config.Description = description;
			return this;
		}

		public CustomApiConfigBuilder SetOwner(Guid? ownerId, OwnerType? ownerType)
		{
			Config.OwnerId = ownerId;
			Config.OwnerType = ownerType;

			return this;
		}

		public CustomApiConfigBuilder WithExecutePrivilegeName(string privilegeName)
		{
			Config.ExecutePrivilegeName = privilegeName;
			return this;
		}

		/// <summary>
		/// Sets the execute privilege to a standard table privilege of <typeparamref name="T"/>,
		/// e.g. <c>WithExecutePrivilege&lt;Account&gt;(Privilege.Read)</c> resolves to <c>prvReadAccount</c>.
		/// The entity schema name is taken from the early-bound type name (<c>typeof(T).Name</c>), which
		/// early-bound generators produce from the entity schema name.
		/// </summary>
		/// <typeparam name="T">The early-bound entity type the privilege applies to.</typeparam>
		/// <param name="privilege">The table privilege required to execute the custom API.</param>
		public CustomApiConfigBuilder WithExecutePrivilege<T>(Privilege privilege) where T : Entity
		{
			return WithExecutePrivilege(typeof(T).Name, privilege);
		}

		/// <summary>
		/// Sets the execute privilege to a standard table privilege of the entity identified by
		/// <paramref name="entitySchemaName"/>, following the <c>prv{Privilege}{EntitySchemaName}</c>
		/// convention. Privilege names use the schema name (e.g. <c>Account</c>), not the logical name
		/// (<c>account</c>). Prefer the generic <see cref="WithExecutePrivilege{T}(Privilege)"/> overload
		/// when an early-bound type is available.
		/// </summary>
		/// <param name="entitySchemaName">The schema name of the table the privilege applies to.</param>
		/// <param name="privilege">The table privilege required to execute the custom API.</param>
		public CustomApiConfigBuilder WithExecutePrivilege(string entitySchemaName, Privilege privilege)
		{
			Config.ExecutePrivilegeName = PrivilegeNameResolver.GetExecutePrivilegeName(privilege, entitySchemaName);
			return this;
		}

		public CustomApiConfigBuilder AddRequestParameter(string uniqueName, CustomApiParameterType type,
			string displayName = null,
			string description = null,
			bool isCustomizable = false,
			bool isOptional = false,
			string logicalEntityName = null)
		{
			var parameterName = $"{Config.Name}-In-{uniqueName}";

			RequestParameters.Add(new RequestParameter(parameterName, uniqueName, displayName ?? parameterName, description, isCustomizable, logicalEntityName, type, isOptional));
			return this;
		}

		public CustomApiConfigBuilder AddRequestParameter(RequestParameter reqParam)
		{
			reqParam.Name = $"{Config.Name}-In-{reqParam.UniqueName}";
			reqParam.DisplayName = reqParam.DisplayName ?? reqParam.Name;

			RequestParameters.Add(reqParam);
			return this;
		}

		public CustomApiConfigBuilder AddResponseProperty(string uniqueName, CustomApiParameterType type,
			string displayName = null,
			string description = null,
			bool isCustomizable = false,
			string logicalEntityName = null)
		{
			var parameterName = $"{Config.Name}-Out-{uniqueName}";

			ResponseProperties.Add(new ResponseProperty(parameterName, uniqueName, displayName ?? parameterName, description, isCustomizable, logicalEntityName, type));
			return this;
		}

		public CustomApiConfigBuilder AddResponseProperty(ResponseProperty respProperty)
		{
			respProperty.Name = $"{Config.Name}-Out-{respProperty.UniqueName}";
			respProperty.DisplayName = respProperty.DisplayName ?? respProperty.Name;

			ResponseProperties.Add(respProperty);
			return this;
		}
	}
}
