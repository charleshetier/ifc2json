using System;
using System.Collections.Generic;
using System.Text;

namespace ConvertIfc2Json.Models
{
    public class geoMultiPoint
    {
        public string type { get; set; }
        public IList<IList<double>> coordinates { get; set; }
    }
}
