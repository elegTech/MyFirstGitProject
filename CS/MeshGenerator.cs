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

    public enum FillStyle { Straight, Dagonal }
    
    /// <summary>
    /// It only supports straight paving since the dagonal paving can be converted into this style. 
    /// </summary>
    class MeshGenerator
    {
        /// <summary>
        /// The area that will be filled by mesh. Currently, only rectangle area is acceptable.
        /// This rectangle is represented by 4 vertices in clockwise sequence in coordinate systems.
        /// The bottom left vertex is No.0, the bottom right vertex is No.1, the top right vertex is No.2,
        /// and the top left vertex is No.3. Such vertex coding rule is consistent with mesh.
        /// 3 ........ 2
        ///   .      .
        ///   .      .
        /// 0 ........ 1
        /// </summary>
        private XYZ[] mRectangleArea;

        private FillStyle mFillMode;

        /// <summary>
        /// The gap between adjacent meshes.
        /// </summary>
        private double mGap;

        /// <summary>
        /// The mesh length along the mRectangleArea.
        /// </summary>
        private double mMeshLength;
        private double mMeshWidth;

        public MeshGenerator(XYZ[] rectangleArea, FillStyle fillMode, double gap, 
                            double meshLength, double meshWidth)
        {
            // Make sure the input array has correct items.
            Contract.Assert(null != rectangleArea && rectangleArea.Length == Utility.MESHVERTEXNUMBER);
          
            this.mRectangleArea = new XYZ[rectangleArea.Length];

            rectangleArea.CopyTo(this.mRectangleArea, 0);
            this.mFillMode = fillMode;

            this.mMeshLength = meshLength;
            this.mMeshWidth = meshWidth;
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


        /// Get the number of meshed that can be placed in the range, considering the gap between meshes.
        private double CalculateMeshNum(double range, double meshDim, double gap)
        {
            Contract.Assert(range > Utility.ZERO && meshDim > Utility.ZERO && gap >= Utility.ZERO);

            int fullMeshNum = (int)Math.Floor(range / (meshDim + gap));

            double partialMesh = (range - fullMeshNum * (meshDim + gap)) / meshDim;

            return fullMeshNum + partialMesh;
        }

      
        /// <summary>
        /// Get mesh list that cover the rectangle area. Basically, such area is covered by m*n meshes.
        /// </summary>
        /// <returns>2D mesh array</returns>
        public Mesh[][] GenerateMesh()
        {
            // Get the dimension, say length of the rectangle area.
            double length = mRectangleArea[0].DistanceTo(mRectangleArea[1]);

            // Get the dimension, say width of the rectangle area.
            double width = mRectangleArea[1].DistanceTo(mRectangleArea[2]);
            
            // Get the number of meshes in the length dimension.
            double meshNumberInColumn = CalculateMeshNum(length, mMeshLength, mGap);

            // Get the number of meshes in the width dimension.
            double meshNumberInRow = CalculateMeshNum(width, mMeshWidth, mGap);



            for(int i=0; i<meshNumberInRow; i++)
            {
                for(int j=0; j<meshNumberInColumn; j++)
                {



                }
                
            }




            return null;

        }



    }
}
