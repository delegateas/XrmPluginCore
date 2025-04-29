using DG.XrmPluginCore.Enums;
using DG.XrmPluginCore.Interfaces.Plugin;

namespace DG.XrmPluginCore.Models.Plugin
{
    public class ImageSpecification : IImageSpecification
    {
        public ImageSpecification(string imageName, string entityAlias, ImageType imageType, string attributes)
        {
            ImageName = imageName;
            EntityAlias = entityAlias;
            ImageType = imageType;
            Attributes = !string.IsNullOrEmpty(attributes) ? attributes : null;
        }

        public ImageSpecification(IImageSpecification imageSpecification)
        {
            ImageName = imageSpecification.ImageName;
            EntityAlias = imageSpecification.EntityAlias;
            ImageType = imageSpecification.ImageType;
            Attributes = !string.IsNullOrEmpty(imageSpecification.Attributes) ? imageSpecification.Attributes : null;
        }

        public string ImageName { get; set; }
        public string EntityAlias { get; set; }
        public ImageType ImageType { get; set; }
        public string Attributes { get; set; }
    }
}
