using System;
using System.Collections.Generic;
using System.Text;

namespace DG.XrmPluginCore.Abstractions.Models
{
    public class CustomApiRegistration
    {
        public CustomApiConfig CustomApiConfig { get; set; }
        public ExtendedCustomApiConfig ExtendedCustomApiConfig { get; set; }
        public IEnumerable<ICustomApiRequestParameter> RequestParameters { get; set; }

        public IEnumerable<ICustomApiResponseParameter> ResponseParameters { get; set; }
    }
}
