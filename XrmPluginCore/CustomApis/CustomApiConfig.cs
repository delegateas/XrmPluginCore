using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.CustomApi;
using System;
using System.Collections.Generic;

namespace XrmPluginCore.CustomApis
{
	public class CustomApiConfig : ICustomApiConfig
	{
		public string UniqueName { get; internal set; }

		public string Name { get; internal set; }

		public string DisplayName { get; internal set; }

		public bool IsFunction { get; internal set; }

		public bool EnabledForWorkflow { get; internal set; }

		public AllowedCustomProcessingStepType AllowedCustomProcessingStepType { get; internal set; }

		public BindingType BindingType { get; internal set; }

		public string BoundEntityLogicalName { get; internal set; }

		public Guid? OwnerId { get; internal set; }

		public OwnerType? OwnerType { get; internal set; }

		public bool IsCustomizable { get; internal set; }

		public bool IsPrivate { get; internal set; }

		public string ExecutePrivilegeName { get; internal set; }

		public string Description { get; internal set; }

		public IEnumerable<IRequestParameter> RequestParameters { get; internal set; }

		public IEnumerable<IResponseProperty> ResponseProperties { get; internal set; }
	}
}
