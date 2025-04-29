using DG.XrmPluginCore.Enums;

namespace DG.XrmPluginCore.Interfaces.Plugin
{
    public interface IImageSpecification
    {
        string Attributes { get; }
        string EntityAlias { get; }
        string ImageName { get; }
        ImageType ImageType { get; }
    }
}