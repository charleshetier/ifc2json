using GeometryGym.Ifc;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConvertIfc2Json.Models
{
    public abstract class JsonIfcElement
    {
        public JsonIfcElement(IfcRoot ifcElement)
        {
            if (ifcElement.GlobalId != null)
            {
                id = ifcElement.GlobalId;
                userData = new JsonIfcUserData();
            }
        }

        public string id { get; private set; }
        public JsonIfcUserData userData { get; private set; }
        public geoFeature boundary { get; set; }
    }

    public abstract class JsonIfcContextElement: JsonIfcElement
    {
        public JsonIfcContextElement(IfcContext ifcElement): base(ifcElement)
        {
            if (ifcElement.ObjectType != null) userData.objectType = ifcElement.ObjectType;
            if (ifcElement.Name != null) userData.name = ifcElement.Name;
            if (ifcElement.StepClassName != null) userData.type = ifcElement.StepClassName;
        }
    }

    public abstract class JsonIfcObjectElement : JsonIfcElement
    {
        public JsonIfcObjectElement(IfcObject ifcElement) : base(ifcElement)
        {
            if (ifcElement.ObjectType != null) userData.objectType = ifcElement.ObjectType;
            if (ifcElement.Name != null) userData.name = ifcElement.Name;
            if (ifcElement.StepClassName != null) userData.type = ifcElement.StepClassName;
        }
    }
}
