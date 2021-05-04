using GeometryGym.Ifc;
using System;

namespace ConvertIfc2Json.Models
{
    public sealed class JsonSiteIfcElement : JsonIfcObjectElement
    {
        public JsonSiteIfcElement(IfcSite site, JsonProjectIfcElement parent, JsonConversionContext context) : base(site)
        {
            userData.projectId = parent?.id;

            if (site.RefLatitude != null && !userData.pset.ContainsKey("RefLatitude"))
            {
                userData.pset.Add("RefLatitude", site.RefLatitude.ToSTEP().ToString());
            }

            if (site.RefLongitude != null && !userData.pset.ContainsKey("RefLongitude"))
            {
                userData.pset.Add("RefLongitude", site.RefLongitude.ToSTEP().ToString());
            }

            if (site.RefElevation != 0 && !userData.pset.ContainsKey("RefElevation"))
            {
                userData.pset.Add("RefElevation", Convert.ToString(site.RefElevation));
            }

            // Extract Pset
            try
            {
                site.ExtractPset(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine("4. Site pset error : " + ex.Message);
            }

            // Add Matrix
            var sObjectPlacements = site.ObjectPlacement.Extract<IfcObjectPlacement>();
            var sLocalPlacements = sObjectPlacements[0].Extract<IfcLocalPlacement>();
            var sPos = sLocalPlacements[0].RelativePlacement as IfcAxis2Placement3D;
            if (sPos.Location != null) userData.location = sPos.Location.Coordinates[0] / context.SCALE + "," + sPos.Location.Coordinates[1] / context.SCALE + "," + sPos.Location.Coordinates[2] / context.SCALE;
            if (sPos.RefDirection != null) userData.refDirection = sPos.RefDirection.DirectionRatios[0] + "," + sPos.RefDirection.DirectionRatios[1] + "," + sPos.RefDirection.DirectionRatios[2];
            if (sPos.Axis != null) userData.axis = sPos.Axis.DirectionRatios[0] + "," + sPos.Axis.DirectionRatios[1] + "," + sPos.Axis.DirectionRatios[2];
        }
    }
}
