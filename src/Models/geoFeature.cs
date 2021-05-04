using System;
using System.Collections.Generic;
using System.Text;

namespace ConvertIfc2Json.Models
{
    public class geoFeature
    {
        public string type { get; set; }
        public string id { get; set; } // Original :  int
        public geoGeometry geometry { get; set; }
        public Dictionary<string, string> properties { get; set; }
    }
}
