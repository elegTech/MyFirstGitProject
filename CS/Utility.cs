using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;


namespace ParameterUtils
{
    class Utility
    {
        public static double ZERO = 0.000000000000;

        /// <summary>
        /// The number of mesh vertex.
        /// </summary>
        public static int MESHVERTEXNUMBER = 4;

        /// <summary>
        /// If an area's dimension is less than this threshold value (unit: mm), no mesh would be generted.
        /// </summary>
        public static double DIMENSIONFLOOR = 10.000000000000;

        public static List<Solid> GetSolid(Element element, Options option)
        {
            Contract.Requires(null != element && null != option);

            GeometryElement geometryElement = element.get_Geometry(option);
            List<Solid> solidList = new List<Solid>();
            GeometryInstance geomInstance = null;

            foreach (GeometryObject geoObj in geometryElement)
            {
                geomInstance = geoObj as GeometryInstance;
                if (null == geomInstance)
                    continue;

                foreach (GeometryObject geom in geomInstance.SymbolGeometry)
                {
                    Solid solid = geom as Solid;
                    if (null == solid || 0 == solid.Faces.Size || 0 == solid.Edges.Size)
                    {
                        continue;
                    }
                    solidList.Add(solid);
                }
            }

            return solidList;
        }

        public static List<Face> GetFace(Solid solid)
        {
            Contract.Requires(null != solid);
            List<Face> faceList = new List<Face>();

            foreach (Face geomFace in solid.Faces)
            {
                if (geomFace.Area > 0)
                    faceList.Add(geomFace);
            }
            return null;
        }

        public static List<List<Curve>> GetFaceCurves(Face face)
        {
            Contract.Requires(null != face);
            if (null == face)
                return null;

            // A face may have holes, therefore it has corresponding inner edge loops.
            // Each loop and outer boundary need a seperate list to store curve information.
            List<List<Curve>> curveLists = new List<List<Curve>>();
            List<Curve> tempCurveList = new List<Curve>();

            EdgeArrayArray edgeArrays = face.EdgeLoops;
            Curve tempCurve = null;
            foreach (EdgeArray edges in edgeArrays)
            {
                foreach (Edge edge in edges)
                {
                    tempCurve = edge.AsCurveFollowingFace(face);
                    tempCurveList.Add(tempCurve);
                }
                curveLists.Add(tempCurveList);

                // Allocate new memory for the next edge list.
                tempCurveList = new List<Curve>();
            }

            return curveLists;
        }

        public static List<List<XYZ>> GetFaceVertex(Face face)
        {
            Contract.Requires(null != face);
            if (null == face)
                return null;

            // All edges should be obtained first.
            List<List<Curve>> curveLists = GetFaceCurves(face);
            if (null == curveLists)
                return null;

            List<List<XYZ>> vertexLists = new List<List<XYZ>>();
            List<XYZ> tempVertexList = new List<XYZ>();
            foreach (List<Curve> curves in curveLists)
            {
                foreach (Curve curve in curves)
                {
                    tempVertexList.Add(curve.GetEndPoint(0));
                    tempVertexList.Add(curve.GetEndPoint(1));
                }
                vertexLists.Add(tempVertexList);

                // Allocate new memory for the next vertex list.
                tempVertexList = new List<XYZ>();
            }

            return vertexLists;
        }
    }
}
