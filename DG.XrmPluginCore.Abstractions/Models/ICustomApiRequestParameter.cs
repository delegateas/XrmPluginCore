namespace DG.XrmPluginCore.Abstractions.Models
{
    public interface ICustomApiRequestParameter : ICustomApiParameter
    {
        bool IsOptional { get; }
    }
}
