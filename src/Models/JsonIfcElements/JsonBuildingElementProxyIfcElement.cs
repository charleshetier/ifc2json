using GeometryGym.Ifc;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConvertIfc2Json.Models
{
    public sealed class JsonBuildingElementProxyIfcElement : JsonIfcObjectElement
    {
        public JsonBuildingElementProxyIfcElement(IfcBuildingElementProxy buildingElementProxy, JsonStoreyIfcElement parent, JsonConversionContext context) : base(buildingElementProxy)
        {
            userData.buildingStorey = new string[] { };
            userData.siteId = parent.userData?.siteId;
            userData.projectId = parent.userData?.projectId;
            userData.buildingId = parent.userData?.buildingId;

            // Environnement element
            userData.buildingStorey = new[] { parent.id };

            if (buildingElementProxy.Tag != null) userData.tag = buildingElementProxy.Tag;

            // Extract pset
            buildingElementProxy.ExtractPset(this);
        }
    }
}
