using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitToolKit.CollisionManager
{
    /// <summary>
    /// Defines different collision detection methods.
    /// </summary>
    public readonly struct Method
    {
        /// <summary>
        /// Represents the collision detection method based on intersection.
        /// </summary>
        public static readonly Method Intersection = new Method("intersection");

        /// <summary>
        /// Represents the collision detection method based on touching.
        /// </summary>
        public static readonly Method Touching = new Method("touching");

        /// <summary>
        /// Gets the string value of the collision detection method.
        /// </summary>
        public string Value { get; }

        private Method(string value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Provides methods to manage collision detection within a Revit project.
    /// </summary>
    public class Collision
    {
        private UIDocument UIDoc { get; }
        private bool Selection { get; set; }
        private IList<Element> List { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Collision"/> class with the specified UIDocument, list of elements, and selection option.
        /// </summary>
        /// <param name="uidoc">The UIDocument representing the current Revit document.</param>
        /// <param name="selection">A boolean value indicating whether to perform element selection during collision detection.</param>
        public Collision(UIDocument uidoc, bool selection = false)
        {
            UIDoc = uidoc;
            Selection = selection;
        }

        /// <summary>
        /// Performs collision detection based on the specified method and selects the resulting elements.
        /// </summary>
        /// <param name="method">The collision detection method. Possible methods are; <b>Intersection</b> or <b>Touching</b>.</param>
        /// <param name="list">A list of elements to perform collision detection on. If null, all non-element type elements in the document will be considered.</param>
        /// <param name="IncludeFurniture">A boolean value indicating whether to include furniture elements in the collision detection.</param>
        /// <returns>A HashSet of elements involved in the collision.</returns>
        public HashSet<Element> GetCollision(Method method, IList<Element> list = null, bool IncludeFurniture = false)
        {
            var initial = list is null ? new FilteredElementCollector(UIDoc.Document).WhereElementIsNotElementType().
            Where(x => x.GetType() != null && 
            (x.GetType() == typeof(Wall) ||
             x.GetType() == typeof(WallFoundation) ||
             x.GetType() == typeof(Floor) ||
             x.GetType() == typeof(Ceiling) ||
             x.GetType() == typeof(Railing) ||
             x.GetType() == typeof(ExtrusionRoof) ||
             x.GetType() == typeof(FootPrintRoof) ||
             x.GetType() == typeof(Stairs) ||
             x.GetType() == typeof(FamilyInstance) && 
                            (x.Category?.Name != "Working Drawings" && 
                             x.Category?.Name != "Site"))).ToList() : list;

            List = IncludeFurniture ? initial.Where(x => x.GetType() == typeof(FamilyInstance) && (x.Category?.Name != "Furniture")).ToList() : initial;

            var result = GetClash(method);

            if (Selection)
                UIDoc.Selection.SetElementIds(result.Select(x => x.Id).ToList());

            return result;
        }

        /// <summary>
        /// Performs collision detection based on the specified method and retrieves the intersecting elements.
        /// </summary>
        /// <param name="method">The collision detection method to use.</param>
        /// <returns>A list of elements involved in the collision based on the specified method.</returns>
        private HashSet<Element> GetClash(Method method)
        {
            HashSet<Element> clash = new HashSet<Element>();

            var list = ProcessElements(ProcessBoundingBoxes());

            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    Element a = list.ElementAt(i).Item1;
                    Element b = list.ElementAt(j).Item1;
                    if (clash.Contains(a) && clash.Contains(b))
                        continue;

                    var solid_a = list.ElementAt(i).Item2;
                    var solid_b = list.ElementAt(j).Item2;
                    if (solid_a is null || solid_b is null)
                        continue;

                    try
                    {
                        Solid unionSolid = BooleanOperationsUtils.ExecuteBooleanOperation(solid_a, solid_b, BooleanOperationsType.Union);

                        if (!CheckArea(solid_a, solid_b, unionSolid) || !CheckFaces(solid_a, solid_b, unionSolid))
                            continue;

                        Solid interSolid = BooleanOperationsUtils.ExecuteBooleanOperation(solid_a, solid_b, BooleanOperationsType.Intersect);

                        bool addToList;

                        switch (method.Value)
                        {
                            default:
                            case "intersection":
                                addToList = interSolid.Volume > 0.000001;
                                break;
                            case "touching":
                                addToList = interSolid.Volume < 0.000001;
                                break;
                        }

                        if (addToList)
                        {
                            clash.Add(a);
                            clash.Add(b);
                        }
                    }
                    catch { continue; }
                }
            }

            return clash;
        }

        /// <summary>
        /// Checks if the combined surface area of two solids is greater than the surface area of their union.
        /// </summary>
        /// <param name="solid_a">The first solid.</param>
        /// <param name="solid_b">The second solid.</param>
        /// <param name="unionSolid">The solid resulting from the union of solid_a and solid_b.</param>
        /// <returns>True if the combined surface area is greater than the surface area of the union; otherwise, false.</returns>
        private bool CheckArea(Solid solid_a, Solid solid_b, Solid unionSolid)
        {
            double sumArea = Math.Round(Math.Abs(solid_a.SurfaceArea + solid_b.SurfaceArea), 5);
            double unionArea = Math.Round(Math.Abs(unionSolid.SurfaceArea), 5);
            return sumArea > unionArea;
        }

        /// <summary>
        /// Checks if the combined number of faces of two solids is greater than the number of faces of their union.
        /// </summary>
        /// <param name="solid_a">The first solid.</param>
        /// <param name="solid_b">The second solid.</param>
        /// <param name="unionSolid">The solid resulting from the union of solid_a and solid_b.</param>
        /// <returns>True if the combined number of faces is greater than the number of faces of the union; otherwise, false.</returns>
        private bool CheckFaces(Solid solid_a, Solid solid_b, Solid unionSolid)
        {
            double sumFaces = Math.Abs(solid_a.Faces.Size + solid_b.Faces.Size);
            double unionFaces = Math.Abs(unionSolid.Faces.Size);
            return sumFaces > unionFaces;
        }

        /// <summary>
        /// Processes the bounding boxes of a list of elements to identify elements whose bounding boxes intersect or touch.
        /// </summary>
        /// <returns>A HashSet containing elements whose bounding boxes intersect or touch with other bounding boxes.</returns>
        private HashSet<Element> ProcessBoundingBoxes()
        {
            HashSet<Element> elements = new HashSet<Element>();

            for (int i = 0; i < List.Count; i++)
            {
                for (int j = i + 1; j < List.Count; j++)
                {
                    Element a = List[i];
                    Element b = List[j];
                    if (a is null || b is null || (elements.Contains(a) && elements.Contains(b)))
                        continue;

                    BoundingBoxXYZ bb1 = a.get_BoundingBox(null);
                    BoundingBoxXYZ bb2 = b?.get_BoundingBox(null);
                    if (bb1 is null || bb2 is null)
                        continue;

                    Outline outline1 = new Outline(bb1.Min, bb1.Max);
                    Outline outline2 = new Outline(bb2.Min, bb2.Max);
                    if (outline1 is null || outline2 is null)
                        continue;

                    if (outline1.Intersects(outline2, -0.5) || outline1.ContainsOtherOutline(outline2, 0.5))
                    {
                        elements.Add(a);
                        elements.Add(b);
                    }
                }
            }

            return elements;
        }

        /// <summary>
        /// Processes a HashSet of elements to retrieve their solid geometries.
        /// </summary>
        /// <param name="list">The HashSet of elements whose solid geometries are to be processed.</param>
        /// <returns>
        /// A HashSet of tuples, where each tuple contains an element and its corresponding solid geometry.
        /// </returns>
        private HashSet<Tuple<Element, Solid>> ProcessElements(HashSet<Element> list)
        {
            HashSet<Tuple<Element, Solid>> elements = new HashSet<Tuple<Element, Solid>>();

            foreach (var element in list)
            {
                var fullGeometry = element?.get_Geometry(new Options());
                if (fullGeometry is null)
                    continue;

                GetElementSolids(fullGeometry, element, elements);
            }

            return elements;
        }

        /// <summary>
        /// Recursively extracts solids from the geometry of an element or its instances.
        /// </summary>
        /// <param name="Geometry">The geometry of an element or its instance.</param>
        /// <param name="element">The element containing the geometry.</param>
        /// <param name="list">The list to which the extracted solids are added.</param>
        private void GetElementSolids(object Geometry, Element element, HashSet<Tuple<Element, Solid>> list)
        {

            GeometryElement parts = Geometry is GeometryElement ? Geometry as GeometryElement
                                   : Geometry is GeometryInstance gInstance ? gInstance.GetInstanceGeometry() : null;

            foreach (var geometryObject in parts)
            {
                if (geometryObject is Solid solid)
                {
                    var tuple = Tuple.Create(element, solid);
                    if (list.Contains(tuple))
                        continue;

                    list.Add(tuple);
                }
                else if (geometryObject is GeometryInstance instance)
                {
                    var instanceGeometry = instance.GetInstanceGeometry();
                    GetElementSolids(instanceGeometry, element, list);
                }
            }
        }
    }
}
