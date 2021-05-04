using System;
using System.Collections.Generic;
using System.Text;

namespace ConvertIfc2Json.Models
{
    public class JsonIfcUserData
    {
        public string name { get; set; }
        public string type { get; set; }
        public string objectType { get; set; }
        public string tag { get; set; }
        public string projectId { get; set; }
        public string siteId { get; set; }
        public string buildingId { get; set; }
        public string[] buildingStorey { get; set; }
        public string spaceId { get; set; }
        public Dictionary<string, string> pset { get; } = new Dictionary<string, string>();
        public string location { get; set; }
        public string refDirection { get; set; }
        public string axis { get; set; }


    }
}
