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
    class AreaMeshIntersection
    {

        #region Member and data accessor
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
        #endregion


        #region Core logics
        /// <summary>
        /// Add a closed polygon area represented by point list. 
        /// <param name="?"></param>               
        public void AddPolygon(List<UV> polygonPointList)
        {
            if (null == polygonPointList || polygonPointList.Count == 0)
                return;

            if(null == intersectionList)
                intersectionList = new List<List<UV>>();

            intersectionList.Add(polygonPointList);
        }

        public void Clear()
        {
            for (int i = 0; i < intersectionList.Count; i++)
                intersectionList[i].Clear();

            intersectionList.Clear();
        }

        #endregion
    }



    class IrregularAreaPaveManager
    {
        #region Private members

        private Face mFaceToBePaved;

        private PolygonArea mFaceBoundary;

        private SqureMeshGenerator mMeshGenerator;

        /// <summary>
        /// Represent intersection area list. A mesh perhaps contains multiple intersection areas.
        /// </summary>
        private List<List<AreaMeshIntersection>> polygonList;

        #endregion


        #region Constructor & Data configurator
         
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

        #endregion


        #region Core privatre logics
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


        private List<UV> GeneratePolygonForMesh(List<PointStruct> areaPointInMesh, List<PointStruct> meshPointList)
        {
            if (null == areaPointInMesh || areaPointInMesh.Count == 0)
                return null;

            if (null == meshPointList || meshPointList.Count == 0)
                return null;

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
                return null;
            }


            // A closed area consists of two point lists: (1)point list in area point list; 
            // (2) point list in mesh point list; It will find the former list first and then the next.
            List<UV> polygonPoints = new List<UV>();
            int polygonStartIndexInArea = index - 1;
            while (polygonStartIndexInArea >= 0)
            {
                if (PointFeature.Vertex == areaPointInMesh[polygonStartIndexInArea].Feature)
                {
                    polygonStartIndexInArea--;
                }
                else if (PointFeature.Intersection == areaPointInMesh[polygonStartIndexInArea].Feature ||
                    PointFeature.VertexAndIntersection == areaPointInMesh[polygonStartIndexInArea].Feature)
                {
                    break;
                }
            }

            int polygonEndIndexInArea = index + 1;
            while (polygonEndIndexInArea < areaPointInMesh.Count)
            {
                if (PointFeature.Vertex == areaPointInMesh[polygonEndIndexInArea].Feature)
                {
                    polygonEndIndexInArea++;
                }
                else if (PointFeature.Intersection == areaPointInMesh[polygonEndIndexInArea].Feature ||
                    PointFeature.VertexAndIntersection == areaPointInMesh[polygonEndIndexInArea].Feature)
                {
                    break;
                }
            }


            // Get the points in mesh point list that belong to an intersection which will form a closed area.
            // Please note that all points of the closed area are listed in anticlockwise sequence. Therefore,
            // The point areaPointInMesh[polygonEndIndex] is the start point in meshPointList.
            int startPointIndexInMesh;
            int endPointIndexInMesh;

            startPointIndexInMesh = meshPointList.FindIndex(
              delegate (PointStruct pointStruct)
              {
                  if (pointStruct.Point == areaPointInMesh[polygonEndIndexInArea].Point)
                      return true;

                  if (pointStruct.Point.DistanceTo(areaPointInMesh[polygonEndIndexInArea].Point) < Utility.THRESHHOLDVALUE)
                      return true;

                  return false;
              });

            // The intersection point must exist in the meshPointList.
            Contract.Assert(-1 != startPointIndexInMesh);
            if(-1 == startPointIndexInMesh)
            {
                return null;
            }

            // There are two methods to get the last point in mesh point list:
            // (1) Just find the point defined by areaPointInMesh[polygonEndIndex] in 
            // mesh point list; (2) Start from the startPointIndexInMesh, iterate the
            // mesh point list to get the first intersection point. The second one is
            // used for efficiency.
            endPointIndexInMesh = startPointIndexInMesh;
            while (++endPointIndexInMesh < meshPointList.Count)
            {
                if (PointFeature.Intersection == meshPointList[endPointIndexInMesh].Feature ||
                    PointFeature.VertexAndIntersection == meshPointList[endPointIndexInMesh].Feature)
                {
                    break;
                }
            }
            
            // Add the points in area point list that consis of a closed area.
            for (int i = polygonStartIndexInArea; i < polygonEndIndexInArea; i++)
            {
                polygonPoints.Add(areaPointInMesh[i].Point);
            }

            // The intersection points have been added above, here add only the 
            // points between them in mesh point list.
            for (int i = startPointIndexInMesh+1; i < endPointIndexInMesh; i++)
            {
                // If the three points indicated by i-1, i, i+1 are colinear, ignore the second point.
                if ((meshPointList[i].Point - meshPointList[i-1].Point).AngleTo(
                     meshPointList[i+1].Point - meshPointList[i].Point) < Utility.THRESHHOLDVALUE)
                    continue;

                polygonPoints.Add(meshPointList[i].Point);
            }

            // Remove the added vetices in areaPointInMesh. Note both end 
            // intersection points remain in area point list.
            areaPointInMesh.RemoveRange(polygonStartIndexInArea+1, polygonEndIndexInArea - polygonStartIndexInArea - 1);

            return polygonPoints;
        }


        /// <summary>
        /// Generate polygon areas for mesh .
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="rowNumber">Mesh row number</param>
        /// <param name="columnNumber">Mesh column number</param>
        /// <returns></returns>
        private AreaMeshIntersection GeneratePolygonListForMesh(IMeshElement mesh, int rowNumber, int columnNumber)
        {
            if (null == mesh)
                return null;

            if (rowNumber < 0 || rowNumber >= mFaceBoundary.MeshGenerator.MeshNumberInRow)
                return null;

            if (columnNumber < 0 || rowNumber >= mFaceBoundary.MeshGenerator.MeshNumberInColumn)
                return null;

            List<PointStruct> areaPointInMesh = mFaceBoundary.AreaPointListInMesh[rowNumber][columnNumber];
            List<PointStruct> meshPointList = mFaceBoundary.MeshPointList[rowNumber][columnNumber];


            AreaMeshIntersection meshAreaList = new AreaMeshIntersection();
            List<UV> polygonPoints = GeneratePolygonForMesh(areaPointInMesh, meshPointList);
            while (null != polygonPoints)
            {
                meshAreaList.AddPolygon(polygonPoints);
                polygonPoints = GeneratePolygonForMesh(areaPointInMesh, meshPointList);
            }
            
            return meshAreaList;
        }
        #endregion


        #region Exposed methods
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
                        polygonList[i][j] = GeneratePolygonListForMesh(mFaceBoundary.MeshGenerator.MeshArrays[i,j], i, j);
                    }
                }
            }

            return true;
        }

        public void Clear()
        {
            for(int i=0; i<polygonList.Count;i++)
            {
                for (int j = 0; j < polygonList[i].Count; j++)
                {
                    polygonList[i][j].Clear();
                }
                polygonList[i].Clear();
            }
            polygonList.Clear();
            mFaceBoundary.Clear();
        }
        #endregion
    }
}
