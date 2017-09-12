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
        /// This rectangle is represented by 4 vertices in clockwise sequence in coordinate systems.
        /// The vertex coding rule is consistent with SquareMesh.
        /// 3 ........ 2
        ///   .      .
        ///   .      .
        /// 0 ........ 1
        /// </summary>
        private XYZ[] mRectangleArea;

        /// <summary>
        /// The gap between adjacent meshes.
        /// </summary>
        private double mGap;

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
        /// It represents the lines along the columns and rows in the rectangle area, including the boundaries.
        /// </summary>
        private Line3D[] mLineArrayInColumn;
        private Line3D[] mLineArrayInRow;


        private SquareMesh[,] mMeshArray;

        public SqureMeshGenerator(XYZ[] rectangleArea, double gap, double meshLength, double meshWidth)
        {
            // Make sure the input array has correct items.
            Contract.Assert(null != rectangleArea && rectangleArea.Length == Utility.MESHVERTEXNUMBER);
          
            mRectangleArea = new XYZ[rectangleArea.Length];
            rectangleArea.CopyTo(this.mRectangleArea, 0);
            mMeshLength = meshLength;
            mMeshWidth = meshWidth;
        }
        
        public double MeshWidth
        {
            get
            {
                return mMeshWidth;
            }
            set
            {
                if(value < Double.Epsilon)
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
                if (value < Double.Epsilon)
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
                if (value < Double.Epsilon)
                    mGap = Utility.ZERO;
                else
                    mGap = value;
            }
        }


        public SquareMesh[,] MeshArray
        {
            get
            {
                if (null == mMeshArray)
                    GenerateMesh();

                return mMeshArray;
            }
        }

        public Line3D[] LineArrayInColumn
        {
            get
            {
                if (null == mLineArrayInColumn)
                    GenerateMeshLines();

                return mLineArrayInColumn;
            }
        }

        public Line3D[] LineArrayInRow
        {
            get
            {
                if(null == mLineArrayInRow)
                    GenerateMeshLines();

                return mLineArrayInRow;
            }
        }


        /// Get the number of meshed that can be placed in the range, considering the gap between meshes.
        private int CalculateMeshNum(double range, double meshDim, double gap)
        {
            Contract.Assert(range > Utility.ZERO && meshDim > Utility.ZERO && gap >= Utility.ZERO);

            int fullMeshNum = (int)Math.Floor(range / (meshDim + gap));
            double partialMesh = (range - fullMeshNum * (meshDim + gap)) / meshDim;

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
            // Get the dimension, say length of the rectangle area.
            double length = mRectangleArea[0].DistanceTo(mRectangleArea[1]);

            // Get the dimension, say width of the rectangle area.
            double width = mRectangleArea[1].DistanceTo(mRectangleArea[2]);

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

            mMeshArray = new SquareMesh[meshNumberInRow, meshNumberInColumn];
            XYZ meshLocation = null;
            XYZ lengthDirection = mRectangleArea[1] - mRectangleArea[0];
            XYZ widthDirection = mRectangleArea[3] - mRectangleArea[0];
          
            for (int i=0; i<meshNumberInRow; i++)
            {
                // The first mesh's location of this row. The delta location of adjacent meshes on 
                // neighbouring rows is the width plusing gap, along the width direction, i.e. the direction of rows.
                meshLocation = mRectangleArea[0] + i * (widthDirection.Normalize() * (mMeshWidth + mGap));

                // Given the location information, a square mesh can 
                mMeshArray[i, 0] = new SquareMesh(meshLocation, lengthDirection, widthDirection, mMeshLength, mMeshWidth);
                for (int j=1; j<meshNumberInColumn; j++)
                {
                    // The location of mesh along length direction is calculated similarly, i.e., each previous mesh's location 
                    // in the same row plusing delta length and gap is the current mesh's location.
                    meshLocation = mMeshArray[i, j-1].GetVertex(0) + j * (lengthDirection.Normalize() * (mMeshLength + mGap));
                    mMeshArray[i, j] = new SquareMesh(meshLocation, lengthDirection, widthDirection, mMeshLength, mMeshWidth);
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
            if(null == mMeshArray)
                return false;

            return true;
        }

        /// <summary>
        /// Genereate mesh lines from the meshes. A mesh line is represented by two points on rectangle area boundaries.
        /// Each line is parallel to length or width boundaries.
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

            mLineArrayInColumn = new Line3D[meshNumberInColumn * 2];
            mLineArrayInRow = new Line3D[meshNumberInRow * 2];

            // Record the column & row lines consist of meshes, each line is specified by both start & end points.
            Point3D startPoint;
            Point3D endPoint;
            XYZ tempVertex = null;
            for (int j=0; j<meshNumberInColumn; j++)
            {
                tempVertex = mMeshArray[0, j].GetVertex(0);
                startPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                tempVertex = mMeshArray[meshNumberInRow-1, j].GetVertex(3);
                endPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                mLineArrayInColumn[j*2] = new Line3D(startPoint, endPoint);

                tempVertex = mMeshArray[0, j].GetVertex(1);
                startPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                tempVertex = mMeshArray[meshNumberInRow-1, j].GetVertex(2);
                endPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                mLineArrayInColumn[j*2 + 1] = new Line3D(startPoint, endPoint);                               
            }

            for (int i=0; i<meshNumberInRow; i++)
            {
                tempVertex = mMeshArray[i, 0].GetVertex(0);
                startPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                tempVertex = mMeshArray[i, meshNumberInColumn-1].GetVertex(1);
                endPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                mLineArrayInRow[i * 2] = new Line3D(startPoint, endPoint);

                tempVertex = mMeshArray[i, 0].GetVertex(3);
                startPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                tempVertex = mMeshArray[i, meshNumberInColumn-1].GetVertex(2);
                endPoint = new Point3D(tempVertex.X, tempVertex.Y, tempVertex.Z);
                mLineArrayInRow[i * 2 + 1] = new Line3D(startPoint, endPoint);
            }

            Contract.Requires(mLineArrayInRow.Length > 0 && mLineArrayInColumn.Length>0);
            return true;
        }
        
    }
}
