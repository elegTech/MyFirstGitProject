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
        public static double MESHGAPFLOOR = 0.5;
        public static double MESHSIZECEIL = 0.5;

        public static double THRESHHOLDVALUE = 0.1;

         
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

        
        /// <summary>
        /// Get all 3D vertices for each edge loop of a face due to possible holes in this face.
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        public static List<List<XYZ>> GetFaceVertex3D(Face face)
        {
            Contract.Requires(null != face);
            if (null == face)
                return null;

            // All curves in sequence should be obtained first.
            IList<CurveLoop> curveLoopList = face.GetEdgesAsCurveLoops();
            if (null == curveLoopList || curveLoopList.Count == 0)
                return null;

            List<List<XYZ>> vertexLists = new List<List<XYZ>>();
            List<XYZ> tempVertexList;
            CurveLoopIterator curveIterator;
            foreach (CurveLoop curves in curveLoopList)
            {
                // Traverse the next curve loop if the current one is illegal. However, some routines in Revit may 
                // set the CurveLoop to be marked "open" or "closed" in spite of the actual geometry of the curves.
                // In these special cases, the CurveLoop class does not require that the CurveLoop is correctly marked. 
                if (null == curves || curves.IsOpen())
                    continue;

                // Allocate new memory for the vertex list.
                tempVertexList = new List<XYZ>();

                curveIterator = curves.GetCurveLoopIterator(); 
                while (!curveIterator.MoveNext())
                {
                    tempVertexList.Add(curveIterator.Current.GetEndPoint(0));
                }
                vertexLists.Add(tempVertexList);
            }

            return vertexLists;
        }


        /// <summary>
        /// Get all 2D vertices for each edge loop of a face due to possible holes in this face.
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        public static List<List<UV>> GetFaceVertex2D(Face face)
        {
            Contract.Requires(null != face);
            if (null == face)
                return null;

            List<List<XYZ>> vertexLists3D = GetFaceVertex3D(face);
            if (null == vertexLists3D || vertexLists3D.Count == 0)
                return null;

            List<List<UV>> vertexLists2D = new List<List<UV>>(vertexLists3D.Count);

            foreach (List<XYZ> vertexList in vertexLists3D)
            {
                List<UV> tempUertexList = new List<UV>(vertexList.Count);
                for (int i=0; i<vertexList.Count; i++)
                {
                    tempUertexList.Add(face.Project(vertexList[i]).UVPoint);
                }
                vertexLists2D.Add(tempUertexList);
            }

            return vertexLists2D;
        }

    }
}
