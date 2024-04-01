using Autodesk.Revit.DB;
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
        /// <param name="list">A list of elements to perform collision detection on. If null, all non-element type elements in the document will be considered.</param>
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
        /// <returns>A list of elements involved in the collision.</returns>
        public IList<Element> GetCollision(Method method, IList<Element> list = null)
        {
            List = list is null ? new FilteredElementCollector(UIDoc.Document).WhereElementIsNotElementType().ToElements() : list;
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
        private IList<Element> GetClash(Method method)
        {
            IList<Element> clash = new List<Element>();
            var list = ProcessElements();

            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < list.Count; j++)
                {
                    if (i == j)
                        continue;

                    var solid_a = list[i].Item2;
                    var solid_b = list[j].Item2;

                    Solid unionSolid = BooleanOperationsUtils.ExecuteBooleanOperation(solid_a, solid_b, BooleanOperationsType.Union);
                    Solid interSolid = BooleanOperationsUtils.ExecuteBooleanOperation(solid_a, solid_b, BooleanOperationsType.Intersect);

                    double sumArea = Math.Round(Math.Abs(solid_a.SurfaceArea + solid_b.SurfaceArea), 5);
                    double unionArea = Math.Round(Math.Abs(unionSolid.SurfaceArea), 5);

                    double sumFaces = Math.Abs(solid_a.Faces.Size + solid_b.Faces.Size);
                    double unionFaces = Math.Abs(unionSolid.Faces.Size);

                    bool addToList;

                    switch (method.Value)
                    {
                        default:
                        case "intersection":
                            addToList = sumArea > unionArea && sumFaces > unionFaces && interSolid.Volume > 0.00001;
                            break;
                        case "touching":
                            addToList = sumArea > unionArea && sumFaces > unionFaces && interSolid.Volume < 0.00001;
                            break;
                    }

                    if (addToList)
                    {
                        clash.Add(list[i].Item1);
                        clash.Add(list[j].Item1);
                    }
                }
            }

            return clash;
        }

        /// <summary>
        /// Processes all elements in the document and extracts their solids for collision detection.
        /// </summary>
        /// <returns>A list of tuples containing elements and their corresponding solids.</returns>
        private IList<Tuple<Element, Solid>> ProcessElements()
        {
            IList<Tuple<Element, Solid>> elements = new List<Tuple<Element, Solid>>();

            foreach (var element in List)
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
        private void GetElementSolids(object Geometry, Element element, IList<Tuple<Element, Solid>> list)
        {
            GeometryElement parts = Geometry is GeometryElement ? Geometry as GeometryElement
                                   : Geometry is GeometryInstance gInstance ? gInstance.GetInstanceGeometry() : null;

            foreach (var geometryObject in parts)
            {
                if (geometryObject is Solid solid)
                {
                    list.Add(Tuple.Create(element, solid));
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
