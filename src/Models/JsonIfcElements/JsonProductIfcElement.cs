using GeometryGym.Ifc;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConvertIfc2Json.Models
{
    public sealed class JsonProductIfcElement : JsonIfcElement
    {
        public JsonProductIfcElement(IfcProduct product, JsonStoreyIfcElement parent, JsonConversionContext context) : base(product)
        {
            userData.buildingStorey = new string[] { };
            userData.siteId = parent.userData?.siteId;
            userData.projectId = parent.userData?.projectId;
            userData.buildingId = parent.userData?.buildingId;

            // Environnement element
            userData.buildingStorey = new[] { parent.id };

            // Extract pset
            product.ExtractPset(this);
        }
    }
}
