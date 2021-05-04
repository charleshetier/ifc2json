using ConvertIfc2Json.Models;
using GeometryGym.Ifc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ConvertIfc2Json.Program;

namespace ConvertIfc2Json
{
    public static class IfcObjectExtensions
    {
        private static void extractPsetBase(IfcObject element, JsonIfcElement newElement, string logId = null)
        {
            if (element.IsDefinedBy != null && element.IsDefinedBy.Count > 0)
            {
                foreach (var psv in element.IsDefinedBy
                    .OfType<IfcPropertySet>()
                    .SelectMany(pset => pset.HasProperties
                    .Select(tuple => tuple.Value)
                    .OfType<IfcPropertySingleValue>()))
                {
                    try
                    {
                        if (psv.Name != null && psv.NominalValue.ValueString != null
                            && !newElement.userData.pset.ContainsKey(psv.Name))
                        {
                            newElement.userData.pset.Add(psv.Name, psv.NominalValue.ValueString);
                        }
                    }
                    catch (Exception e)
                    {
                        if (logId != null) Console.WriteLine($"{logId}. Pset write error {e.Message}");
                    }
                }
            }
        }

        public static void ExtractPset(this IfcSite element, JsonIfcElement newElement) => extractPsetBase(element, newElement, logId: "21");
        public static void ExtractPset(this IfcProduct element, JsonIfcElement newElement) => extractPsetBase(element, newElement);
        public static void ExtractPset(this IfcBuilding element, JsonIfcElement newElement) => extractPsetBase(element, newElement, logId: "23");
        public static void ExtractPset(this IfcBuildingElementProxy element, JsonIfcElement newElement) => extractPsetBase(element, newElement, logId: "24");
        public static void ExtractPset(this IfcBuildingStorey element, JsonIfcElement newElement) => extractPsetBase(element, newElement, logId: "25");
    }
}
