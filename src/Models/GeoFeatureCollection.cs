using System;
using System.Collections.Generic;
using System.Text;

namespace ConvertIfc2Json.Models
{
    public class GeoFeatureCollection
    {
        public string type { get; set; }
        public object crs { get; set; }
        public string name { get; set; }
        public bool exceededTransferLimit { get; set; }
        public IList<geoFeature> features { get; set; }
    }
}
