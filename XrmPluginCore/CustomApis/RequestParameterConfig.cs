using DG.XrmPluginCore.Abstractions.Models;

namespace DG.XrmPluginCore.CustomApis
{
    public class RequestParameterConfig : ParameterConfig, ICustomApiRequestParameter
    {
        public bool IsOptional { get; internal set; }

        protected override string Direction => "In";

        internal RequestParameterConfig(string apiName, string uniqueName, CustomApiParameterType type, string displayName = null, string description = null, bool isCustomizable = false, bool isOptional = false, string logicalEntityName = null)
            : base(apiName, uniqueName, displayName ?? uniqueName, description, isCustomizable, logicalEntityName, type)
        {
            IsOptional = isOptional;
        }

        internal RequestParameterConfig()
        {

        }
    }
}
