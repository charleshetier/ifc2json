using GeometryGym.Ifc;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConvertIfc2Json.Models
{
    public sealed class JsonProjectIfcElement: JsonIfcContextElement
    {
        public JsonProjectIfcElement(IfcProject project): base(project)
        {

        }
    }
}
