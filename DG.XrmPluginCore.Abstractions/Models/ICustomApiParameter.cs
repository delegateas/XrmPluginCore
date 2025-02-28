namespace DG.XrmPluginCore.Abstractions.Models
{
    public interface ICustomApiParameter
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
