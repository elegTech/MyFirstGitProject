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

    class PointComparer : IComparer<UV>
    {
        private UV basePoint;

        public PointComparer(UV basePoint)
        {
            this.basePoint = basePoint;     
        }

        int IComparer<UV>.Compare(UV x, UV y)
        {
            if (null == x)
            {
                // If x is null and y is null, they're
                // equal.
                if (null == y)
                {
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater. 
                    return -1;
                }
            }
            else
            {
                // If x is not null, and y is null, x is greater.
                if (null == y)
                {
                    return 1;
                }
                else
                {
                    if (x == y)
                        return 0;

                    if (x.IsAlmostEqualTo(y, Utility.THRESHHOLDVALUE))
                        return 0;

                    if (x.DistanceTo(basePoint) < y.DistanceTo(basePoint))
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
            }
        }
    }



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
        /// Face curves vertices defined using 2D points.
        /// </summary>
        private List<List<UV>> vertexLists2D;


        /// <summary>
        /// In addition to all vertices in vertexLists, it includes 
        /// intersection points of the area boundaries and meshes.
        /// </summary>
        private List<List<UV>> pointLists2D;


        /// <summary>
        /// Record 2D index of the point in pointLists2D.
        /// </summary>
        public struct PointListIndex
        {
            // They are indexes of the point in pointLists2D.
            int curveIndexOfLoop, pointIndexOfCurve;
            
            // Indicate whether the point is a vertex of this area or not.
            bool isAreaVertex;

            public int CurveIndex
            {
                get { return curveIndexOfLoop; }
            }

            public int PointIndex
            {
                get { return pointIndexOfCurve; }
            }


            public PointListIndex(int curveIndex, int pointIndex, bool isVertex = false)
            {
                this.curveIndexOfLoop = curveIndex;
                this.pointIndexOfCurve = pointIndex;
                this.isAreaVertex = isVertex;
            }
        }


        /// <summary>
        /// It stores the points lie in specific mesh using the row index and column index
        /// of the pointLists2D. Namely, pointListInMesh[i][j] indicates the points on 
        /// area curves in the mesh at No.i row & No.j column. Each "PointIndex" stores 
        /// the 2D indexs in pointLists2D/pointLists3D.
        /// </summary> 
        private List<List<List<PointListIndex>>> pointListInMesh;


        /// <summary>
        /// Store the mesh vertices that lie in area, and the intersection points
        /// on four mesh sides (Note: not in the mesh).
        /// </summary>
        private List<List<List<UV>>> meshPointList;


        private SqureMeshGenerator mMeshGenerator;

        /// <summary>
        /// Gap of meshs paved on the face.
        /// </summary>
        private double mGap;


        private double mMeshLength;
        private double mMeshWidth;


        /// <summary>
        /// Boundary rectangle defined using 2D points.
        /// </summary>
        private UV[] mBoundaryRectangle2D;

        public PolygonArea(Face face, double gap, double meshDimensionU, double meshDimensionV)
        {
            mFace = face;
            mGap = gap;
            mMeshLength = meshDimensionU;
            mMeshWidth = meshDimensionV;
            Initialize();
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

        public UV[] BoundaryRectangle2D
        {
            get
            {
                return mBoundaryRectangle2D;
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


        public List<List<List<PointListIndex>>> AreaPointListInMesh
        {
            get
            {
                return pointListInMesh;
            }
        }

        public List<List<List<UV>>> MeshPointList
        {
            get
            {
                return meshPointList;
            }
        }

        private void Initialize()
        {
            // Return when the boundary is inaccessable.
            if (!GenerateBoundary())
                return;

            mMeshGenerator = new SqureMeshGenerator(mBoundaryRectangle2D, mGap, mMeshLength, mMeshWidth);
            mMeshGenerator.GenerateMeshLines();

            pointListInMesh = new List<List<List<PointListIndex>>>();
            meshPointList = new List<List<List<UV>>>();

            // Initialize the pointListInMesh according to the row number and column number of 2D mesh arrays.
            for (int i=0; i< mMeshGenerator.MeshNumberInRow; i++)
            {
                pointListInMesh.Add(new List<List<PointListIndex>>());
                meshPointList.Add(new List<List<UV>>());
                for (int j=0; j<mMeshGenerator.MeshNumberInColumn; j++)
                {
                    pointListInMesh[i].Add(new List<PointListIndex>());
                    meshPointList[i].Add(new List<UV>());
                }
            }

            GetEdgeVertex2D();
        }


        private bool GetEdgeVertex2D()
        {
            Contract.Assert(null != mFace);
            if (null == mFace)
                return false;

            vertexLists2D = Utility.GetFaceVertex2D(mFace);
            if (null == vertexLists2D || vertexLists2D.Count == 0)
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

            mBoundaryRectangle2D = new UV[2];
            mBoundaryRectangle2D[0] = boundary.Min;
            mBoundaryRectangle2D[1] = boundary.Max;

            return true;
        }


        /// <summary>
        /// If the point is on mesh sides, return the side index. Otherwise, return -1.
        /// Note that the side index indicates that the edge's start point is No.index vertex.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public int GetMeshSideThroughPoint(UV point, IMeshElement mesh, ref bool isCoincidentWithMeshVertex)
        {
            if (null == point || null == mesh)
                return -1;

            UV[] vertexList = mesh.GetVertices2D();
            int vertexCount = vertexList.Length;
            Line2D line;
            Point2D startPoint, endPoint;
            Point2D tempPoint = new Point2D(point.U, point.V);
            List<int> pointList = new List<int>();
            isCoincidentWithMeshVertex = false;

            for (int i=0; i<vertexCount;i++)
            {
                if (point == vertexList[i] || point.DistanceTo(vertexList[i]) < Utility.THRESHHOLDVALUE)
                {
                    isCoincidentWithMeshVertex = true;
                    return -1;
                }

                startPoint = new Point2D(vertexList[i].U, vertexList[i].V);

                // Avoid overstepping of array boundary.
                endPoint = new Point2D(vertexList[(i+1) % vertexCount].U, vertexList[(i+1) % vertexCount].V);

                line = new Line2D(startPoint, endPoint);

                if (line.ClosestPointTo(tempPoint, true).DistanceTo(tempPoint) < Utility.THRESHHOLDVALUE)
                {
                    return i;
                }
            }

            return -1;
        }

        
        /// <summary>
        /// Sort the vertices and intersection points that lie in mesh sides
        /// in order to make them in counter-clock direction.
        /// </summary>
        /// <returns></returns>
        private bool GenerateMeshPointListInArea()
        {
            if (null == mFace || null == mMeshGenerator)
                return false;

            UV point = null;
            bool isCoincidentWithVertex = false;
            int sideIndex = -1;

            // Use to store points on each mesh edge. No.i vertex belongs to
            // No.i edge.   
            List<List<UV>> pointLists = new List<List<UV>>(Utility.MESHVERTEXNUMBER); 
            for(int i=0; i<Utility.MESHVERTEXNUMBER; i++)
            {
                pointLists.Add(new List<UV>());
            }


            for (int i=0; i<mMeshGenerator.MeshNumberInRow; i++)
            {
                for(int j=0; j< mMeshGenerator.MeshNumberInColumn; j++)
                {      
                    
                    // Add four vertices of mesh into the point list first.
                    for (int k = 0; k < Utility.MESHVERTEXNUMBER; k++)
                    {
                        pointLists[k].Add(mMeshGenerator.MeshArrays[i, j].GetVertex2D(k));
                    }

                    foreach (PointListIndex pointIndex in pointListInMesh[i][j])
                    {
                        point = pointLists2D[pointIndex.CurveIndex][pointIndex.PointIndex];
                        sideIndex = GetMeshSideThroughPoint(point, mMeshGenerator.MeshArrays[i, j], ref isCoincidentWithVertex);
                        
                        // Ignore the vertex point.
                        if (isCoincidentWithVertex)
                        {
                            continue;
                        }

                        // Add intersection point without considering the sequence.
                        // Namely, points on each side maybe unordered.
                        if (sideIndex >= 0 && sideIndex <= Utility.MESHVERTEXNUMBER)
                        {
                            pointLists[sideIndex].Add(point);
                        }
                    }

                    // Sort the items in point list based on the distance to the first point.
                    for (int k = 0; k < pointLists.Count; k++)
                    {
                        pointLists[k].Sort(delegate (UV x, UV y)
                        {
                            if (null == x && null == y) return 0;
                            else if (null == x) return -1;
                            else if (null == y) return 1;

                            if ((x - pointLists[k][0]).GetLength() > (y - pointLists[k][0]).GetLength())
                                return 1;
                            else
                                return -1;
                        });

                        meshPointList[i][j].AddRange(pointLists[k]);
                    }

                    // Add four vertices of mesh into the point list first.
                    for (int k = 0; k < Utility.MESHVERTEXNUMBER; k++)
                    {
                        pointLists[k].Clear();
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// It calculates which mesh (or gap) the given point lies in. If it does lies in a mesh,
        /// the out parameters roughMeshColumnNo & roughMeshRowNo individually indicates the mesh number.
        /// Otherwise, the isInColumnGap and isInRowGap denotes it lies in the neighbor gap of the mesh 
        /// with the No [roughMeshRowNo, roughMeshColumnNo] along length and/or width direction.
        /// This operation is valid when the mesh generator is a square mesh generator.
        /// </summary>
        /// <returns></returns>
        private bool CalculateMeshNo(UV point2D, ref int roughMeshColumnNo, ref int roughMeshRowNo, 
                                                    ref bool isInColumnGap, ref bool isInRowGap)
        {
            if (null == point2D)
                return false;

            if (null == mMeshGenerator)
                return false;

            // The distance between the point and the minimum coordinates of boundingBox along length direction.
            double distanceAlongLengthDirection = point2D.U - mBoundaryRectangle2D[0].U;

            // The distance between the point and the minimum coordinates of boundingBox along width direction.
            double distanceAlongWidthDirection = point2D.V - mBoundaryRectangle2D[0].V;

            // Roughtly calculate which mesh the point may be located along length direction.
            roughMeshColumnNo = (int) Math.Floor(distanceAlongLengthDirection / (mMeshLength + mGap));

            // Roughtly calculate which mesh the point may be located along width direction.
            roughMeshRowNo = (int) Math.Floor(distanceAlongWidthDirection / (mMeshWidth + mGap));

            // Check the U (i.e., X) coordinate value is greater than that of the mesh's right line, 
            // if so, the point lies in the gap, otherwise, it lies in the mesh.  
            if(point2D.U > mMeshGenerator.LineArrayInColumn2D[roughMeshColumnNo*2+1].StartPoint.X)
                if(Math.Abs(point2D.U - mMeshGenerator.LineArrayInColumn2D[roughMeshColumnNo * 2 + 1].StartPoint.X) > Utility.THRESHHOLDVALUE)
                    isInColumnGap = true;

            if (point2D.V > mMeshGenerator.LineArrayInRow2D[roughMeshRowNo*2+1].StartPoint.Y)
                if (Math.Abs(point2D.V - mMeshGenerator.LineArrayInRow2D[roughMeshRowNo * 2 + 1].StartPoint.Y) > Utility.THRESHHOLDVALUE)
                    isInRowGap = true;

            return true;
        }


        /// <summary>
        /// It first get the lines lie in the interval of the start point and end point along U direction.
        /// And then find the intersection points.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="columnIndexStart"></param>
        /// <param name="columnIndexEnd"></param>
        private List<UV> GetIntersectPointListWithColumnLines(UV startPoint, UV endPoint)
        {
            if (null == startPoint || null == endPoint || startPoint == endPoint)
                return null;

            // No processing is needed when they are Coincident points.
            if ((startPoint - endPoint).IsZeroLength())
                return null;

            double uMin = Math.Min(startPoint.U, endPoint.U);
            double uMax = Math.Max(startPoint.U, endPoint.U);

            List<Line2D> lineListColumn2D = new List<Line2D>(mMeshGenerator.LineArrayInColumn2D);

            // Find the first index of the line that lies in the right of the start point.
            int columnIndexStart = lineListColumn2D.FindIndex(
                    delegate (Line2D line)
                    {
                        return line.StartPoint.X > uMin;
                    }
                );

            // Find the first index of the line that lies in the left of the end point.
            // The end index should not be found in the way shown above.
            int columnIndexEnd = lineListColumn2D.FindLastIndex(
                    delegate (Line2D line)
                    {
                        return line.EndPoint.X < uMax;
                    }
                );

            List<UV> intersectPointList = new List<UV>();
            Line2D tempLine = new Line2D(new Point2D(startPoint.U, startPoint.V), new Point2D(endPoint.U, endPoint.V));
            Point2D? intersectPoint;
            for(int i=columnIndexStart; i<=columnIndexEnd; i++)
            {
                intersectPoint = lineListColumn2D[i].IntersectWith(tempLine);
                Contract.Assert(intersectPoint.HasValue);
                if (intersectPoint.HasValue)
                {
                    intersectPointList.Add(new UV(intersectPoint.Value.X, intersectPoint.Value.Y));
                }
            }

            // In this case, the end point lies in the left of the start point along U direction.
            // Therefore, the items of intersection point list should be reversed to make such items
            // arrange along the direction of startPoint->endPoint.
            if (startPoint.U > endPoint.U)
            {
                intersectPointList.Reverse();
            }

            return intersectPointList;
        }


        /// <summary>
        /// It first get the lines lie in the interval of the start point and end point along V direction.
        /// And then find the intersection points.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="columnIndexStart"></param>
        /// <param name="columnIndexEnd"></param>
        private List<UV> GetIntersectPointListWithRowLines(UV startPoint, UV endPoint)
        {
            if (null == startPoint || null == endPoint || startPoint == endPoint)
                return null;

            // No processing is needed when they are Coincident points.
            if ((startPoint - endPoint).IsZeroLength())
                return null;

            double vMin = Math.Min(startPoint.V, endPoint.V);
            double vMax = Math.Max(startPoint.V, endPoint.V);

            List<Line2D> lineListRow2D = new List<Line2D>(mMeshGenerator.LineArrayInRow2D);

            // Find the first index of the line above the start point.
            int rowIndexStart = lineListRow2D.FindIndex(
                    delegate (Line2D line)
                    {
                        return line.StartPoint.Y > vMin;
                    }
                );

            // Find the last index of the line below the end point.
            // The end index should not be found in the way shown above.
            int rowIndexEnd = lineListRow2D.FindLastIndex(
                    delegate (Line2D line)
                    {
                        return line.EndPoint.Y < vMax;
                    }
                );

            List<UV> intersectPointList = new List<UV>();
            Line2D tempLine = new Line2D(new Point2D(startPoint.U, startPoint.V), new Point2D(endPoint.U, endPoint.V));
            Point2D? intersectPoint;
            for (int i = rowIndexStart; i <= rowIndexEnd; i++)
            {
                intersectPoint = lineListRow2D[i].IntersectWith(tempLine);
                Contract.Assert(intersectPoint.HasValue);
                if (intersectPoint.HasValue)
                {
                    intersectPointList.Add(new UV(intersectPoint.Value.X, intersectPoint.Value.Y));
                }
            }

            // In this case, the end point lies in the left of the start point along U direction.
            // Therefore, the items of intersection point list should be reversed to make such items
            // arrange along the direction of startPoint->endPoint.
            if (startPoint.V > endPoint.V)
            {
                intersectPointList.Reverse();
            }

            return intersectPointList;
        }

        /// <summary>
        /// Use binary search method to find the target point.
        /// </summary>
        /// <param name="pointList"></param>
        /// <param name="point"></param>
        /// <param name="comparer"></param>
        private void SearchAndInsert(List<UV> pointList, UV point, PointComparer comparer)
        {
            if (null == pointList || null == comparer)
                return;

            // If the point is not found, a negative number that is the bitwise 
            // complement of the index of the next element that is larger than the point.
            int index = pointList.BinarySearch(point, comparer);

            // If the same value is found, avoid insertion since this point already exists in the point list.
            if (index >= 0 && index <= pointList.Count)
                return;

            if (index < 0)
            {
                pointList.Insert(~index, point);
            }
        }


        /// <summary>
        /// Resort the input intersection point lists through distance to the given start point. 
        /// Usually both input point list are sorted individually.
        /// </summary>
        /// <param name="pointList">Point list consists of sequential points with ascending distance to the first item.</param>
        /// <param name="point">Point that will be inserted.</param>
        private List<UV> InsertPointBasedOnDistance(List<UV> pointListColumn, List<UV> pointListRow, UV startPoint)
        {
            if (null == pointListColumn || null == pointListRow || pointListColumn.Count == 0 || pointListRow.Count == 0)
                return null;

            List<UV> pointListWithShortLength;
            List<UV> pointListWithLongerLength;

            if (pointListColumn.Count > pointListRow.Count)
            {
                pointListWithLongerLength = pointListColumn;
                pointListWithShortLength = pointListRow;
            }
            else
            {
                pointListWithLongerLength = pointListRow;
                pointListWithShortLength = pointListColumn;
            }

            PointComparer comparer = new PointComparer(startPoint);

            // Insert the items of pointListWithShortLength into the pointListWithLongerLength.
            for (int i=0; i< pointListWithShortLength.Count; i++)
            {
                SearchAndInsert(pointListWithLongerLength, pointListWithShortLength[i], comparer);
            }

            return pointListWithLongerLength;
        }


        /// <summary>
        /// Calculate all intersection points between area boundaries
        /// and given meshes. 
        /// </summary>
        /// <param name="meshArrays"></param>
        /// <returns></returns>
        public bool CalculateInsectionPoints()
        {
            pointLists2D = new List<List<UV>>();

            UV startPoint, endPoint;
            
            // Store all intersection points of area edges and mesh lines.
            List<UV> intersectPointListRow;
            List<UV> intersectPointListColumn;
            List<UV> intersectPointList;

            // Traverse each curve loop's vertices along topological direction.
            for (int i=0; i<vertexLists2D.Count; i++)
            {
                pointLists2D[i] = new List<UV>();
                for (int j=0; j<vertexLists2D[i].Count;)
                {
                    // An edge defined by two points.
                    startPoint = vertexLists2D[i][j++];

                    // The end point is the start point at the final iteration.
                    endPoint = vertexLists2D[i][j % vertexLists2D[i].Count];

                    // Get intersect points with column lines and row lines individually.
                    intersectPointListRow = GetIntersectPointListWithRowLines(startPoint, endPoint);
                    intersectPointListColumn = GetIntersectPointListWithColumnLines(startPoint, endPoint);

                    // Sort all the intersect points based on the distance to the start point.
                    intersectPointList = InsertPointBasedOnDistance(intersectPointListRow, intersectPointListColumn, startPoint);

                    // Add the start point and all sorted points into the target point list. These
                    // points are arranged in sequnce along the edge specified by startPoint->endPoint.
                    pointLists2D[i].Add(startPoint);
                    pointLists2D[i].AddRange(intersectPointList);
                }
            }
            
            return true;
        }

        // For each mesh, get the points lie in the mesh. Actually, it's easy to get such information for 
        // intersection points when calculats them in "GetIntersectPointListWithRowLines". However, we 
        // regenerate it here straightly for easy understanding, temporarily ignoring the efficiency. Additionally,
        // the rule for calculating which mesh the point lies in varies when there's no gap between two meshes.  
        public bool GeneratePointListInMesh()
        {
            if (null == pointLists2D || pointLists2D.Count == 0)
                return false;

            int roughMeshColumnNo = -1;
            int roughMeshRowNo = -1;
            bool isInColumnGap = false;
            bool isInRowGap = false;

            for (int i=0; i<pointLists2D.Count; i++)
            {
                for (int j=0; j< pointLists2D[i].Count; j++)
                {
                    if (!CalculateMeshNo(pointLists2D[i][j], ref roughMeshColumnNo, 
                            ref roughMeshRowNo, ref isInColumnGap, ref isInRowGap))
                        continue;

                    // If a vertex lies in gap, this vertex will be out of consideration.
                    if(!isInColumnGap && !isInRowGap)
                    {
                        pointListInMesh[roughMeshRowNo][roughMeshColumnNo].Add(new PointListIndex(i,j));
                    }
                }
            }

            return true;
        }


        public bool PrepareAreaPointLists()
        {
            if (null == mMeshGenerator.MeshArrays || mMeshGenerator.MeshArrays.Length == 0)
                return false;

            // Obtain all intersection points first.
            if(!CalculateInsectionPoints())
                return false;

            // Allocate all intersection point into perticular mesh. Then 
            // all points are rearranged based on mesh. Note that the area  
            // vertex that lies in gap are excluded.
            if (!GeneratePointListInMesh())
                return false;

            // Sort all intersection points and mesh vertices in sequence
            // for each mesh. Till now, two point lists are ready.
            if (!GenerateMeshPointListInArea())
                return false;

            return true;
        }
    }
}
