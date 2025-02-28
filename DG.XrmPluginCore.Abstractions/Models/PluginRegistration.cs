using System.Collections.Generic;

namespace DG.XrmPluginCore.Abstractions.Models
{
    public class PluginRegistration
    {
        public PluginRegistration(StepConfig stepConfig, ExtendedStepConfig extendedStepConfig, IEnumerable<ImageSpecification> images)
        {
            StepConfig = stepConfig;
            ExtendedStepConfig = extendedStepConfig;
            Images = images;
        }

        public StepConfig StepConfig { get; set; }
        public ExtendedStepConfig ExtendedStepConfig { get; set; }
        public IEnumerable<ImageSpecification> Images { get; set; }
    }
}
