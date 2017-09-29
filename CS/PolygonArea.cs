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


        /// <summary>
        /// In addition to all vertices in vertexLists, it includes 
        /// intersection points of the area boundaries and meshes.
        /// </summary>
        private List<List<UV>> pointLists2D;


        /// <summary>
        /// 
        /// </summary>
        private struct PointListIndex
        {
            int curveIndexOfLoop;
            int pointIndexOfCurve;

            // Indicate whether the point is a vertex of this area or not.
            bool isVertex;

            public PointListIndex(int curveIndex, int pointIndex, bool isVertex)
            {
                this.curveIndexOfLoop = curveIndex;
                this.pointIndexOfCurve = pointIndex;
                this.isVertex = isVertex;
            }
        }


        /// <summary>
        /// It stores points lie in specific mesh using the row index and column index
        /// of the pointLists2D. Namely, pointListInMesh[i][j] indicates the points in
        /// the mesh at No.i row & No.j column. Each "PointIndex" stores the 2D indexs 
        /// in pointLists2D/pointLists3D.
        /// </summary> 
        private List<List<List<PointListIndex>>> pointListInMesh;

        private SqureMeshGenerator mMeshGenerator;

        /// <summary>
        /// Gap of meshs paved on the face.
        /// </summary>
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

            Initialize();
        }

        public PolygonArea(Face face, double gap, double meshDimensionU, double meshDimensionV)
        {
            mFace = face;
            mGap = gap;
            mMeshLength = meshDimensionU;
            mMeshWidth = meshDimensionV;

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

        private void Initialize()
        {
            // Return when the boundary is inaccessable.
            if (!GenerateBoundary())
                return;

            mMeshGenerator = new SqureMeshGenerator(mBoundaryRectangle2D, mGap, mMeshLength, mMeshWidth);

            pointListInMesh = new List<List<List<PointListIndex>>>();

            // Initialize the pointListInMesh according to the row number and column number of 2D mesh arrays.
            for(int i=0; i< mMeshGenerator.MeshNumberInRow; i++)
            {
                pointListInMesh.Add(new List<List<PointListIndex>>());
                for(int j=0; j<mMeshGenerator.MeshNumberInColumn; j++)
                {
                    pointListInMesh[i].Add(new List<PointListIndex>());
                }
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
                isInColumnGap = true;

            if (point2D.V > mMeshGenerator.LineArrayInRow2D[roughMeshRowNo * 2 + 1].StartPoint.Y)
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
        private List<Point2D> GetIntersectPointListWithColumnLines(UV startPoint, UV endPoint)
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

            List<Point2D> intersectPointList = new List<Point2D>();
            Line2D tempLine = new Line2D(new Point2D(startPoint.U, startPoint.V), new Point2D(endPoint.U, endPoint.V));
            Point2D? intersectPoint;
            for(int i=columnIndexStart; i<columnIndexEnd; i++)
            {
                intersectPoint = lineListColumn2D[i].IntersectWith(tempLine);
                Contract.Assert(intersectPoint.HasValue);
                intersectPointList.Add(intersectPoint.Value);
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
        private List<Point2D> GetIntersectPointListWithRowLines(UV startPoint, UV endPoint)
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

            List<Point2D> intersectPointList = new List<Point2D>();
            Line2D tempLine = new Line2D(new Point2D(startPoint.U, startPoint.V), new Point2D(endPoint.U, endPoint.V));
            Point2D? intersectPoint;
            for (int i = rowIndexStart; i < rowIndexEnd; i++)
            {
                intersectPoint = lineListRow2D[i].IntersectWith(tempLine);
                Contract.Assert(intersectPoint.HasValue);
                intersectPointList.Add(intersectPoint.Value);
            }

            return intersectPointList;
        }


        /// <summary>
        /// Calculate the intersect points with mesh lines for the line defined by given points.
        /// </summary>
        /// <param name="startPointCurveIndex">The curve index of start point of a curve along the topological direction.</param>
        /// <param name="startPointVertexIndex">The vertex index of start point of a curve along the topological direction.</param>
        /// <param name="endPointRowIndex">The curve index of the end point of a curve along the topological direction.</param>
        /// <param name="endPointColumnIndex">The vertex index of the end point of a curve along the topological direction.</param>
        /// <returns></returns>
        private List<Point2D> IntersectWithMeshLines(int startPointCurveIndex, int startPointVertexIndex, int endPointCurveIndex, int endPointVertexIndex)
        {

            // Exclude illegal inputs first.
            if (startPointCurveIndex < 0 || startPointCurveIndex >= vertexLists2D.Count)
                return null;

            if (endPointCurveIndex < 0 || endPointCurveIndex >= vertexLists2D.Count)
                return null;

            if (startPointVertexIndex <0 || startPointVertexIndex >= vertexLists2D[startPointCurveIndex].Count)
                return null;

            if (endPointVertexIndex < 0 || endPointVertexIndex >= vertexLists2D[endPointCurveIndex].Count)
                return null;
            
            // Both are the same point in this case.
            if (startPointCurveIndex==endPointCurveIndex && startPointVertexIndex==endPointVertexIndex)
                return null;
            
            List<Point2D> intersectPointList = new List<Point2D>();

            int roughMeshColumnNo = -1;
            int roughMeshRowNo = -1;
            bool isInColumnGap = false;
            bool isInRowGap = false;

            // Record the start index and end index of column lines that have 
            // intersections points with given edge defined by the start point
            // and end point above.
            int columnIndexStart = -1;
            int columnIndexEnd = -1;
            int rowIndexStart = -1;
            int rowIndexEnd = -1;

            UV startPoint = vertexLists2D[startPointCurveIndex][startPointVertexIndex];
            UV endPoint = vertexLists2D[endPointCurveIndex][endPointVertexIndex];

            CalculateMeshNo(startPoint, ref roughMeshColumnNo, ref roughMeshRowNo,
                               ref isInColumnGap, ref isInRowGap);

            // If the start point lies neither in row gap nor in column gap, it means that
            // the start point lies in the mesh at specific row and column. Otherwise, the
            // point lies in a gap. Note that a point in gap.
            if (!isInColumnGap && !isInRowGap)
            {
                pointListInMesh[roughMeshRowNo][roughMeshColumnNo].Add(new PointListIndex(startPointCurveIndex, startPointVertexIndex, true));
            }














            return intersectPointList;
        }
   

        /// <summary>
        /// Resort the input point lists  point through distance to the given start point. 
        /// </summary>
        /// <param name="pointList">Point list consists of sequential points with ascending distance to the first item.</param>
        /// <param name="point">Point that will be inserted.</param>
        private List<Point2D> InsertPointBasedOnDistance(List<Point2D> pointListColumn, List<Point2D> pointListRow, Point2D startPoint)
        {
            if (null == pointListColumn || null == pointListRow || pointListColumn.Count == 0 || pointListRow.Count == 0)
                return null;





            int index = pointList.Count / 2;
            double distance = point.DistanceTo(pointList[0]);
            while()
            {
                if(distance < pointList[index].DistanceTo(pointList[0]))
                {
                    index /= 2;
                }
                if(distance > pointList[index].DistanceTo(pointList[0]))
                {
                    index += (pointList.Count - index) / 2;
                }


            }

            return;

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

            pointLists2D = new List<List<UV>>();

            UV startPoint, endPoint;
            
            // Store all intersection points of area edges and mesh lines.
            List<Point2D> intersectPointListRow;
            List<Point2D> intersectPointListColumn;

            // Traverse each curve loop's vertices.
            for (int i=0; i<vertexLists2D.Count; i++)
            {
                pointLists2D[i] = new List<UV>();
                for (int j=0; j<vertexLists2D[i].Count; j++)
                {
                    // An edge defined by two points.
                    startPoint = vertexLists2D[i][j];
                    endPoint = vertexLists2D[i][j+1];

                    intersectPointListRow = GetIntersectPointListWithRowLines(startPoint, endPoint);
                    intersectPointListColumn = GetIntersectPointListWithColumnLines(startPoint, endPoint);

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
