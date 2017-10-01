using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace ParameterUtils
{
    class IrregularAreaPaveManager
    {
        private Face mFaceToBePaved;

        private PolygonArea mFaceBoundary;

        private SqureMeshGenerator mMeshGenerator;

        public IrregularAreaPaveManager(Face face, double gap, double meshLength, double meshWidth)
        {
            mFaceToBePaved = face;
            Initialize(gap, meshLength, meshWidth);
        }

        public Face FaceToBePaved
        {
            get { return mFaceToBePaved; }
            set
            {
                if (null != value)
                    mFaceToBePaved = value;
            }
        }
        

        public void ConfigurePavementSetting(double gap, double meshLength, double meshWidth)
        {
            if (null == mFaceToBePaved)
                return;

            Initialize(gap, meshLength, meshWidth);
        }


        private void Initialize(double gap, double meshLength, double meshWidth)
        {
            if (null == mFaceToBePaved)
                return;

            BoundingBoxUV boundingBox = mFaceToBePaved.GetBoundingBox();
            UV[] boundingBoxPointArray = new UV[] { boundingBox.Min, boundingBox.Max };

            mMeshGenerator = new SqureMeshGenerator(boundingBoxPointArray, gap, meshLength, meshWidth);
            mFaceBoundary.MeshGenerator = mMeshGenerator;
        }


        public bool Pave()
        {
            if (null == mFaceToBePaved)
                return false;

            if (mFaceBoundary.PrepareAreaPointLists())
                return false;

            int meshRowNumber = mFaceBoundary.MeshGenerator.MeshNumberInRow;
            int meshColumnNumber = mFaceBoundary.MeshGenerator.MeshNumberInColumn;


            for (int i=0; i<meshRowNumber; i++)
            {
                for(int j=0; j<meshColumnNumber; j++)
                {
                    // This mesh does not contain any intersection.
                    if (mFaceBoundary.AreaPointListInMesh[i][j].Count == 0 &&
                        mFaceBoundary.MeshPointList[i][j].Count == Utility.MESHVERTEXNUMBER)
                    {
                        continue;
                    }

                    // Similar to the case above.
                    if (mFaceBoundary.AreaPointListInMesh[i][j].Count == 1 &&
                        mFaceBoundary.MeshPointList[i][j].Count == Utility.MESHVERTEXNUMBER)
                    {
                        continue;
                    }

                    // Similar to the case above.
                    if (mFaceBoundary.AreaPointListInMesh[i][j].Count == 0 &&
                        mFaceBoundary.MeshPointList[i][j].Count == Utility.MESHVERTEXNUMBER)
                    {
                        if (mFaceToBePaved.IsInside(mFaceBoundary.MeshGenerator.mesh))
                        {


                        }

                        continue;
                    }


                }
            }
            ;
            mFaceBoundary.MeshPointList


            return false;
        }




    }
}
