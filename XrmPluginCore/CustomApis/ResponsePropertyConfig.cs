using DG.XrmPluginCore.Abstractions.Models;

namespace DG.XrmPluginCore.CustomApis
{
    public class ResponsePropertyConfig : ParameterConfig, ICustomApiResponseParameter
    {
        protected override string Direction => "Out";

        internal ResponsePropertyConfig(string apiName, string uniqueName, CustomApiParameterType type, string displayName = null, string description = null, bool isCustomizable = false, string logicalEntityName = null) :
            base(apiName, uniqueName, displayName ?? uniqueName, description, isCustomizable, logicalEntityName, type)
        {
        }

        internal ResponsePropertyConfig()
        {
        }
    }
}
