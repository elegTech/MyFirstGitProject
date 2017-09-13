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
    /// <summary>
    /// It models a polygon area that will be paved. Currently, the
    /// area boundaries are line segments.
    /// </summary>
    class PolygonArea
    {
        /// <summary>
        /// A revit solid's face that will be paved by tiles.
        /// </summary>
        private Face mFace;

        /// <summary>
        /// Get the face vertice. Note that a face can have multiple
        /// edge loops when there is a hole in this face. Hence the 
        /// vertices are stored for individual edge. 
        /// </summary>
        private List<List<XYZ>> vertexLists;

        /// <summary>
        /// In addition to all vertices in vertexLists, it includes 
        /// intersect points this area boundaries with meshes.
        /// </summary>
        private List<List<XYZ>> pointLists;


        public PolygonArea(Face face)
        {
            mFace = face;
        }

        public Face Face
        {
            get
            {
                return mFace;
            }

            set
            {
                if (null != mFace)
                    mFace = value;
            }
        }

        public List<List<XYZ>> VertexLists
        {
            get
            {
                return vertexLists;
            }
        }

        private bool GetEdgeVertex()
        {
            Contract.Assert(null != mFace);
            if (null == mFace)
                return false;

            vertexLists = Utility.GetFaceVertex(mFace);
            if (null == vertexLists || vertexLists.Count == 0)
                return false;

            return true;
        }

        /// <summary>
        /// It hides internal logic to make the object easy to use.
        /// </summary>
        /// <returns></returns>
        public bool PrepareAreaBoundaryVertex()
        {
            return GetEdgeVertex();
        }

        /// <summary>
        /// Calculate all intersection points between area boundaries
        /// and given meshes. 
        /// </summary>
        /// <param name="meshArrays"></param>
        /// <returns></returns>
        public bool CalculateInsectionPoints(SquareMesh[,] meshArrays)
        {
            Contract.Assert(null != meshArrays && meshArrays.Length > 0);
            if (null == meshArrays || meshArrays.Length == 0)
                return false;

            pointLists = new List<List<XYZ>>();
            for (int i=0; i<vertexLists.Count; i++)
            {
                for (int j=0; j<vertexLists[i].Count; j++)
                {
                    vertexLists[i][j]


                }

            }


            return true;
        }
    }
}
