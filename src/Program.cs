﻿using System;
using System.Collections.Generic;
using System.IO;
using GeometryGym.Ifc;
using MathNet.Spatial.Euclidean;
using MathNet.Numerics;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;

namespace ConvertIfc2Json
{
    class Program
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
            var SCALE = 1.0;

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
                            var newProject = new JsonIfcElement();
                            if (project.GlobalId != null)
                            {
                                // REVIEW ne semble pas servir
                                //foreach (var unit in project.UnitsInContext.Units)
                                //{
                                //    var u = project.UnitsInContext.Extract<IfcSIUnit>();
                                //    // Console.WriteLine("2." + unit.StepClassName);
                                //}

                                // Computing the json conversion scale from current Geogym project
                                SCALE = project.GetJsonConversionScale();

                                newProject.id = project.GlobalId;

                                projectId = newProject.id;
                                newProject.userData = new JsonIfcUserData();
                                newProject.userData.pset = new Dictionary<string, string>();
                                if (project.Name != null) newProject.userData.name = project.Name;
                                if (project.ObjectType != null) newProject.userData.objectType = project.ObjectType;
                                if (project.StepClassName != null) newProject.userData.type = project.StepClassName;

                                outputElements.Add(newProject);

                            }
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
                            var newSite = new JsonIfcElement();
                            if (site.GlobalId != null)
                            {
                                newSite.id = site.GlobalId;
                                newSite.userData = new JsonIfcUserData();
                                newSite.userData.projectId = projectId;
                                newSite.userData.pset = new Dictionary<string, string>();
                                if (site.Name != null) newSite.userData.name = site.Name;
                                if (site.ObjectType != null) newSite.userData.objectType = site.ObjectType;
                                if (site.StepClassName != null) newSite.userData.type = site.StepClassName;

                                if (site.RefLatitude != null) if (newSite.userData.pset.ContainsKey("RefLatitude") != true)
                                    {
                                        IfcCompoundPlaneAngleMeasure Lat = site.RefLatitude;
                                        newSite.userData.pset.Add("RefLatitude", Lat.ToSTEP().ToString());
                                    }

                                if (site.RefLongitude != null) if (newSite.userData.pset.ContainsKey("RefLongitude") != true)
                                    {
                                        IfcCompoundPlaneAngleMeasure Long = site.RefLongitude;
                                        newSite.userData.pset.Add("RefLongitude", Long.ToSTEP().ToString());
                                    }

                                if (site.RefElevation != 0) if (newSite.userData.pset.ContainsKey("RefElevation") != true)
                                        newSite.userData.pset.Add("RefElevation", Convert.ToString(site.RefElevation));

                                // Extract Pset
                                try
                                {
                                    extractPset(ref newSite, site);
                                }
                                catch (System.Exception ex)
                                {
                                    Console.WriteLine("4. Site pset error : " + ex.Message);
                                }

                                // Add Matrix
                                var sObjectPlacements = site.ObjectPlacement.Extract<IfcObjectPlacement>();
                                var sLocalPlacements = sObjectPlacements[0].Extract<IfcLocalPlacement>();
                                var sPos = sLocalPlacements[0].RelativePlacement as IfcAxis2Placement3D;
                                if (sPos.Location != null) newSite.userData.location = sPos.Location.Coordinates[0] / SCALE + "," + sPos.Location.Coordinates[1] / SCALE + "," + sPos.Location.Coordinates[2] / SCALE;
                                if (sPos.RefDirection != null) newSite.userData.refDirection = sPos.RefDirection.DirectionRatios[0] + "," + sPos.RefDirection.DirectionRatios[1] + "," + sPos.RefDirection.DirectionRatios[2];
                                if (sPos.Axis != null) newSite.userData.axis = sPos.Axis.DirectionRatios[0] + "," + sPos.Axis.DirectionRatios[1] + "," + sPos.Axis.DirectionRatios[2];

                                outputElements.Add(newSite);

                                // IFC Building
                                buildings = site.Extract<IfcBuilding>();
                                foreach (var building in buildings)
                                {
                                    var newBuildind = new JsonIfcElement();
                                    if (building.GlobalId != null)
                                    {
                                        newBuildind.id = building.GlobalId;
                                        newBuildind.userData = new JsonIfcUserData();
                                        newBuildind.userData.projectId = projectId;
                                        newBuildind.userData.siteId = site.GlobalId;
                                        newBuildind.userData.pset = new Dictionary<string, string>();
                                        if (building.Name != null) newBuildind.userData.name = building.Name;
                                        if (building.ObjectType != null) newBuildind.userData.objectType = building.ObjectType;
                                        if (building.StepClassName != null) newBuildind.userData.type = building.StepClassName;


                                        // Add Matrix
                                        var bObjectPlacements = building.ObjectPlacement.Extract<IfcObjectPlacement>();
                                        var bLocalPlacements = bObjectPlacements[0].Extract<IfcLocalPlacement>();
                                        IfcAxis2Placement3D bPos = bLocalPlacements[0].RelativePlacement as IfcAxis2Placement3D;
                                        if (bPos.Location != null) newBuildind.userData.location = bPos.Location.Coordinates[0] / SCALE + "," + bPos.Location.Coordinates[1] / SCALE + "," + bPos.Location.Coordinates[2] / SCALE;
                                        if (bPos.RefDirection != null) newBuildind.userData.refDirection = bPos.RefDirection.DirectionRatios[0] + "," + bPos.RefDirection.DirectionRatios[1] + "," + bPos.RefDirection.DirectionRatios[2];
                                        if (bPos.Axis != null) newBuildind.userData.axis = bPos.Axis.DirectionRatios[0] + "," + bPos.Axis.DirectionRatios[1] + "," + bPos.Axis.DirectionRatios[2];


                                        // Extract Pset
                                        extractPset(ref newBuildind, building);

                                        // building Address
                                        try
                                        {
                                            if (building.BuildingAddress != null)
                                            {
                                                if (building.BuildingAddress.AddressLines.Count > 0)
                                                {
                                                    for (int i = 0; i < building.BuildingAddress.AddressLines.Count; i++)
                                                    {
                                                        double index = i + 1;
                                                        newBuildind.userData.pset.Add("AddressLine" + index.ToString(), building.BuildingAddress.AddressLines[i]);
                                                    }
                                                }
                                                if (building.BuildingAddress.PostalBox != "") newBuildind.userData.pset.Add("PostalBox", building.BuildingAddress.PostalBox);
                                                if (building.BuildingAddress.PostalCode != "") newBuildind.userData.pset.Add("PostalCode", building.BuildingAddress.PostalCode);
                                                if (building.BuildingAddress.Town != "") newBuildind.userData.pset.Add("Town", building.BuildingAddress.Town);
                                                if (building.BuildingAddress.Region != "") newBuildind.userData.pset.Add("Region", building.BuildingAddress.Region);
                                                if (building.BuildingAddress.Country != "") newBuildind.userData.pset.Add("Country", building.BuildingAddress.Country);
                                            }

                                        }
                                        catch (System.Exception ex)
                                        {
                                            Console.WriteLine("5. Buildind Adresse (id: " + newBuildind.id + ") : " + ex.Message);
                                        }


                                        outputElements.Add(newBuildind);
                                    }

                                    // IFC Building Storey // Levels
                                    List<IfcBuildingStorey> buildingStoreys = building.Extract<IfcBuildingStorey>();
                                    foreach (IfcBuildingStorey buildingStorey in buildingStoreys)
                                    {
                                        var storeyElement = new JsonIfcElement();
                                        storeyElement.id = buildingStorey.GlobalId;
                                        storeyElement.userData = new JsonIfcUserData();
                                        storeyElement.userData.projectId = projectId;
                                        storeyElement.userData.siteId = site.GlobalId;
                                        storeyElement.userData.buildingId = building.GlobalId;
                                        // storeyElement.userData.objectType = buildingStorey.objectType;
                                        storeyElement.userData.type = "IfcBuildingStorey";
                                        storeyElement.userData.name = buildingStorey.LongName;
                                        storeyElement.userData.pset = new Dictionary<string, string>();
                                        // Extract Pset
                                        // extractPset(ref storeyElement, buildingStorey);
                                        if (storeyElement.userData.pset.ContainsKey("Elevation") != true) storeyElement.userData.pset.Add("Elevation", (buildingStorey.Elevation / SCALE).ToString());

                                        // Add Matrix
                                        List<IfcObjectPlacement> bsObjectPlacements = buildingStorey.ObjectPlacement.Extract<IfcObjectPlacement>();
                                        List<IfcLocalPlacement> bsLocalPlacements = bsObjectPlacements[0].Extract<IfcLocalPlacement>();
                                        IfcAxis2Placement3D bsPos = bsLocalPlacements[0].RelativePlacement as IfcAxis2Placement3D;
                                        if (bsPos.Location != null) newBuildind.userData.location = bsPos.Location.Coordinates[0] / SCALE + "," + bsPos.Location.Coordinates[1] / SCALE + "," + bsPos.Location.Coordinates[2] / SCALE;
                                        if (bsPos.RefDirection != null) newBuildind.userData.refDirection = bsPos.RefDirection.DirectionRatios[0] + "," + bsPos.RefDirection.DirectionRatios[1] + "," + bsPos.RefDirection.DirectionRatios[2];
                                        if (bsPos.Axis != null) newBuildind.userData.axis = bsPos.Axis.DirectionRatios[0] + "," + bsPos.Axis.DirectionRatios[1] + "," + bsPos.Axis.DirectionRatios[2];

                                        outputElements.Add(storeyElement);


                                        // IFC Space // Rooms
                                        List<IfcSpace> spaces = buildingStorey.Extract<IfcSpace>();

                                        // Check IfcProduct Ids
                                        List<string> productsIds = new List<string>();
                                        double productCounter = 0;

                                        // IfcProduct
                                        List<IfcProduct> products = buildingStorey.Extract<IfcProduct>();
                                        foreach (IfcProduct product in products)
                                        {
                                            JsonIfcElement newElementProd = new JsonIfcElement();
                                            try
                                            {
                                                if (product.GlobalId != null)
                                                {
                                                    newElementProd.id = product.GlobalId;
                                                    newElementProd.userData = new JsonIfcUserData();
                                                    newElementProd.userData.buildingStorey = new string[] { };
                                                    newElementProd.userData.pset = new Dictionary<string, string>();
                                                    if (product.Name != null) newElementProd.userData.name = product.Name; // product.LongName;
                                                    if (product.ObjectType != null) newElementProd.userData.objectType = product.ObjectType;
                                                    // if (product.Tag != null) newElement.userData.tag = product.Tag;
                                                    if (product.StepClassName != null) newElementProd.userData.type = product.StepClassName;

                                                    // Environnement element
                                                    if (projectId != null) newElementProd.userData.projectId = projectId;
                                                    if (site.GlobalId != null) newElementProd.userData.siteId = site.GlobalId;
                                                    if (building.GlobalId != null) newElementProd.userData.buildingId = building.GlobalId;
                                                    List<string> sIds = new List<string>();
                                                    sIds.Add(storeyElement.id);
                                                    newElementProd.userData.buildingStorey = sIds.ToArray();

                                                    // Extract pset
                                                    extractPset(ref newElementProd, product);
                                                    double spaceCounter = 0;

                                                    // Link to the Space
                                                    foreach (IfcSpace space in spaces)
                                                    {

                                                        try
                                                        {

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
                                                                                height = (areaSolid.Depth / SCALE).ToString();
                                                                                elevation = (buildingStorey.Elevation / SCALE).ToString();

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
                                                                                                    xy.Add(pts[0] / SCALE);
                                                                                                    xy.Add(pts[1] / SCALE);
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
                                                                                                        xy.Add(pt.Coordinates[0] / SCALE);
                                                                                                        xy.Add(pt.Coordinates[1] / SCALE);
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

                                                                                    props.Add("location", pos.Location.Coordinates[0] / SCALE + "," + pos.Location.Coordinates[1] / SCALE + "," + pos.Location.Coordinates[2] / SCALE);
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
                                                                                                    lb.Add(lbP.X / SCALE);
                                                                                                    lb.Add(lbP.Y / SCALE);
                                                                                                    // right-Bottom
                                                                                                    IList<double> rb = new List<double>();
                                                                                                    Point3D rbP = new Point3D(lm.X + XDim, lm.Y - YDim, lm.Z);
                                                                                                    rb.Add(rbP.X / SCALE);
                                                                                                    rb.Add(rbP.Y / SCALE);
                                                                                                    // right-top
                                                                                                    IList<double> rt = new List<double>();
                                                                                                    Point3D rtP = new Point3D(lm.X + XDim, lm.Y + YDim, lm.Z);
                                                                                                    rt.Add(rtP.X / SCALE);
                                                                                                    rt.Add(rtP.Y / SCALE);
                                                                                                    // left-top
                                                                                                    IList<double> lt = new List<double>();
                                                                                                    Point3D ltP = new Point3D(lm.X - XDim, lm.Y + YDim, lm.Z);
                                                                                                    lt.Add(ltP.X / SCALE);
                                                                                                    lt.Add(ltP.Y / SCALE);

                                                                                                    IList<IList<double>> polyExt = new List<IList<double>>();
                                                                                                    polyExt.Add(lb);
                                                                                                    polyExt.Add(rb);
                                                                                                    polyExt.Add(rt);
                                                                                                    polyExt.Add(lt);
                                                                                                    polyExt.Add(lb);
                                                                                                    coords.Add(polyExt);
                                                                                                    props.Add("location", pos.Location.Coordinates[0] / SCALE + "," + pos.Location.Coordinates[1] / SCALE + "," + pos.Location.Coordinates[2] / SCALE);
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
                                                                                                    xy.Add(pts[0] / SCALE);
                                                                                                    xy.Add(pts[1] / SCALE);
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
                                                                                                    xy.Add(p.X / SCALE);
                                                                                                    xy.Add(p.Y / SCALE);
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

                                                                                    props.Add("location", pos.Location.Coordinates[0] / SCALE + "," + pos.Location.Coordinates[1] / SCALE + "," + pos.Location.Coordinates[2] / SCALE);
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
                                                                                    elevation = (buildingStorey.Elevation / SCALE).ToString();
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
                                                                                                            xy.Add(pt.Coordinates[0] / SCALE); //+ loc.X);
                                                                                                            xy.Add(pt.Coordinates[1] / SCALE); // + loc.Y);
                                                                                                            xy.Add(pt.Coordinates[2] / SCALE); // + loc.YZ;
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
                                                            List<IfcBuildingElementProxy> builingElements = space.Extract<IfcBuildingElementProxy>();
                                                            // IFC Elements
                                                            foreach (IfcBuildingElementProxy bElement in builingElements)
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

                                                        catch (System.Exception ex)
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
                                            catch (System.Exception ex)
                                            {
                                                Console.WriteLine("29. Element read error" + ex.Message);
                                                returnMessage = (int)ExitCode.UnknownError;
                                            }

                                            productCounter += 1;
                                        }



                                        // IFC Elements

                                        List<IfcBuildingElementProxy> elements = buildingStorey.Extract<IfcBuildingElementProxy>();

                                        foreach (IfcBuildingElementProxy element in elements)
                                        {
                                            JsonIfcElement newElement = new JsonIfcElement();
                                            try
                                            {
                                                if (element.GlobalId != null && productsIds.Contains(element.GlobalId) == false)
                                                {
                                                    newElement.id = element.GlobalId;
                                                    newElement.userData = new JsonIfcUserData();
                                                    newElement.userData.buildingStorey = new string[] { };
                                                    newElement.userData.pset = new Dictionary<string, string>();
                                                    if (element.Name != null) newElement.userData.name = element.Name;
                                                    if (element.ObjectType != null) newElement.userData.objectType = element.ObjectType;
                                                    if (element.Tag != null) newElement.userData.tag = element.Tag;
                                                    if (element.StepClassName != null) newElement.userData.type = element.StepClassName;

                                                    // Environnement element
                                                    if (projectId != null) newElement.userData.projectId = projectId;
                                                    if (site.GlobalId != null) newElement.userData.siteId = site.GlobalId;
                                                    if (building.GlobalId != null) newElement.userData.buildingId = building.GlobalId;
                                                    List<string> sIds = new List<string>();
                                                    sIds.Add(storeyElement.id);
                                                    newElement.userData.buildingStorey = sIds.ToArray();


                                                    // Extract pset
                                                    extractPset(ref newElement, element);

                                                    // Add to list
                                                    outputElements.Add(newElement);


                                                }

                                            }
                                            catch (System.Exception ex)
                                            {
                                                Console.WriteLine("17. Element read error" + ex.Message);
                                                returnMessage = (int)ExitCode.UnknownError;
                                            }

                                        }
                                    }
                                }
                            }
                        }
                        catch (System.Exception ex)
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

        public static void extractPsetBase(this IfcObject element, JsonIfcElement newElement, string logId = null)
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
                        if(logId != null) Console.WriteLine($"{logId}. Pset write error {e.Message}");
                    }

                }

            }

        }

        static void extractPset(ref JsonIfcElement newElement, IfcSite element) => extractPsetBase(element, newElement, logId: "21");
        static void extractPset(ref JsonIfcElement newElement, IfcProduct element) => extractPsetBase(element, newElement);
        static void extractPset(ref JsonIfcElement newElement, IfcBuilding element) => extractPsetBase(element, newElement, logId: "23");
        static void extractPset(ref JsonIfcElement newElement, IfcBuildingElementProxy element) => extractPsetBase(element, newElement, logId: "24");
        static void extractPset(ref JsonIfcElement newElement, IfcBuildingStorey element) => extractPsetBase(element, newElement, logId: "25");

        enum ExitCode : int
        {
            Success = 0,
            InvalidFile = 1,
            InvalidFilename = 2,
            NodataIsAvailableForEncoding = 3,
            UnknownError = 10,
        }


        public class JsonIfcElement
        {
            public string id { get; set; }
            public JsonIfcUserData userData { get; set; }
            public geoFeature boundary { get; set; }
        }

        internal class JsonIfcUserData
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
            public Dictionary<string, string> pset { get; set; }
            public string location { get; set; }
            public string refDirection { get; set; }
            public string axis { get; set; }


        }

        // GeoJson Export
        public class GeoFeatureCollection
        {
            public string type { get; set; }
            public object crs { get; set; }
            public string name { get; set; }
            public bool exceededTransferLimit { get; set; }
            public IList<geoFeature> features { get; set; }
        }
        public class geoFeature
        {
            public string type { get; set; }
            public string id { get; set; } // Original :  int
            public geoGeometry geometry { get; set; }
            public Dictionary<string, string> properties { get; set; }
        }
        public class geoGeometry
        {
            public string type { get; set; }
            public object coordinates { get; set; }
        }

        public class geoPoint
        {
            public string type { get; set; }
            public IList<double> coordinates { get; set; }
        }
        public class geoMultiPoint
        {
            public string type { get; set; }
            public IList<IList<double>> coordinates { get; set; }
        }
        public class geoMultiLineString
        {
            public string type { get; set; }
            public IList<IList<IList<double>>> coordinates { get; set; }
        }



    }
}

