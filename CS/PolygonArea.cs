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

using MathNet.Spatial.Euclidean;


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
        /// Get the face curves vertices. Note that a face can have multiple
        /// edge loops when there is a hole in this face. Hence the 
        /// vertices are stored for individual edge. 
        /// </summary>
        private List<List<XYZ>> vertexLists3D;


        /// <summary>
        /// Face curves vertices defined using 2D points.
        /// </summary>
        private List<List<UV>> vertexLists2D;

        /// <summary>
        /// In addition to all vertices in vertexLists, it includes 
        /// intersection points of the area boundaries and meshes.
        /// </summary>
        private List<List<XYZ>> pointLists3D;


        private SqureMeshGenerator mMeshGenerator;

        private double mGap;

        private double mMeshLength;
        private double mMeshWidth;


        /// <summary>
        /// The boundary of the polygon area, represented by four vertices in counter-clockwise sequence.
        /// The first vertex is the bottom left one, codes as No.0. 
        /// </summary>
        private XYZ[] mBoundaryRectangle3D;


        /// <summary>
        /// Boundary rectangle defined using 2D points.
        /// </summary>
        private UV[] mBoundaryRectangle2D;


        public PolygonArea(Face face)
        {
            mFace = face;
            GenerateBoundary();
        }

        public PolygonArea(Face face, double gap, double meshLength, double meshWidth)
        {
            mFace = face;
            mGap = gap;
            mMeshLength = meshLength;
            mMeshWidth = meshWidth;
            GenerateBoundary();
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

        public double Gap
        {
            get
            {
                return mGap;
            }

            set
            {
                if (value >= Utility.ZERO)
                    mGap = value;
            }
        }


        public double MeshLength
        {
            get
            {
                return mMeshLength;
            }

            set
            {
                if (value > Utility.ZERO)
                    mMeshLength = value;
            }
        }

        public double MeshWidth
        {
            get
            {
                return mMeshWidth;
            }

            set
            {
                if (value > Utility.ZERO)
                    mMeshWidth = value;
            }
        }

        public XYZ[] BoundaryRectangle3D
        {
            get 
            {
                return mBoundaryRectangle3D; 
            }
        }

        public UV[] BoundaryRectangle2D
        {
            get
            {
                return mBoundaryRectangle2D;
            }
        }

        public List<List<XYZ>> VertexLists3D
        {
            get
            {
                return vertexLists3D;
            }
        }

        public List<List<UV>> VertexLists2D
        {
            get
            {
                return vertexLists2D;
            }
        }

        public SqureMeshGenerator MeshGenerator
        {
            get 
            {
                return mMeshGenerator;
            }

            set 
            {
                if (null != value)
                    mMeshGenerator = value;
            }
        }

        private bool GetEdgeVertex3D()
        {
            Contract.Assert(null != mFace);
            if (null == mFace)
                return false;

            vertexLists3D = Utility.GetFaceVertex3D(mFace);
            if (null == vertexLists3D || vertexLists3D.Count == 0)
                return false;

            return true;
        }


        private bool GetEdgeVertex2D()
        {
            Contract.Assert(null != mFace);
            if (null == mFace)
                return false;

            vertexLists2D = Utility.GetFaceVertex2D(mFace);
            if (null == vertexLists3D || vertexLists3D.Count == 0)
                return false;

            return true;
        }

        /// <summary>
        /// Get the 2D and 3D vertices of the face boundary 
        /// </summary>
        /// <returns></returns>
        private bool GenerateBoundary()
        {
            if (null == mFace)
                return false;

            BoundingBoxUV boundary = mFace.GetBoundingBox();

            mBoundaryRectangle3D = new XYZ[2];
            mBoundaryRectangle3D[0] = mFace.Evaluate(boundary.Min);
            mBoundaryRectangle3D[1] = mFace.Evaluate(boundary.Max);

            mBoundaryRectangle2D = new UV[2];
            mBoundaryRectangle2D[0] = boundary.Min;
            mBoundaryRectangle2D[1] = boundary.Max;

            return true;
        }

        /// <summary>
        /// It calculates which mesh the given point lies in.
        /// This operation is valid when the mesh generator is a square mesh generator.
        /// </summary>
        /// <param name="point3D"></param>
        /// <returns>Mesh.No</returns>
        private bool GenerateVertexMeshCode(UV point2D)
        {
            if (null == point2D)
                return false;

            Line2D[] meshLineArrayInColumn = mMeshGenerator.LineArrayInColumn2D;
            Line2D[] meshLineArrayInRow = mMeshGenerator.LineArrayInRow2D;




            return true;
        }


        /// <summary>
        /// It hides internal logic to make the object easy to use.
        /// </summary>
        /// <returns></returns>
        public bool PrepareAreaBoundaryVertex()
        {
            return GetEdgeVertex3D();
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

            pointLists3D = new List<List<XYZ>>();
            UV startPoint, endPoint;
            for (int i=0; i<vertexLists2D.Count; i++)
            {
                for (int j=0; j<vertexLists2D[i].Count; j++)
                {
                    startPoint = vertexLists2D[i][j];
                    endPoint = vertexLists2D[i][j+1];
                    
                    


                }
            }


            return true;
        }

        public bool ConfigureMeshGenerator()
        {
            if(null == mMeshGenerator)
                mMeshGenerator = new SqureMeshGenerator(mBoundaryRectangle2D, mGap, mMeshLength, mMeshWidth);       

            return true;
        }

    }
}
