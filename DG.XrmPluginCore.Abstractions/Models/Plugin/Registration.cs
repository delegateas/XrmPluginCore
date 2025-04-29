using System.Collections.Generic;

namespace DG.XrmPluginCore.Models.Plugin
{
    public class Registration
    {
        public Registration(StepConfig stepConfig, IEnumerable<ImageSpecification> images)
        {
            StepConfig = stepConfig;
            Images = images;
        }

        public StepConfig StepConfig { get; set; }

        public IEnumerable<ImageSpecification> Images { get; set; }
    }
}
