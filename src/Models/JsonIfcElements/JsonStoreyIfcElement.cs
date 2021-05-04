using GeometryGym.Ifc;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConvertIfc2Json.Models
{
    public sealed class JsonStoreyIfcElement: JsonIfcElement
    {
        public JsonStoreyIfcElement(IfcBuildingStorey storey, JsonBuildingIfcElement parent, JsonConversionContext context) : base(storey)
        {
            userData.siteId = parent.userData?.siteId;
            userData.projectId = parent.userData?.projectId;
            userData.buildingId = parent.userData?.buildingId;

            // userData.objectType = storey.objectType; // comment is from original code
            userData.type = "IfcBuildingStorey";
            userData.name = storey.LongName;

            // Extract Pset
            // extractPset(ref storeyElement, buildingStorey);

            if (!userData.pset.ContainsKey("Elevation")) userData.pset.Add("Elevation", (storey.Elevation / context.SCALE).ToString());

            // Add Matrix
            var bsObjectPlacements = storey.ObjectPlacement.Extract<IfcObjectPlacement>();
            var bsLocalPlacements = bsObjectPlacements[0].Extract<IfcLocalPlacement>();
            var bsPos = bsLocalPlacements[0].RelativePlacement as IfcAxis2Placement3D;
            if (bsPos.Location != null) userData.location = bsPos.Location.Coordinates[0] / context.SCALE + "," + bsPos.Location.Coordinates[1] / context.SCALE + "," + bsPos.Location.Coordinates[2] / context.SCALE;
            if (bsPos.RefDirection != null) userData.refDirection = bsPos.RefDirection.DirectionRatios[0] + "," + bsPos.RefDirection.DirectionRatios[1] + "," + bsPos.RefDirection.DirectionRatios[2];
            if (bsPos.Axis != null) userData.axis = bsPos.Axis.DirectionRatios[0] + "," + bsPos.Axis.DirectionRatios[1] + "," + bsPos.Axis.DirectionRatios[2];
        }
    }
}
