using DG.XrmPluginCore.Abstractions.Enums;

namespace DG.XrmPluginCore.Abstractions.Models.Plugin
{
    public class ImageSpecification
    {
        public ImageSpecification(string imageName, string entityAlias, ImageType imageType, string attributes)
        {
            ImageName = imageName;
            EntityAlias = entityAlias;
            ImageType = imageType;
            Attributes = !string.IsNullOrEmpty(attributes) ? attributes : null;
        }

        public string ImageName { get; set; }
        public string EntityAlias { get; set; }
        public ImageType ImageType { get; set; }
        public string Attributes { get; set; }
    }
}
