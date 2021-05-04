using GeometryGym.Ifc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConvertIfc2Json.Models
{
    public sealed class JsonBuildingIfcElement: JsonIfcObjectElement
    {
        public JsonBuildingIfcElement(IfcBuilding building, JsonSiteIfcElement parent, JsonConversionContext context): base(building)
        {
            userData.siteId = parent.userData?.siteId;
            userData.projectId = parent.userData?.projectId;

            // Add Matrix
            var bObjectPlacements = building.ObjectPlacement.Extract<IfcObjectPlacement>();
            var bLocalPlacements = bObjectPlacements[0].Extract<IfcLocalPlacement>();
            var bPos = bLocalPlacements[0].RelativePlacement as IfcAxis2Placement3D;
            if (bPos.Location != null) userData.location = bPos.Location.Coordinates[0] / context.SCALE + "," + bPos.Location.Coordinates[1] / context.SCALE + "," + bPos.Location.Coordinates[2] / context.SCALE;
            if (bPos.RefDirection != null) userData.refDirection = bPos.RefDirection.DirectionRatios[0] + "," + bPos.RefDirection.DirectionRatios[1] + "," + bPos.RefDirection.DirectionRatios[2];
            if (bPos.Axis != null) userData.axis = bPos.Axis.DirectionRatios[0] + "," + bPos.Axis.DirectionRatios[1] + "," + bPos.Axis.DirectionRatios[2];

            // Extract Pset
            building.ExtractPset(this);

            // building Address
            try
            {
                if (building.BuildingAddress != null)
                {
                    if (building.BuildingAddress.AddressLines.Count > 0)
                    {
                        for (var i = 0; i < building.BuildingAddress.AddressLines.Count; i++)
                        {
                            var index = i + 1;
                            userData.pset.Add($"AddressLine {index}", building.BuildingAddress.AddressLines[i]);
                        }
                    }
                    if (building.BuildingAddress.PostalBox != "") userData.pset.Add("PostalBox", building.BuildingAddress.PostalBox);
                    if (building.BuildingAddress.PostalCode != "") userData.pset.Add("PostalCode", building.BuildingAddress.PostalCode);
                    if (building.BuildingAddress.Town != "") userData.pset.Add("Town", building.BuildingAddress.Town);
                    if (building.BuildingAddress.Region != "") userData.pset.Add("Region", building.BuildingAddress.Region);
                    if (building.BuildingAddress.Country != "") userData.pset.Add("Country", building.BuildingAddress.Country);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"5. Buildind Adresse (id: {id}) : { ex.Message}");
            }
        }
    }
}
