using System;
using System.Collections.Generic;
using System.IO;
using GeometryGym.Ifc;
using MathNet.Spatial.Euclidean;
using MathNet.Numerics;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using ConvertIfc2Json.Models;

namespace ConvertIfc2Json
{
    public class Program
    {
        public static int Main(string[] args)
        {

            var returnMessage = (int)ExitCode.Success;
            var outputElements = new List<JsonIfcElement>();
            var pathSource = string.Empty;
            var pathDest = string.Empty;
            var activeComptactJson = true;
            var readVersion = false;
            var activeFullJson = false;
            //var SCALE = 1.0;
            var context = new JsonConversionContext();
            JsonProjectIfcElement newProject = null;

            try
            {

                foreach (string arg in args)
                {
                    if (arg.ToLower().Trim() == "--version") readVersion = true;
                    if (arg.ToLower().Trim() == "--indented") activeComptactJson = false;
                    if (arg.ToLower().Trim() == "--full") activeFullJson = true;
                    if (arg.Substring(0, 2) != "--" && pathSource != string.Empty && pathDest == string.Empty) pathDest = arg;
                    if (arg.Substring(0, 2) != "--" && pathSource == string.Empty) pathSource = arg;
                }

                if (readVersion)
                {
                    var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    Console.WriteLine($"1. ConvertIfc2Json : {version}{Environment.NewLine}(.Net version {typeof(string).Assembly.ImageRuntimeVersion})");
                    return returnMessage;
                }



                if (File.Exists(pathSource))
                {
                    if (pathDest == string.Empty) pathDest = pathSource + ".json";
                    var db = new DatabaseIfc();
                    IfcProject project;
                    var projectId = "";
                    var sites = new List<IfcSite>();
                    var buildings = new List<IfcBuilding>();

                    try
                    {
                        db = new DatabaseIfc(pathSource);
                        project = db.Project;
                        sites = project.Extract<IfcSite>();



                        // IFC Project
                        try
                        {
                            if (project.GlobalId != null)
                            {
                                // REVIEW ne semble pas servir
                                //foreach (var unit in project.UnitsInContext.Units)
                                //{
                                //    var u = project.UnitsInContext.Extract<IfcSIUnit>();
                                //    // Console.WriteLine("2." + unit.StepClassName);
                                //}

                                // Computing the json conversion scale from current Geogym project
                                context.SCALE = project.GetJsonConversionScale();
                                projectId = project.GlobalId;


                            }

                            outputElements.Add(newProject = new JsonProjectIfcElement(project));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("3. Element read error " + ex.Message);
                            returnMessage = (int)ExitCode.UnknownError;
                        }


                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine("31. Write file : " + ex.Message);
                        returnMessage = (int)ExitCode.InvalidFile;
                    }

                    // IFC Site
                    foreach (var site in sites)
                    {
                        try
                        {
                            if (site.GlobalId != null)
                            {
                                var newSite = new JsonSiteIfcElement(site, newProject, context);
                                outputElements.Add(newSite);

                                // IFC Building
                                buildings = site.Extract<IfcBuilding>();
                                foreach (var building in buildings)
                                {
                                    var newBuildind = new JsonBuildingIfcElement(building, newSite, context); // REVIEW shouldn't create this unused instance if global id is null
                                    if (building.GlobalId != null)
                                    {
                                        outputElements.Add(newBuildind);
                                    }

                                    // IFC Building Storey // Levels
                                    var buildingStoreys = building.Extract<IfcBuildingStorey>();
                                    foreach (var buildingStorey in buildingStoreys)
                                    {
                                        var storeyElement = new JsonStoreyIfcElement(buildingStorey, newBuildind, context);
                                        outputElements.Add(storeyElement);


                                        // IFC Space // Rooms
                                        var spaces = buildingStorey.Extract<IfcSpace>();

                                        // Check IfcProduct Ids
                                        var productsIds = new List<string>();
                                        var productCounter = 0;

                                        // IfcProduct
                                        var products = buildingStorey.Extract<IfcProduct>();
                                        foreach (var product in products)
                                        {
                                            try
                                            {
                                                if (product.GlobalId != null)
                                                {
                                                    var newElementProd = new JsonProductIfcElement(product, storeyElement, context);
                                                    var spaceCounter = 0;

                                                    // Link to the Space
                                                    foreach (var space in spaces)
                                                    {
                                                        try
                                                        {
                                                            // REVIEW = LatestOrDefault
                                                            // IfcSpace
                                                            if (space.GlobalId == product.GlobalId)
                                                            {
                                                                try
                                                                {
                                                                    newElementProd.userData.name = space.LongName;
                                                                }
                                                                catch (NotSupportedException exEncode)
                                                                {
                                                                    newElementProd.userData.name = space.Name;
                                                                    Console.WriteLine("15. Space Name read error (id: " + space.GlobalId + ") " + exEncode.Message); // returnMessage = (int)ExitCode.NodataIsAvailableForEncoding;
                                                                }
                                                                catch (System.Exception ex)
                                                                {
                                                                    Console.WriteLine("29. Space Name LongName read error" + ex.Message);
                                                                }

                                                                newElementProd.userData.pset.Add("number", space.Name);

                                                                // Create boundary
                                                                geoGeometry geom = new geoGeometry();
                                                                IList<IList<IList<double>>> coords = new List<IList<IList<double>>>();
                                                                Dictionary<string, string> props = new Dictionary<string, string>();
                                                                string height = "0.0";
                                                                string elevation = "0.0";


                                                                // Representation
                                                                if (space.Representation.Representations.Count > 0)
                                                                {

                                                                    foreach (IfcRepresentationItem item in space.Representation.Representations[0].Items)
                                                                    {

                                                                        try
                                                                        {

                                                                            if (item.StepClassName == "IfcExtrudedAreaSolid")
                                                                            {
                                                                                IfcExtrudedAreaSolid areaSolid = item as IfcExtrudedAreaSolid;
                                                                                IfcAxis2Placement3D pos = areaSolid.Position;
                                                                                Point3D loc = new Point3D(pos.Location.Coordinates[0], pos.Location.Coordinates[1], pos.Location.Coordinates[2]);
                                                                                height = (areaSolid.Depth / context.SCALE).ToString();
                                                                                elevation = (buildingStorey.Elevation / context.SCALE).ToString();

                                                                                if (areaSolid.SweptArea.StepClassName == "IfcArbitraryClosedProfileDef")
                                                                                { // Polyline
                                                                                    IfcArbitraryClosedProfileDef arbitraryClosedProfiles = areaSolid.SweptArea as IfcArbitraryClosedProfileDef;
                                                                                    IList<IList<double>> polyExt = new List<IList<double>>();

                                                                                    if (arbitraryClosedProfiles.OuterCurve.StepClassName == "IfcIndexedPolyCurve")
                                                                                    {
                                                                                        IfcIndexedPolyCurve outerCurve = arbitraryClosedProfiles.OuterCurve as IfcIndexedPolyCurve;
                                                                                        IfcCartesianPointList2D points = outerCurve.Points as IfcCartesianPointList2D;
                                                                                        foreach (double[] pts in points.CoordList)
                                                                                        {
                                                                                            if (pts.Length >= 2)
                                                                                            {
                                                                                                try
                                                                                                {
                                                                                                    IList<double> xy = new List<double>();
                                                                                                    xy.Add(pts[0] / context.SCALE);
                                                                                                    xy.Add(pts[1] / context.SCALE);
                                                                                                    polyExt.Add(xy);
                                                                                                }
                                                                                                catch (System.Exception exTransf)
                                                                                                {
                                                                                                    Console.WriteLine("6." + exTransf.Message);
                                                                                                }

                                                                                            }

                                                                                        }

                                                                                    }
                                                                                    else
                                                                                    {

                                                                                        List<IfcPolyline> poly = arbitraryClosedProfiles.OuterCurve.Extract<IfcPolyline>();

                                                                                        if (poly.Count > 0 && poly[0].Points.Count > 0)
                                                                                        {
                                                                                            foreach (IfcCartesianPoint pt in poly[0].Points)
                                                                                            {

                                                                                                if (pt.Coordinates.Count >= 2)
                                                                                                {
                                                                                                    try
                                                                                                    {
                                                                                                        IList<double> xy = new List<double>();
                                                                                                        xy.Add(pt.Coordinates[0] / context.SCALE);
                                                                                                        xy.Add(pt.Coordinates[1] / context.SCALE);
                                                                                                        polyExt.Add(xy);
                                                                                                    }
                                                                                                    catch (System.Exception exTransf)
                                                                                                    {
                                                                                                        Console.WriteLine("7. " + exTransf.Message);
                                                                                                    }

                                                                                                }
                                                                                            }

                                                                                        }

                                                                                    }


                                                                                    coords.Add(polyExt);

                                                                                    props.Add("location", pos.Location.Coordinates[0] / context.SCALE + "," + pos.Location.Coordinates[1] / context.SCALE + "," + pos.Location.Coordinates[2] / context.SCALE);
                                                                                    if (pos.RefDirection != null) props.Add("refDirection", pos.RefDirection.DirectionRatios[0] + "," + pos.RefDirection.DirectionRatios[1] + "," + pos.RefDirection.DirectionRatios[2]);
                                                                                    if (pos.Axis != null) props.Add("axis", pos.Axis.DirectionRatios[0] + "," + pos.Axis.DirectionRatios[1] + "," + pos.Axis.DirectionRatios[2]);


                                                                                }
                                                                                else if (areaSolid.SweptArea.StepClassName == "IfcRectangleProfileDef") // Rectangle
                                                                                {
                                                                                    List<IfcRectangleProfileDef> rectangleProfile = areaSolid.SweptArea.Extract<IfcRectangleProfileDef>();

                                                                                    if (rectangleProfile.Count > 0)
                                                                                    {
                                                                                        if (rectangleProfile[0].XDim > 0.0000001 && rectangleProfile[0].YDim > 0.0000001)
                                                                                        {

                                                                                            if (rectangleProfile[0].Position.Location.Coordinates.Count >= 2)
                                                                                            {
                                                                                                try
                                                                                                {
                                                                                                    Point3D lm = new Point3D(0, 0, 0);
                                                                                                    double XDim = rectangleProfile[0].XDim / 2;
                                                                                                    double YDim = rectangleProfile[0].YDim / 2;

                                                                                                    // Left-Bottom
                                                                                                    IList<double> lb = new List<double>();
                                                                                                    Point3D lbP = new Point3D(lm.X - XDim, lm.Y - YDim, lm.Z);
                                                                                                    lb.Add(lbP.X / context.SCALE);
                                                                                                    lb.Add(lbP.Y / context.SCALE);
                                                                                                    // right-Bottom
                                                                                                    IList<double> rb = new List<double>();
                                                                                                    Point3D rbP = new Point3D(lm.X + XDim, lm.Y - YDim, lm.Z);
                                                                                                    rb.Add(rbP.X / context.SCALE);
                                                                                                    rb.Add(rbP.Y / context.SCALE);
                                                                                                    // right-top
                                                                                                    IList<double> rt = new List<double>();
                                                                                                    Point3D rtP = new Point3D(lm.X + XDim, lm.Y + YDim, lm.Z);
                                                                                                    rt.Add(rtP.X / context.SCALE);
                                                                                                    rt.Add(rtP.Y / context.SCALE);
                                                                                                    // left-top
                                                                                                    IList<double> lt = new List<double>();
                                                                                                    Point3D ltP = new Point3D(lm.X - XDim, lm.Y + YDim, lm.Z);
                                                                                                    lt.Add(ltP.X / context.SCALE);
                                                                                                    lt.Add(ltP.Y / context.SCALE);

                                                                                                    IList<IList<double>> polyExt = new List<IList<double>>();
                                                                                                    polyExt.Add(lb);
                                                                                                    polyExt.Add(rb);
                                                                                                    polyExt.Add(rt);
                                                                                                    polyExt.Add(lt);
                                                                                                    polyExt.Add(lb);
                                                                                                    coords.Add(polyExt);
                                                                                                    props.Add("location", pos.Location.Coordinates[0] / context.SCALE + "," + pos.Location.Coordinates[1] / context.SCALE + "," + pos.Location.Coordinates[2] / context.SCALE);
                                                                                                    if (pos.RefDirection != null) props.Add("refDirection", pos.RefDirection.DirectionRatios[0] + "," + pos.RefDirection.DirectionRatios[1] + "," + pos.RefDirection.DirectionRatios[2]);
                                                                                                    if (pos.Axis != null) props.Add("axis", pos.Axis.DirectionRatios[0] + "," + pos.Axis.DirectionRatios[1] + "," + pos.Axis.DirectionRatios[2]);

                                                                                                }
                                                                                                catch (System.Exception exMatrixTransf)
                                                                                                {
                                                                                                    Console.WriteLine("8. " + exMatrixTransf.Message);
                                                                                                }


                                                                                            }
                                                                                        }
                                                                                    }

                                                                                }
                                                                                else if (areaSolid.SweptArea.StepClassName == "IfcArbitraryProfileDefWithVoids") // 
                                                                                {
                                                                                    // OuterCurve [IfcCurve]
                                                                                    IfcArbitraryProfileDefWithVoids arbitraryProfileDefWithVoids = areaSolid.SweptArea as IfcArbitraryProfileDefWithVoids;
                                                                                    IfcArbitraryClosedProfileDef arbitraryClosedProfiles = areaSolid.SweptArea as IfcArbitraryClosedProfileDef;
                                                                                    IList<IList<double>> polyExt = new List<IList<double>>();

                                                                                    if (arbitraryProfileDefWithVoids.OuterCurve.StepClassName == "IfcIndexedPolyCurve")
                                                                                    {
                                                                                        IfcIndexedPolyCurve outerCurve = arbitraryClosedProfiles.OuterCurve as IfcIndexedPolyCurve;
                                                                                        IfcCartesianPointList2D points = outerCurve.Points as IfcCartesianPointList2D;
                                                                                        foreach (double[] pts in points.CoordList)
                                                                                        {
                                                                                            if (pts.Length >= 2)
                                                                                            {
                                                                                                try
                                                                                                {
                                                                                                    IList<double> xy = new List<double>();
                                                                                                    xy.Add(pts[0] / context.SCALE);
                                                                                                    xy.Add(pts[1] / context.SCALE);
                                                                                                    polyExt.Add(xy);
                                                                                                }
                                                                                                catch (System.Exception exTransf)
                                                                                                {
                                                                                                    Console.WriteLine("9. " + exTransf.Message);
                                                                                                }

                                                                                            }

                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        List<IfcPolyline> poly = arbitraryProfileDefWithVoids.OuterCurve.Extract<IfcPolyline>();

                                                                                        foreach (IfcCartesianPoint pt in poly[0].Points)
                                                                                        {

                                                                                            if (pt.Coordinates.Count >= 2)
                                                                                            {
                                                                                                try
                                                                                                {
                                                                                                    IList<double> xy = new List<double>();
                                                                                                    Point3D p = new Point3D(pt.Coordinates[0], pt.Coordinates[1], 0);
                                                                                                    xy.Add(p.X / context.SCALE);
                                                                                                    xy.Add(p.Y / context.SCALE);
                                                                                                    polyExt.Add(xy);
                                                                                                }
                                                                                                catch (System.Exception exTransf)
                                                                                                {
                                                                                                    Console.WriteLine("10. " + exTransf.Message);
                                                                                                }

                                                                                            }
                                                                                        }
                                                                                    }

                                                                                    coords.Add(polyExt);

                                                                                    props.Add("location", pos.Location.Coordinates[0] / context.SCALE + "," + pos.Location.Coordinates[1] / context.SCALE + "," + pos.Location.Coordinates[2] / context.SCALE);
                                                                                    if (pos.RefDirection != null) props.Add("refDirection", pos.RefDirection.DirectionRatios[0] + "," + pos.RefDirection.DirectionRatios[1] + "," + pos.RefDirection.DirectionRatios[2]);
                                                                                    if (pos.Axis != null) props.Add("axis", pos.Axis.DirectionRatios[0] + "," + pos.Axis.DirectionRatios[1] + "," + pos.Axis.DirectionRatios[2]);

                                                                                }

                                                                            }
                                                                            else if (item.StepClassName == "IfcFacetedBrep-XXX")  // TODO : Fix export 3D Object
                                                                            {                                                     // https://standards.buildingsmart.org/IFC/RELEASE/IFC4_1/FINAL/HTML/schema/ifcgeometricmodelresource/lexical/ifcfacetedbrep.htm

                                                                                List<IfcFacetedBrep> facetedBreps = item.Extract<IfcFacetedBrep>();
                                                                                if (facetedBreps.Count > 0)
                                                                                {
                                                                                    IfcFacetedBrep facetedBrep = facetedBreps[0];
                                                                                    elevation = (buildingStorey.Elevation / context.SCALE).ToString();
                                                                                    if (facetedBrep.Outer.StepClassName == "IfcClosedShell")
                                                                                    {

                                                                                        if (facetedBrep.Outer.CfsFaces.Count > 0) // 
                                                                                        {
                                                                                            // CfsFaces[].Bounds[IfcFaceBound].Bound.Polgon[IfcCartesianPoint].Coordinates[3]
                                                                                            // OuterCurve [IfcCurve]
                                                                                            foreach (IfcFace cfsFace in facetedBrep.Outer.CfsFaces)
                                                                                            {
                                                                                                foreach (IfcFaceBound faceBound in cfsFace.Bounds)
                                                                                                {
                                                                                                    IList<IList<double>> polyExt = new List<IList<double>>();
                                                                                                    if (faceBound.Bound.StepClassName == "IfcPolyLoop")
                                                                                                    {
                                                                                                        IfcPolyLoop polyLoop = faceBound.Bound as IfcPolyLoop;
                                                                                                        foreach (IfcCartesianPoint pt in polyLoop.Polygon)
                                                                                                        {
                                                                                                            IList<double> xy = new List<double>();
                                                                                                            xy.Add(pt.Coordinates[0] / context.SCALE); //+ loc.X);
                                                                                                            xy.Add(pt.Coordinates[1] / context.SCALE); // + loc.Y);
                                                                                                            xy.Add(pt.Coordinates[2] / context.SCALE); // + loc.YZ;
                                                                                                            polyExt.Add(xy);
                                                                                                        }
                                                                                                    }

                                                                                                    // ERREUR OBJET 3D
                                                                                                    // TODO : Fix export 3D Object
                                                                                                    // coords.Add(polyExt);
                                                                                                }

                                                                                            }


                                                                                        }


                                                                                    }
                                                                                }

                                                                            }

                                                                        }
                                                                        catch (System.Exception exRepresentationItem)
                                                                        {
                                                                            Console.WriteLine("11. Element read error exRepresentationItem" + exRepresentationItem.Message);
                                                                            returnMessage = (int)ExitCode.UnknownError;
                                                                        }

                                                                    }


                                                                }

                                                                if (coords.Count == 0)
                                                                {
                                                                    // Console.WriteLine("12. " + coords.Count);
                                                                }

                                                                props.Add("height", height);
                                                                props.Add("elevation", elevation);

                                                                geom.type = "Polygon";
                                                                geom.coordinates = coords;

                                                                newElementProd.boundary = new geoFeature();
                                                                newElementProd.boundary.type = "Feature";
                                                                newElementProd.boundary.id = null;
                                                                newElementProd.boundary.properties = props;
                                                                newElementProd.boundary.geometry = geom;
                                                            }
                                                            var builingElements = space.Extract<IfcBuildingElementProxy>();
                                                            // IFC Elements
                                                            foreach (var bElement in builingElements)
                                                            {
                                                                IfcRelContainedInSpatialStructure productIds = bElement.ContainedInStructure;
                                                                foreach (IfcProduct pId in productIds.RelatedElements)
                                                                {
                                                                    try
                                                                    {
                                                                        if (pId.GlobalId == product.GlobalId)
                                                                        {
                                                                            newElementProd.userData.spaceId = space.GlobalId;
                                                                        }
                                                                    }
                                                                    catch (System.Exception ex)
                                                                    {
                                                                        Console.WriteLine("13. Element read error" + ex.Message);
                                                                        returnMessage = (int)ExitCode.UnknownError;
                                                                    }
                                                                }
                                                            }


                                                        }

                                                        catch (Exception ex)
                                                        {
                                                            Console.WriteLine("16. Element read error" + ex.Message);
                                                            returnMessage = (int)ExitCode.UnknownError;
                                                        }

                                                        spaceCounter += 1;
                                                    }

                                                    // Add to list
                                                    productsIds.Add(newElementProd.id);

                                                    if (newElementProd.userData.type != "IfcBuildingStorey")
                                                    {
                                                        outputElements.Add(newElementProd);
                                                    }
                                                    else
                                                    {
                                                        // Console.WriteLine("14. Error IfcBuildingStorey");
                                                    }

                                                }

                                            }
                                            catch (NotSupportedException exEncode)
                                            {
                                                Console.WriteLine("28. Name read error (product counter: " + productCounter + ") " + exEncode.Message); // returnMessage = (int)ExitCode.NodataIsAvailableForEncoding;
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine("29. Element read error" + ex.Message);
                                                returnMessage = (int)ExitCode.UnknownError;
                                            }

                                            productCounter += 1;
                                        }



                                        // IFC Elements

                                        var elements = buildingStorey.Extract<IfcBuildingElementProxy>();
                                        foreach (IfcBuildingElementProxy element in elements
                                            .Where(element => element.GlobalId != null && !productsIds.Contains(element.GlobalId)))
                                        {
                                            try
                                            {
                                                // Add to list
                                                outputElements.Add(new JsonBuildingElementProxyIfcElement(element, storeyElement, context));

                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine("17. Element read error" + ex.Message);
                                                returnMessage = (int)ExitCode.UnknownError;
                                            }

                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("18. Element read error" + ex.Message);
                            returnMessage = (int)ExitCode.UnknownError;
                        }

                    }



                    // Json Settings
                    Newtonsoft.Json.JsonSerializerSettings jsonSettings = new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };

                    if (activeComptactJson)
                    {


                        if (activeFullJson)
                        {
                            // Original File
                            string jsonFormat = Newtonsoft.Json.JsonConvert.SerializeObject(db.JSON(), Newtonsoft.Json.Formatting.None, jsonSettings);
                            File.WriteAllText(pathDest, jsonFormat);
                        }
                        else
                        {
                            string jsonFormat = Newtonsoft.Json.JsonConvert.SerializeObject(outputElements, Newtonsoft.Json.Formatting.None, jsonSettings);
                            File.WriteAllText(pathDest, jsonFormat);
                        }
                    }
                    else
                    {
                        if (activeFullJson)
                        {
                            // Original File
                            db.WriteFile(pathDest);
                        }
                        else
                        {
                            string jsonFormat = Newtonsoft.Json.JsonConvert.SerializeObject(outputElements, Newtonsoft.Json.Formatting.Indented, jsonSettings);
                            File.WriteAllText(pathDest, jsonFormat);
                        }
                    }

                }
                else
                {
                    returnMessage = (int)ExitCode.InvalidFilename;
                }

            }
            catch (Exception ioEx)
            {
                Console.WriteLine("19. " + ioEx.Message);
                returnMessage = (int)ExitCode.InvalidFile;
            }

            Console.WriteLine("20. " + pathDest);
            return returnMessage;
        }



        enum ExitCode : int
        {
            Success = 0,
            InvalidFile = 1,
            InvalidFilename = 2,
            NodataIsAvailableForEncoding = 3,
            UnknownError = 10,
        }

        // GeoJson Export
    }
}

