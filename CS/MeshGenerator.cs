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
    public enum FillStyle { Straight, Dagonal }
    
    /// <summary>
    /// It only supports straight paving since the dagonal paving can be converted into this style. 
    /// </summary>
    class SqureMeshGenerator
    {
        /// <summary>
        /// The area that will be filled by mesh. Currently, only rectangle area is acceptable.
        /// This rectangle is represented by 4 vertices in counter-clockwise sequence in coordinate systems.
        /// The vertex coding rule is consistent with SquareMesh. This area can be defined using 3D points or
        /// 2D points, note that all vertices are on the same plan.
        /// 3 ........ 2
        ///   .      .
        ///   .      .
        /// 0 ........ 1
        /// </summary>
        private XYZ[] mRectangleArea3D = null;

        // The rectangle area is defined by 2D points.
        private UV[] mRectangleArea2D = null;

        /// <summary>
        /// The gap between adjacent meshes.
        /// </summary>
        private double mGap = Utility.ZERO;

        /// <summary>
        /// The mesh length along the mRectangleArea.
        /// </summary>
        private double mMeshLength = Utility.ZERO;
        private double mMeshWidth = Utility.ZERO;

        /// <summary>
        /// Illegal initial value indicates that the variable is unset in order to prevent misuse.
        /// </summary>
        private int meshNumberInColumn = -1;
        private int meshNumberInRow = -1;

        /// <summary>
        /// It represents the lines in XYZ along the columns and rows in the rectangle area, including the boundaries.
        /// </summary>
        private Line3D[] mLineArrayInColumn3D = null;
        private Line3D[] mLineArrayInRow3D = null;

        /// <summary>
        /// It represents the lines in UV along the columns and rows in the rectangle area, including the boundaries.
        /// </summary>
        private Line2D[] mLineArrayInColumn2D = null;
        private Line2D[] mLineArrayInRow2D = null;

        private SquareMesh[,] mMeshArrays;

        private bool is3DMesh = false;

        public SqureMeshGenerator(XYZ[] rectangleArea3D, double gap, double meshLength, double meshWidth)
        {
            // Make sure the input array has correct items.
            Contract.Assert(null != rectangleArea3D && rectangleArea3D.Length == Utility.MESHVERTEXNUMBER);
          
            mRectangleArea3D = new XYZ[rectangleArea3D.Length];
            rectangleArea3D.CopyTo(mRectangleArea3D, 0);
            mMeshLength = meshLength;
            mMeshWidth = meshWidth;
            is3DMesh = true;
        }

        public SqureMeshGenerator(UV[] rectangleArea2D, double gap, double meshLength, double meshWidth)
        {
            // Make sure the input array has correct items.
            Contract.Assert(null != rectangleArea2D && rectangleArea2D.Length == Utility.MESHVERTEXNUMBER);

            mRectangleArea2D = new UV[rectangleArea2D.Length];
            rectangleArea2D.CopyTo(mRectangleArea2D, 0);
            mMeshLength = meshLength;
            mMeshWidth = meshWidth;
            is3DMesh = false;
        }

        public double MeshWidth
        {
            get
            {
                return mMeshWidth;
            }
            set
            {
                if(value < Utility.ZERO)
                    mMeshWidth = Utility.ZERO;
                else
                    mMeshWidth = value;
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
                if (value < Utility.ZERO)
                    mMeshLength = Utility.ZERO;
                else
                    mMeshLength = value;
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
                if (value < Utility.MESHGAPCEIL)
                    mGap = Utility.ZERO;
                else
                    mGap = value;
            }
        }

        public XYZ[] RectangleArea3D
        {
            get
            {
                return mRectangleArea3D;
            }
            set
            {
                if (null == value || value.Length == 0)
                    mRectangleArea3D = null;
                else
                {
                    value.CopyTo(mRectangleArea3D, 0);
                    is3DMesh = true;
                }
            }
        }


        public UV[] RectangleArea2D
        {
            get
            {
                return mRectangleArea2D;
            }
            set
            {
                if (null == value || value.Length == 0)
                    mRectangleArea2D = null;
                else
                {
                    value.CopyTo(mRectangleArea2D, 0);
                    is3DMesh = false;
                }
            }
        }


        public SquareMesh[,] MeshArrays
        {
            get
            {
                if (null == mMeshArrays)
                    GenerateMesh();

                return mMeshArrays;
            }
        }

        public Line3D[] LineArrayInColumn3D
        {
            get
            {
                if (null == mLineArrayInColumn3D)
                    GenerateMeshLines();

                return mLineArrayInColumn3D;
            }
        }

        public Line2D[] LineArrayInColumn2D
        {
            get
            {
                if (null == mLineArrayInColumn2D)
                    GenerateMeshLines();

                return mLineArrayInColumn2D;
            }
        }

        public Line3D[] LineArrayInRow3D
        {
            get
            {
                if(null == mLineArrayInRow3D)
                    GenerateMeshLines();

                return mLineArrayInRow3D;
            }
        }

        public Line2D[] LineArrayInRow2D
        {
            get
            {
                if (null == mLineArrayInRow2D)
                    GenerateMeshLines();

                return mLineArrayInRow2D;
            }
        }

        public bool ResetData3D(XYZ[] rectangleArea, double gap, double meshLength, double meshWidth)
        {
            Contract.Assert(null != rectangleArea && rectangleArea.Length == Utility.MESHVERTEXNUMBER);
            if (null == rectangleArea || rectangleArea.Length == 0)
                return false;

            if (gap < Utility.MESHGAPCEIL)
                return false;

            if (meshLength < Utility.MESHSIZECEIL || meshLength < Utility.MESHSIZECEIL)
                return false;


            rectangleArea.CopyTo(mRectangleArea3D, 0);
            mMeshLength = meshLength;
            mMeshWidth = meshWidth;
            is3DMesh = true;

            return true;
        }


        public bool ResetData2D(UV[] rectangleArea, double gap, double meshLength, double meshWidth)
        {
            Contract.Assert(null != rectangleArea && rectangleArea.Length == Utility.MESHVERTEXNUMBER);
            if (null == rectangleArea || rectangleArea.Length == 0)
                return false;

            if (gap < Utility.MESHGAPCEIL)
                return false;

            if (meshLength < Utility.MESHSIZECEIL || meshLength < Utility.MESHSIZECEIL)
                return false;


            rectangleArea.CopyTo(mRectangleArea2D, 0);
            mMeshLength = meshLength;
            mMeshWidth = meshWidth;
            is3DMesh = true;

            return true;
        }

        /// Get the number of meshes that can be placed in the range, considering the gap between meshes.
        private int CalculateMeshNum(double range, double meshDimension, double gap)
        {
            Contract.Assert(range > Utility.ZERO && meshDimension > Utility.ZERO && gap >= Utility.ZERO);

            int fullMeshNum = (int)Math.Floor(range / (meshDimension + gap));
            double partialMesh = (range - fullMeshNum * (meshDimension + gap)) / meshDimension;

            return fullMeshNum + (int)Math.Ceiling(partialMesh);
        }

      
        /// <summary>
        /// Get mesh list that cover the rectangle area. Basically, the area is covered 
        /// by m*n meshes. The layout of these meshes is a m*n matrix, shown as follows:
        ///  (m-1,0)..........(m-1,n-1)
        ///         ..........
        ///         ..........
        ///    (0,0)..........(0,n-1)
        /// </summary>
        /// <returns>2D mesh array</returns>
        private bool GenerateMesh()
        {

            double length = Utility.ZERO;
            double width = Utility.ZERO;

            // Get the dimension, i.e., length and width of the rectangle area.
            if(is3DMesh)
            { 
                length = mRectangleArea3D[0].DistanceTo(mRectangleArea3D[1]);
                width = mRectangleArea3D[1].DistanceTo(mRectangleArea3D[2]);
            }
            else
            {
                length = mRectangleArea2D[0].DistanceTo(mRectangleArea2D[1]);
                width = mRectangleArea2D[1].DistanceTo(mRectangleArea2D[2]);
            }


            // Make sure the area dimension is legal.
            if (length < Utility.DIMENSIONFLOOR || width < Utility.DIMENSIONFLOOR)
                return false;

            // Get the number of meshes in the length dimension.
            meshNumberInColumn = CalculateMeshNum(length, mMeshLength, mGap);

            // Get the number of meshes in the width dimension.
            meshNumberInRow = CalculateMeshNum(width, mMeshWidth, mGap);

            // Make sure that meshNumberInRow and meshNumberInColumn should be positive.
            if (meshNumberInRow < 0 || meshNumberInColumn < 0)
                return false;

            mMeshArrays = new SquareMesh[meshNumberInRow, meshNumberInColumn];

            if (is3DMesh)
            {
                XYZ meshLocation = null;
                XYZ lengthDirection = mRectangleArea3D[1] - mRectangleArea3D[0];
                XYZ widthDirection = mRectangleArea3D[3] - mRectangleArea3D[0];

                for (int i = 0; i < meshNumberInRow; i++)
                {
                    // The first mesh's location of this row. The delta location of adjacent meshes on 
                    // neighbouring rows is the width plusing gap, along the width direction, i.e. the direction of rows.
                    meshLocation = mRectangleArea3D[0] + i * (widthDirection.Normalize() * (mMeshWidth + mGap));

                    // Given the first sequare mesh using the location information.
                    mMeshArrays[i, 0] = new SquareMesh(meshLocation, lengthDirection, widthDirection, mMeshLength, mMeshWidth);
                    for (int j = 1; j < meshNumberInColumn; j++)
                    {
                        // The location of mesh along length direction is calculated similarly, i.e., each previous mesh's location 
                        // in the same row plusing delta length and gap is the current mesh's location.
                        meshLocation = mMeshArrays[i, j - 1].GetVertex3D(0) + j * (lengthDirection.Normalize() * (mMeshLength + mGap));
                        mMeshArrays[i, j] = new SquareMesh(meshLocation, lengthDirection, widthDirection, mMeshLength, mMeshWidth);
                    }
                }
            }
            else
            {
                UV meshLocation = null;
                UV lengthDirection = mRectangleArea2D[1] - mRectangleArea2D[0];
                UV widthDirection = mRectangleArea2D[3] - mRectangleArea2D[0];

                for (int i = 0; i < meshNumberInRow; i++)
                {
                    // The first mesh's location of this row. The delta location of adjacent meshes on 
                    // neighbouring rows is the width plusing gap, along the width direction, i.e. the direction of rows.
                    meshLocation = mRectangleArea2D[0] + i * (widthDirection.Normalize() * (mMeshWidth + mGap));

                    // Given the location information, a square mesh can 
                    mMeshArrays[i, 0] = new SquareMesh(meshLocation, lengthDirection, widthDirection, mMeshLength, mMeshWidth);
                    for (int j = 1; j < meshNumberInColumn; j++)
                    {
                        // The location of mesh along length direction is calculated similarly, i.e., each previous mesh's location 
                        // in the same row plusing delta length and gap is the current mesh's location.
                        meshLocation = mMeshArrays[i, j - 1].GetVertex2D(0) + j * (lengthDirection.Normalize() * (mMeshLength + mGap));
                        mMeshArrays[i, j] = new SquareMesh(meshLocation, lengthDirection, widthDirection, mMeshLength, mMeshWidth);
                    }
                }


            }
            return true;
        }


        /// <summary>
        /// Check whether it is ready to generate mesh lines or not, according to the meshes.
        /// </summary>
        /// <returns></returns>
        private bool CanGenerateMeshLines()
        {
            if(null == mMeshArrays)
                return false;

            return true;
        }

        /// <summary>
        /// Genereate mesh lines from the meshes. A mesh line is represented by two points 
        /// on rectangle area boundaries. Each line is parallel to length or width boundaries.
        /// </summary>
        /// <returns></returns>
        private bool GenerateMeshLines()
        {
            // If it's not ready, generate the meshes first.
            if (!CanGenerateMeshLines())
            {
                if (!GenerateMesh())
                    return false;
            }
            if (is3DMesh)
            {
                mLineArrayInColumn3D = new Line3D[meshNumberInColumn * 2];
                mLineArrayInRow3D = new Line3D[meshNumberInRow * 2];
            }
            else
            {
                mLineArrayInColumn2D = new Line2D[meshNumberInColumn * 2];
                mLineArrayInRow2D = new Line2D[meshNumberInRow * 2];
            }
            // Record the column & row lines consist of meshes, each line is specified by both 
            // start & end points. Specifically, for a line, the coordinate value of startpoint
            // is less than that of endpoint. Namely, startpoint->endpoint should be one of the
            // following cases: No.0->No.1; No.1->No.2; No.0->No.3; No.3->No.2; 
            // Moreover, these lines are stored in sequence along 
            if (is3DMesh)
            { 
                Point3D startPoint;
                Point3D endPoint;
                XYZ tempVertex = null;

                // It records two lines for each mesh on column direction, i.e., No.0->No.3,No.1->No.2.
                for (int j = 0; j < meshNumberInColumn; j++)
                {
                    tempVertex = mMeshArrays[0, j].GetVertex3D(0);
                    startPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                    tempVertex = mMeshArrays[meshNumberInRow - 1, j].GetVertex3D(3);
                    endPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                    mLineArrayInColumn3D[j * 2] = new Line3D(startPoint, endPoint);

                    tempVertex = mMeshArrays[0, j].GetVertex3D(1);
                    startPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                    tempVertex = mMeshArrays[meshNumberInRow - 1, j].GetVertex3D(2);
                    endPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                    mLineArrayInColumn3D[j * 2 + 1] = new Line3D(startPoint, endPoint);
                }

                // It records two lines for each mesh on row direction, i.e., No.0->No.1,No.3->No.2.
                for (int i = 0; i < meshNumberInRow; i++)
                {
                    tempVertex = mMeshArrays[i, 0].GetVertex3D(0);
                    startPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                    tempVertex = mMeshArrays[i, meshNumberInColumn - 1].GetVertex3D(1);
                    endPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                    mLineArrayInRow3D[i * 2] = new Line3D(startPoint, endPoint);

                    tempVertex = mMeshArrays[i, 0].GetVertex3D(3);
                    startPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                    tempVertex = mMeshArrays[i, meshNumberInColumn - 1].GetVertex3D(2);
                    endPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                    mLineArrayInRow3D[i * 2 + 1] = new Line3D(startPoint, endPoint);
                }
            }
            else
            {
                Point2D startPoint;
                Point2D endPoint;
                UV tempVertex = null;

                // It records two lines for each mesh on column direction, i.e., No.0->No.3,No.1->No.2.
                for (int j = 0; j < meshNumberInColumn; j++)
                {
                    tempVertex = mMeshArrays[0, j].GetVertex2D(0);
                    startPoint = new Point2D(tempVertex.U, tempVertex.V);
                    tempVertex = mMeshArrays[meshNumberInRow - 1, j].GetVertex2D(3);
                    endPoint = new Point2D(tempVertex.U, tempVertex.V);
                    mLineArrayInColumn2D[j * 2] = new Line2D(startPoint, endPoint);

                    tempVertex = mMeshArrays[0, j].GetVertex2D(1);
                    startPoint = new Point2D(tempVertex.U, tempVertex.V);
                    tempVertex = mMeshArrays[meshNumberInRow - 1, j].GetVertex2D(2);
                    endPoint = new Point2D(tempVertex.U, tempVertex.V);
                    mLineArrayInColumn2D[j * 2 + 1] = new Line2D(startPoint, endPoint);
                }

                // It records two lines for each mesh on row direction, i.e., No.0->No.1,No.3->No.2.
                for (int i = 0; i < meshNumberInRow; i++)
                {
                    tempVertex = mMeshArrays[i, 0].GetVertex2D(0);
                    startPoint = new Point2D(tempVertex.U, tempVertex.V);
                    tempVertex = mMeshArrays[i, meshNumberInColumn - 1].GetVertex2D(1);
                    endPoint = new Point2D(tempVertex.U, tempVertex.V);
                    mLineArrayInRow2D[i * 2] = new Line2D(startPoint, endPoint);

                    tempVertex = mMeshArrays[i, 0].GetVertex2D(3);
                    startPoint = new Point2D(tempVertex.U, tempVertex.V);
                    tempVertex = mMeshArrays[i, meshNumberInColumn - 1].GetVertex2D(2);
                    endPoint = new Point2D(tempVertex.U, tempVertex.V);
                    mLineArrayInRow2D[i * 2 + 1] = new Line2D(startPoint, endPoint);
                }
            }

            return true;
        }
        
    }
}
