using System;
using System.Collections.Generic;
using System.Text;

namespace ConvertIfc2Json.Models
{
    public class geoMultiLineString
    {
        public string type { get; set; }
        public IList<IList<IList<double>>> coordinates { get; set; }
    }
}
