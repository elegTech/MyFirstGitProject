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
    class AreaMeshIntersection
    {
        private List<List<UV>> intersectionList;

        public List<List<UV>> IntersectionList
        {
            get { return intersectionList; }
        }
    
        public int IntersectionCount                          
        {
            get 
            {
                if (null != intersectionList)
                    return intersectionList.Count;
                else
                    return 0;
            }
        }


        /// <summary>
        /// Add a closed polygon area represented by point list. 
        /// <param name="?"></param>               
        public void AddPolygon(List<UV> polygonPointList)
        {
            if(null == intersectionList)
                intersectionList = new List<List<UV>>();

            intersectionList.Add(polygonPointList);
        }
    }



    class IrregularAreaPaveManager
    {
        private Face mFaceToBePaved;

        private PolygonArea mFaceBoundary;

        private SqureMeshGenerator mMeshGenerator;

        /// <summary>
        /// Represent intersection area list. A mesh perhaps contains multiple intersection areas.
        /// </summary>
        private List<List<AreaMeshIntersection>> polygonList;
        


        public IrregularAreaPaveManager(Face face, double gap, double meshLength, double meshWidth)
        {
            mFaceToBePaved = face;
            mFaceBoundary = new PolygonArea(face, gap, meshLength, meshWidth);
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

            if(null == mMeshGenerator)
                mMeshGenerator = new SqureMeshGenerator(boundingBoxPointArray, gap, meshLength, meshWidth);
            else
                mMeshGenerator.ResetData2D(boundingBoxPointArray, gap, meshLength, meshWidth);
    
            mFaceBoundary.MeshGenerator = mMeshGenerator;

            // Initialize the intersection area for each mesh.
            polygonList = new List<List<AreaMeshIntersection>>();
            for (int i = 0; i < mMeshGenerator.MeshNumberInRow; i++)
            {
                polygonList.Add(new List<AreaMeshIntersection>());
                for (int j = 0; j < mMeshGenerator.MeshNumberInColumn; j++)
                {
                    polygonList[i].Add(new AreaMeshIntersection());
                }
            }
        }


        private bool GeneratePolygonForMesh()
        {


            return;
        }


        /// <summary>
        /// Generate polygon areas for mesh .
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="rowNumber">Mesh row number</param>
        /// <param name="columnNumber">Mesh column number</param>
        /// <returns></returns>
        private bool GeneratePolygonListForMesh(IMeshElement mesh, int rowNumber, int columnNumber)
        {
            if (null == mesh)
                return false;

            if (rowNumber < 0 || rowNumber >= mFaceBoundary.MeshGenerator.MeshNumberInRow)
                return false;

            if (columnNumber < 0 || rowNumber >= mFaceBoundary.MeshGenerator.MeshNumberInColumn)
                return false;

            List<PointStruct> areaPointInMesh = mFaceBoundary.AreaPointListInMesh[rowNumber][columnNumber];
            List<PointStruct> meshPointList = mFaceBoundary.MeshPointList[rowNumber][columnNumber];


            // Get an area vertex first.
            int index = areaPointInMesh.FindIndex(
                    delegate (PointStruct pointStruct)
                    {
                        return pointStruct.Feature == PointFeature.Vertex;
                    }
                );

            // It means no area vertex lies in this mesh, in other words, 
            // this mesh can be ignored since no intersection exists.
            if (-1 == index)
            {
                return true;
            }


            List<UV> polygonPoints = new List<UV>();
            polygonPoints.Add(areaPointInMesh[index].Point);
            int polygonStartIndex = -1;
            int polygonEndIndex = -1;

            while (0 != index)
            {
                if (PointFeature.Vertex == areaPointInMesh[index].Feature)
                {
                    index--;
                }
                else if (PointFeature.Intersection == areaPointInMesh[index].Feature ||
                    PointFeature.VertexAndIntersection == areaPointInMesh[index].Feature ||)
                {
                    polygonStartIndex = index;
                    break;
                }
            }
            while (index < areaPointInMesh.Count)
            {
                if (PointFeature.Vertex == areaPointInMesh[index].Feature)
                {
                    index++;
                }
                else if (PointFeature.Intersection == areaPointInMesh[index].Feature ||
                    PointFeature.VertexAndIntersection == areaPointInMesh[index].Feature ||)
                {
                    polygonEndIndex = index;
                    break;
                }
            }

            for (int i = polygonStartIndex; i < polygonEndIndex; i++)
            {
                polygonPoints.Add(areaPointInMesh[i].Point);
            }





            polygonList[rowNumber][columnNumber].AddPolygon(new List<UV>());




            for (int i=0; i<areaPointInMesh.Count; i++)
            {
                

            }







            UV point, nextPoint;
            AreaMeshIntersection meshAreaList = new AreaMeshIntersection();







            for (int i = 0; i < areaPointInMesh.Count; i++)
            {
                point = areaPointInMesh[i].Point;
                polygonPoints.Add(point);

                // If the next point on curve is included in the mesh,  
                if (areaPointInMesh[i+1].Contains(nextPoint))
                {
                    polygonPoints.Add(nextPoint);
                        
                }


            }

                //mFaceBoundary.MeshPointList;



                return false;
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
                    // In addition to mesh vertices, if there's no other point exists in mesh point list, 
                    // it implies that the intersection points are mesh vertices.
                    if (mFaceBoundary.MeshPointList[i][j].Count == Utility.MESHVERTEXNUMBER)
                    {
                        // The mesh is completely covered by area, no matter how many intersection points 
                        // exist in mFaceBoundary.AreaPointListInMesh[i][j].
                        if (mFaceToBePaved.IsInside(mFaceBoundary.MeshGenerator.MeshArrays[i,j].GetMeshCenter2D()))
                        {
                            polygonList[i][j].AddPolygon(new List<UV>(mFaceBoundary.MeshGenerator.MeshArrays[i,j].GetVertices2D()));
                        }
                    }
                    else
                    {
                        GeneratePolygonListForMesh(mFaceBoundary.MeshGenerator.MeshArrays[i,j], i, j);
                    }
                }
            }

            return true;
        }




    }
}
