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

    public enum FillStyle { Straight, Diagonal }
    
    /// <summary>
    /// It only supports straight paving since diagonal paving can be converted into this style. 
    /// </summary>
    class MeshGenerator
    {
        /// <summary>
        /// The area that will be filled by mesh. Currently, only rectangle area is acceptable.
        /// This rectangle is represented by 4 vertices in clockwise sequence, the bottom left
        /// vertex is No.0, the bottom right vertex is No.1, the top right vertex is No.2, and
        /// the top left vertex is No.3. Such vertex coding rule is consistent with mesh.
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
            // A mesh has only 4 vertices.
            Contract.Assert(null != rectangleArea && rectangleArea.Length == 4);
          
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
                if(value < Utility.GAPTHRESHOLD)
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
                if (value < Utility.GAPTHRESHOLD)
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
                if (value < Utility.GAPTHRESHOLD)
                    mGap = Utility.ZERO;
                else
                    mGap = value;
            }
        }

        public Mesh[] GenerateMesh()
        {






            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private double CalculateMeshNumber(double range, double meshDimension, double gap)
        {



        }

    }
}
