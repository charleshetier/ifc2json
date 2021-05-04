using System;
using System.Collections.Generic;
using System.Text;

namespace ConvertIfc2Json.Models
{
    public class geoPoint
    {
        public string type { get; set; }
        public IList<double> coordinates { get; set; }
    }
}
