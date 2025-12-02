using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.CustomApi;

namespace XrmPluginCore.CustomApis
{
	public class RequestParameter : IRequestParameter
	{
		internal RequestParameter(
			string name,
			string uniqueName,
			string displayName,
			string description,
			bool isCustomizable,
			string logicalEntityName,
			CustomApiParameterType type,
			bool isOptional)
		{
			Name = name;
			UniqueName = uniqueName;
			DisplayName = displayName;
			Description = description;
			IsCustomizable = isCustomizable;
			LogicalEntityName = logicalEntityName;
			Type = type;
			IsOptional = isOptional;
		}

		internal RequestParameter(IRequestParameter requestParameter)
		{
			Name = requestParameter.Name;
			UniqueName = requestParameter.UniqueName;
			DisplayName = requestParameter.DisplayName;
			Description = requestParameter.Description;
			IsCustomizable = requestParameter.IsCustomizable;
			LogicalEntityName = requestParameter.LogicalEntityName;
			Type = requestParameter.Type;
			IsOptional = requestParameter.IsOptional;
		}

		public bool IsOptional { get; }

		public string Name { get; internal set; }

		public string UniqueName { get; }

		public string DisplayName { get; internal set; }

		public string Description { get; }

		public bool IsCustomizable { get; }

		public string LogicalEntityName { get; }

		public CustomApiParameterType Type { get; }
	}
}
