using DG.XrmPluginCore.Enums;

namespace DG.XrmPluginCore.Interfaces.CustomApi
{
    public interface IParameter
    {
        string Name { get; }
        string UniqueName { get; }
        string DisplayName { get; }
        string Description { get; }
        bool IsCustomizable { get; }
        string LogicalEntityName { get; }
        CustomApiParameterType Type { get; }
    }
}
