using System.Collections.Generic;

namespace DG.XrmPluginCore.Abstractions.Models.CustomApi
{
    public class Registration
    {
        public Config Config { get; set; }

        public IEnumerable<RequestParameter> RequestParameters { get; set; }

        public IEnumerable<ResponseProperty> ResponseParameters { get; set; }
    }
}
