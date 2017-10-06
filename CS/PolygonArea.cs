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

    class PointComparer : IComparer<PointStruct>
    {
        private UV basePoint;

        public PointComparer(UV basePoint)
        {
            this.basePoint = basePoint;     
        }

        int IComparer<PointStruct>.Compare(PointStruct x, PointStruct y)
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
                    if (x.Point == y.Point || x == y)
                        return 0;

                    if (x.Point.IsAlmostEqualTo(y.Point, Utility.THRESHHOLDVALUE))
                        return 0;

                    if (x.Point.DistanceTo(basePoint) < y.Point.DistanceTo(basePoint))
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
    enum PointFeature { Vertex, Intersection, VertexAndIntersection };


    class PointStruct
    {
        private UV mPoint;

        /// <summary>
        /// A point can be a vertex, an intersection point only or an intersection point coincident with a vertex.
        /// </summary>  

        private PointFeature mFeature;

        public PointStruct(UV point, PointFeature feature)
        {
            mPoint = point;
            mFeature = feature;
        }

        public UV Point
        {
            get {return mPoint;}
        }

        public PointFeature Feature
        {
            get { return mFeature; }
            set { mFeature = value; }
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
        private List<List<PointStruct>> pointLists2D;
        

        /// <summary>
        /// It stores the points (that belong to area boundary) lie in specific mesh using 
        /// the row index and column index of the pointLists2D. Namely, pointListInMesh[i][j]  
        /// indicates the points on area curves in the mesh at No.i row & No.j column.  
        /// Each "PointIndex" stores the 2D indexs in pointLists2D/pointLists3D.
        /// </summary> 
        private List<List<List<PointStruct>>> areaPointListInMesh;


        /// <summary>
        /// Store the mesh vertices that lie in area, and the intersection points
        /// on four mesh sides (Note: not in the mesh).
        /// </summary>
        private List<List<List<PointStruct>>> meshPointList;


        private SqureMeshGenerator mMeshGenerator;

        /// <summary>
        /// Gap of meshs paved on the face.
        /// </summary>
        private double mGap;


        private double mMeshLength;
        private double mMeshWidth;


        private enum Direction2D { UDirection, VDirection };


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


        public List<List<List<PointStruct>>> AreaPointListInMesh
        {
            get
            {
                return areaPointListInMesh;
            }
        }

        public List<List<List<PointStruct>>> MeshPointList
        {
            get
            {
                return meshPointList;
            }
        }

        public List<List<PointStruct>> AreaVertexAndIntersectionPointLists2D
        {
            get { return pointLists2D; }
        }

        private void Initialize()
        {
            // Return when the boundary is inaccessible.
            if (!GenerateBoundary())
                return;

            mMeshGenerator = new SqureMeshGenerator(mBoundaryRectangle2D, mGap, mMeshLength, mMeshWidth);
            mMeshGenerator.GenerateMeshLines();

            areaPointListInMesh = new List<List<List<PointStruct>>>();
            meshPointList = new List<List<List<PointStruct>>>();

            // Initialize the pointListInMesh according to the row number and column number of 2D mesh arrays.
            for (int i=0; i< mMeshGenerator.MeshNumberInRow; i++)
            {
                areaPointListInMesh.Add(new List<List<PointStruct>>());
                meshPointList.Add(new List<List<PointStruct>>());
                for (int j=0; j<mMeshGenerator.MeshNumberInColumn; j++)
                {
                    areaPointListInMesh[i].Add(new List<PointStruct>());
                    meshPointList[i].Add(new List<PointStruct>());
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
        /// <param name="isCoincidentWithMeshVertex">Return true if the input point is coincident with a mesh vertex.</param>
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
                    return i;
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
            if (null == mMeshGenerator)
                return false;

            
            bool isCoincidentWithVertex = false;
            int sideIndex = -1;

            // Use to store points on each mesh edge. No.i vertex belongs to
            // No.i edge.   
            List<List<PointStruct>> pointLists = new List<List<PointStruct>>(Utility.MESHVERTEXNUMBER); 
            for(int i=0; i<Utility.MESHVERTEXNUMBER; i++)
            {
                pointLists.Add(new List<PointStruct>());
            }


            for (int i=0; i<mMeshGenerator.MeshNumberInRow; i++)
            {
                for(int j=0; j<mMeshGenerator.MeshNumberInColumn; j++)
                {      
                    // Add four vertices of mesh into the point list first to keep the start vertex of each side
                    // to be stored at the first index.
                    for (int k = 0; k < Utility.MESHVERTEXNUMBER; k++)
                    {
                        pointLists[k].Add(new PointStruct(mMeshGenerator.MeshArrays[i, j].GetVertex2D(k), PointFeature.Vertex));
                    }
                    PointStruct pointStruct;
                    for (int k = 0; k < areaPointListInMesh[i][j].Count; i++)
                    {
                        pointStruct = areaPointListInMesh[i][j][k];
                        sideIndex = GetMeshSideThroughPoint(pointStruct.Point, mMeshGenerator.MeshArrays[i, j], ref isCoincidentWithVertex);

                        // Ignore the vertex point since they already exists in the mesh point list.
                        if (isCoincidentWithVertex)
                        {
                            pointStruct.Feature = PointFeature.VertexAndIntersection;
                            continue;
                        }

                        // Add intersection point without considering the sequence.
                        // Namely, points on each side maybe unordered.
                        if (sideIndex >= 0 && sideIndex <= Utility.MESHVERTEXNUMBER)
                        {
                            pointLists[sideIndex].Add(pointStruct);
                        }
                    }

                    // Sort the items in point list based on the distance to the first point,
                    // i.e, the start vertex of each mesh side.
                    for (int k = 0; k < pointLists.Count; k++)
                    {
                        pointLists[k].Sort(delegate(PointStruct x, PointStruct y)
                        {
                            if (null == x.Point && null == y.Point) return 0;
                            else if (null == x.Point) return -1;
                            else if (null == y.Point) return 1;
                            else if (x.Point == y.Point || x.Point.DistanceTo(y.Point) < Utility.THRESHHOLDVALUE) return 0;

                            if ((x.Point - pointLists[k][0].Point).GetLength() > (y.Point - pointLists[k][0].Point).GetLength())
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

            // Roughly calculate which mesh the point may be located along length direction.
            roughMeshColumnNo = (int) Math.Floor(distanceAlongLengthDirection / (mMeshLength + mGap));

            // Roughly calculate which mesh the point may be located along width direction.
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
        /// Calculate intersection points with specific line list for the line defined by the
        /// start point and end point. Please note that the intersection points should be sorted
        /// in sequence with the topological direction of startPoint->endPoint.
        /// </summary>
        /// <returns></returns>  
        private List<PointStruct> IntersectWithLines(List<Line2D> lineList, UV startPoint, UV endPoint, 
                                                    Direction2D direction, ref bool startIsIncluded)
        {
            if (null == lineList || lineList.Count == 0 || null == startPoint || 
                              null == endPoint || startPoint == endPoint)
                return null;

            // No processing is needed when they are Coincident points.
            if ((startPoint - endPoint).IsZeroLength())
                return null;

            double minValue;
            double maxValue;
            if (Direction2D.UDirection == direction)
            {
                minValue = Math.Min(startPoint.U, endPoint.U);
                maxValue = Math.Max(startPoint.U, endPoint.U);
            }
            else
            {
                minValue = Math.Min(startPoint.V, endPoint.V);
                maxValue = Math.Max(startPoint.V, endPoint.V);
            }

            List<PointStruct> intersectPointList = new List<PointStruct>();

            // If both points are parallel with specific direction,
            // check the startPoint is lie on a line or not, if yes,
            // add the start point and remark that the start point is included.
            // In this context, both points are crossed by the same line. 
            if (Math.Abs(minValue - maxValue) < Utility.THRESHHOLDVALUE)
            {
                int index = lineList.FindIndex(
                        delegate (Line2D line)
                        {
                            return Math.Abs(line.StartPoint.X - minValue) < Utility.THRESHHOLDVALUE;
                        }
                   );

                if (-1 != index)
                {
                    intersectPointList.Add(new PointStruct(startPoint, PointFeature.VertexAndIntersection));
                    startIsIncluded = true;
                }
                return intersectPointList;    
            }
                        
            // Find the first index of the line that lies in the right of the start point.
            int indexStart = -1;
            int indexEnd = -1;
            if (Direction2D.UDirection == direction)
            {
                indexStart = lineList.FindIndex(
                        delegate(Line2D line)
                        {
                            return line.StartPoint.X > minValue;
                        }
                    );

                // Find the first index of the line that lies in the left of the end point.
                // The end index should not be found in the way shown above since the end
                // point should not be added at the same time. 
                indexEnd = lineList.FindLastIndex(
                        delegate(Line2D line)
                        {
                            return line.EndPoint.X < maxValue;
                        }
                    );
            }
            else
            {
                indexStart = lineList.FindIndex(
                        delegate(Line2D line)
                        {
                            return line.StartPoint.Y > minValue;
                        }
                    );

                // Find the last index of the line below the end point.
                // The end index should not be found in the way shown above.
                indexEnd = lineList.FindLastIndex(
                        delegate(Line2D line)
                        {
                            return line.EndPoint.Y < maxValue;
                        }
                    );
            }

            Line2D tempLine = new Line2D(new Point2D(startPoint.U, startPoint.V), new Point2D(endPoint.U, endPoint.V));
            Point2D? intersectPoint;
            for (int i = indexStart; i <= indexEnd; i++)
            {
                intersectPoint = lineList[i].IntersectWith(tempLine);
                Contract.Assert(intersectPoint.HasValue);
                if (intersectPoint.HasValue)
                {
                    intersectPointList.Add(new PointStruct(new UV(intersectPoint.Value.X, intersectPoint.Value.Y), PointFeature.Intersection));
                }
            }

            if (intersectPointList.Count == 0)
                return intersectPointList;

            // In routine context, find the last line index that is less than the start point, 
            // and the first line index that is greater than the end point to check whether  
            // the start point is crossed by a line, i.e., it is also a intersection point.  
            // In this context, it handles the case that the start point is crossed by a line.
            int firstIndexGreaterThanStartPoint = -1;
            int lastIndexLessThanStartPoint = -1;

            if (Direction2D.UDirection == direction)
            {
                if (minValue == startPoint.U)
                {
                    // Check whether the start point lies on a line or not. Please note that usually the
                    // firstIndexGreaterThanStartPoint is greater. However, if the start point lies in the
                    // first line, phisically the start point's coordinate maybe less though it's not right
                    // in logic.
                    firstIndexGreaterThanStartPoint = indexStart;
                    if (lineList[firstIndexGreaterThanStartPoint - 1].StartPoint.X == startPoint.U)
                    {
                        intersectPointList.Insert(0, new PointStruct(startPoint, PointFeature.VertexAndIntersection));
                        startIsIncluded = true;
                    }

                    // Check whether the start point is nearly coincident with the nearest intersection point computed.
                    // If yes, ramark that the start point is actually included.
                    if (!startIsIncluded && intersectPointList[0].Point.DistanceTo(startPoint) < Utility.THRESHHOLDVALUE)
                    {
                        intersectPointList[0].Feature = PointFeature.VertexAndIntersection;
                        startIsIncluded = true;
                    }
                }
                if (maxValue == startPoint.U)
                {
                    // Check whether the start point lies on a line or not. 
                    lastIndexLessThanStartPoint = indexEnd;
                    if (lineList[lastIndexLessThanStartPoint + 1].StartPoint.X == startPoint.U)
                    {
                        intersectPointList.Add(new PointStruct(startPoint, PointFeature.VertexAndIntersection));
                        startIsIncluded = true;
                    }

                    // Check whether the start point is nearly coincident with the nearest intersection point computed.
                    if (!startIsIncluded && intersectPointList[intersectPointList.Count - 1].Point.DistanceTo(startPoint) < Utility.THRESHHOLDVALUE)
                    {
                        intersectPointList[intersectPointList.Count - 1].Feature = PointFeature.VertexAndIntersection;
                        startIsIncluded = true;
                    }
                }
            }
            else
            {
                if (minValue == startPoint.V)
                {
                    // Check whether the start point lies on a line or not. 
                    firstIndexGreaterThanStartPoint = indexStart;
                    if (lineList[firstIndexGreaterThanStartPoint - 1].StartPoint.Y == startPoint.V)
                    {
                        intersectPointList.Insert(0, new PointStruct(startPoint, PointFeature.VertexAndIntersection));
                        startIsIncluded = true;
                    }


                    // Check whether the start point is nearly coincident with the nearest intersection point computed.
                    if (!startIsIncluded && intersectPointList[0].Point.DistanceTo(startPoint) < Utility.THRESHHOLDVALUE)
                    {
                        intersectPointList[0].Feature = PointFeature.VertexAndIntersection;
                        startIsIncluded = true;
                    }
                }
                if (maxValue == startPoint.V)
                {
                    // Check whether the start point lies on a line or not. 
                    lastIndexLessThanStartPoint = indexEnd;
                    if (lineList[lastIndexLessThanStartPoint + 1].StartPoint.Y == startPoint.V)
                    {
                        intersectPointList.Add(new PointStruct(startPoint, PointFeature.VertexAndIntersection));
                        startIsIncluded = true;
                    }

                    // Check whether the start point is nearly coincident with the nearest intersection point computed.
                    if (!startIsIncluded && intersectPointList[intersectPointList.Count - 1].Point.DistanceTo(startPoint) < Utility.THRESHHOLDVALUE)
                    {
                        intersectPointList[intersectPointList.Count - 1].Feature = PointFeature.VertexAndIntersection;
                        startIsIncluded = true;
                    }
                }
            }          

            // In this case, the coordinates of end point is less than that of the start point 
            // along U/V direction. Therefore, the items of intersection point list should be 
            // reversed to make such items arrange along the topological direction of startPoint->endPoint.
            if ((Direction2D.UDirection == direction && startPoint.U > endPoint.U) ||
                (Direction2D.VDirection == direction && startPoint.V > endPoint.V))
            {
                intersectPointList.Reverse();
            }

            return intersectPointList;
        }

        
        /// <summary>
        /// It first get the lines lie in the interval of the start point and end point along U direction.
        /// And then find the intersection points.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="columnIndexStart"></param>
        /// <param name="columnIndexEnd"></param>
        [Obsolete("This method is obsolete; use method 2 instead")]
        private List<PointStruct> GetIntersectPointListWithColumnLines1(UV startPoint, UV endPoint, ref bool startIsIncluded)
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
                        if (Math.Abs(line.StartPoint.X - uMin) < Utility.THRESHHOLDVALUE)
                            return true;

                        return line.StartPoint.X > uMin;
                    }
                );

            // Find the first index of the line that lies in the left of the end point.
            // The end index should not be found in the way shown above.
            int columnIndexEnd = lineListColumn2D.FindLastIndex(
                    delegate (Line2D line)
                    {
                        if (Math.Abs(line.EndPoint.X - uMax) < Utility.THRESHHOLDVALUE)
                            return true;

                        return line.EndPoint.X < uMax;
                    }
                );

            List<PointStruct> intersectPointList = new List<PointStruct>();
            Line2D tempLine = new Line2D(new Point2D(startPoint.U, startPoint.V), new Point2D(endPoint.U, endPoint.V));
            Point2D? intersectPoint;
            for(int i=columnIndexStart; i<=columnIndexEnd; i++)
            {
                intersectPoint = lineListColumn2D[i].IntersectWith(tempLine);
                Contract.Assert(intersectPoint.HasValue);
                if (intersectPoint.HasValue)
                {
                    intersectPointList.Add(new PointStruct(new UV(intersectPoint.Value.X, intersectPoint.Value.Y), PointFeature.Intersection));
                }
            }

            // If the start point is coincident with either the first  or the end intersection point, reset 
            // the intersection point feature. 
            if (intersectPointList[0].Point.DistanceTo(startPoint) < Utility.THRESHHOLDVALUE)
            {
                intersectPointList[0].Feature = PointFeature.VertexAndIntersection;
                startIsIncluded = true;
            }

            if (intersectPointList[intersectPointList.Count - 1].Point.DistanceTo(startPoint) < Utility.THRESHHOLDVALUE)
            {
                intersectPointList[intersectPointList.Count - 1].Feature = PointFeature.VertexAndIntersection;
                startIsIncluded = true;

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


        private List<PointStruct> GetIntersectPointListWithColumnLines2(UV startPoint, UV endPoint, ref bool startIsIncluded)
        {
            List<Line2D> lineListColumn2D = new List<Line2D>(mMeshGenerator.LineArrayInColumn2D);
            return IntersectWithLines(lineListColumn2D, startPoint, endPoint, Direction2D.UDirection, ref startIsIncluded);
        }


        /// <summary>
        /// It first get the lines lie in the interval of the start point and end point along V direction.
        /// And then find the intersection points.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="columnIndexStart"></param>
        /// <param name="columnIndexEnd"></param>
        [Obsolete("This method is obsolete; use method 2 instead")]
        private List<PointStruct> GetIntersectPointListWithRowLines1(UV startPoint, UV endPoint, ref bool startIsIncluded)
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
                        if (Math.Abs(line.StartPoint.Y - vMin) < Utility.THRESHHOLDVALUE)
                            return true;

                        return line.StartPoint.Y > vMin;
                    }
                );

            // Find the last index of the line below the end point.
            // The end index should not be found in the way shown above.
            int rowIndexEnd = lineListRow2D.FindLastIndex(
                    delegate (Line2D line)
                    {
                        if (Math.Abs(line.EndPoint.Y - vMax) < Utility.THRESHHOLDVALUE)
                            return true;

                        return line.EndPoint.Y < vMax;
                    }
                );

            List<PointStruct> intersectPointList = new List<PointStruct>();
            Line2D tempLine = new Line2D(new Point2D(startPoint.U, startPoint.V), new Point2D(endPoint.U, endPoint.V));
            Point2D? intersectPoint;
            for (int i = rowIndexStart; i <= rowIndexEnd; i++)
            {
                intersectPoint = lineListRow2D[i].IntersectWith(tempLine);
                Contract.Assert(intersectPoint.HasValue);
                if (intersectPoint.HasValue)
                {
                    intersectPointList.Add(new PointStruct(new UV(intersectPoint.Value.X, intersectPoint.Value.Y), PointFeature.Intersection));
                }
            }

            // If the start point is coincident with either the first  or the end intersection point, reset its feature. 
            if (intersectPointList[0].Point.DistanceTo(startPoint) < Utility.THRESHHOLDVALUE)
            {
                intersectPointList[0].Feature = PointFeature.VertexAndIntersection;
                startIsIncluded = true;
            }

            if (intersectPointList[intersectPointList.Count - 1].Point.DistanceTo(startPoint) < Utility.THRESHHOLDVALUE)
            {
                intersectPointList[intersectPointList.Count - 1].Feature = PointFeature.VertexAndIntersection;
                startIsIncluded = true;

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


        private List<PointStruct> GetIntersectPointListWithRowLines2(UV startPoint, UV endPoint, ref bool startIsIncluded)
        {
            List<Line2D> lineListColumn2D = new List<Line2D>(mMeshGenerator.LineArrayInRow2D);
            return IntersectWithLines(lineListColumn2D, startPoint, endPoint, Direction2D.VDirection, ref startIsIncluded);
        }


        /// <summary>
        /// Use binary search method to find the target point.
        /// </summary>
        /// <param name="pointList"></param>
        /// <param name="point"></param>
        /// <param name="comparer"></param>
        private void SearchAndInsert(List<PointStruct> pointList, PointStruct point, PointComparer comparer)
        {
            if (null == pointList || null == comparer || null == point)
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
        private List<PointStruct> InsertPointBasedOnDistance(List<PointStruct> pointListColumn, List<PointStruct> pointListRow, UV startPoint)
        {
            if (null == pointListColumn || null == pointListRow || pointListColumn.Count == 0 || pointListRow.Count == 0)
                return null;

            List<PointStruct> pointListWithShortLength;
            List<PointStruct> pointListWithLongerLength;

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
            pointLists2D = new List<List<PointStruct>>();

            UV startPoint, endPoint;
            
            // Store all intersection points of area edges and mesh lines.
            List<PointStruct> intersectPointListRow;
            List<PointStruct> intersectPointListColumn;
            List<PointStruct> intersectPointList;
            bool startIsIncluded = false;

            // Traverse each curve loop's vertices along topological direction.
            for (int i=0; i<vertexLists2D.Count; i++)
            {
                pointLists2D[i] = new List<PointStruct>();
                for (int j=0; j<vertexLists2D[i].Count;)
                {
                    // An edge defined by two points.
                    startPoint = vertexLists2D[i][j++];

                    // The end point is the start point at the final iteration.
                    endPoint = vertexLists2D[i][j % vertexLists2D[i].Count];

                    // Get intersect points with column lines and row lines individually.
                    intersectPointListRow = GetIntersectPointListWithRowLines2(startPoint, endPoint, ref startIsIncluded);
                    intersectPointListColumn = GetIntersectPointListWithColumnLines2(startPoint, endPoint, ref startIsIncluded);

                    // Sort all the intersect points based on the distance to the start point.
                    intersectPointList = InsertPointBasedOnDistance(intersectPointListRow, intersectPointListColumn, startPoint);

                    // Add the start point and all sorted points into the target point list. These
                    // points are arranged in sequence along the edge specified by startPoint->endPoint.
                    // If the start point is not included during intersection calculation, it should be 
                    // added into the point list as a simple vertex. Otherwise, the start point has been
                    // added into the intersection point list, there is no need to add it repeatedly.
                    if (!startIsIncluded)
                        pointLists2D[i].Add(new PointStruct(startPoint, PointFeature.Vertex));
                    
                    pointLists2D[i].AddRange(intersectPointList);
                }
            }
            
            return true;
        }

        // For each mesh, get the points lie in the mesh. Actually, it's easy to get such information for 
        // intersection points when calculates them in "GetIntersectPointListWithRowLines". However, we 
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
                    if (!CalculateMeshNo(pointLists2D[i][j].Point, ref roughMeshColumnNo, 
                            ref roughMeshRowNo, ref isInColumnGap, ref isInRowGap))
                        continue;

                    // If a point lies in gap, this point will be out of consideration.
                    if(!isInColumnGap && !isInRowGap)
                    {
                        areaPointListInMesh[roughMeshRowNo][roughMeshColumnNo].Add(pointLists2D[i][j]);
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

            // Allocate all intersection point into particular mesh. Then 
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
