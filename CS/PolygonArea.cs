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

        private SqureMeshGenerator mMeshGenerator;

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
        }

        public PolygonArea(Face face, double gap, double meshDimensionU, double meshDimensionV)
        {
            mFace = face;
            mGap = gap;
            mMeshLength = meshDimensionU;
            mMeshWidth = meshDimensionV;
            GenerateBoundary();
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
        /// Calculate the intersect points with mesh lines for the line defined by given points.
        /// </summary>
        /// <param name="startPoint">The start point of a curve along the topological direction.</param>
        /// <param name="endPoint">The end point of a curve along the topological direction.</param>
        /// <returns></returns>
        private List<Point2D> IntersectWithMeshLines(UV startPoint, UV endPoint)
        {
            if (null == startPoint || null == endPoint)
                return null;

            // Both are the same point.
            if (startPoint.Equals(endPoint))
                return null;

            List<Point2D> intersectPointList = new List<Point2D>;

            int roughMeshColumnNo = -1;
            int roughMeshRowNo = -1;
            bool isInColumnGap = false;
            bool isInRowGap = false;

            // Record the start index and end index of column lines that have 
            // intersections points with given edge defined by the start point
            // and end point above.
            int columnIndexStart, columnIndexEnd;
            int rowIndexStart, rowIndexEnd;

            CalculateMeshNo(startPoint, ref roughMeshColumnNo, ref roughMeshRowNo,
                               ref isInColumnGap, ref isInRowGap);

            // Determine the first column line that will intersect with the line startPoint->endPoint.
            if (isInColumnGap)
                columnIndexStart = roughMeshColumnNo * 2 + 2;
            else
                columnIndexStart = roughMeshColumnNo * 2 + 1;

            // Determine the first row line that will intersect with the line startPoint->endPoint.
            if (isInRowGap)
                rowIndexStart = roughMeshRowNo * 2 + 2;
            else
                rowIndexStart = roughMeshRowNo * 2 + 1;


            CalculateMeshNo(endPoint, ref roughMeshColumnNo, ref roughMeshRowNo,
                                ref isInColumnGap, ref isInRowGap);

            // Determine the end column line that will intersect with the line startPoint->endPoint.
            // Please take care of the index calculation which is different to that of start point.
            if (isInColumnGap)
                columnIndexEnd = roughMeshColumnNo * 2 + 1;
            else
                columnIndexEnd = roughMeshColumnNo * 2;

            // Determine the end row line that will intersect with the line startPoint->endPoint.
            if (isInRowGap)
                rowIndexEnd = roughMeshRowNo * 2 + 1;
            else
                rowIndexEnd = roughMeshRowNo * 2;









            return intersectPointList;
        }
   

        /// <summary>
        /// Insert the point into the point list based on the distance to the point 
        /// list's first item. The items in point list are sorted,  from lowest to highest.  
        /// </summary>
        /// <param name="pointList">Point list consists of sequential points with ascending distance to the first item.</param>
        /// <param name="point">Point that will be inserted.</param>
        private void InsertPointBasedOnDistance(List<Point2D> pointList, Point2D point)
        {
            if (null == pointList)
                return;

            // Direct add the point if there is only one or no point in the list.
            if (pointList.Count <= 1)
            {
                pointList.Add(point);
                return;
            }

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
            

            int roughMeshColumnNo = -1;
            int roughMeshRowNo = -1;
            bool isInColumnGap = false;
            bool isInRowGap = false;

            // Used to indicate a edge of the area
            UV startPoint, endPoint;

            Point2D? intersectPoint;

            // Record the start index and end index of column lines that have 
            // intersections points with given edge defined by the start point
            // and end point above.
            int columnIndexStart, columnIndexEnd;
            int rowIndexStart, rowIndexEnd;
            
            // Store all intersection points of area edges and mesh lines.
            List<Point2D> intersectPoints = new List<Point2D>();

            Line2D edgeLine;
            // Traverse each curve loop's vertices.
            for (int i=0; i<vertexLists2D.Count; i++)
            {
                pointLists2D[i] = new List<UV>();
                for (int j=0; j<vertexLists2D[i].Count; j++)
                {
                    startPoint = vertexLists2D[i][j];
                    endPoint = vertexLists2D[i][j+1];



                    CalculateMeshNo(startPoint, ref roughMeshColumnNo, ref roughMeshRowNo,
                                                   ref isInColumnGap, ref isInRowGap);

                    // Determine the first column line that will intersect with the line startPoint->endPoint.
                    if (isInColumnGap)
                        columnIndexStart = roughMeshColumnNo * 2 + 2;
                    else
                        columnIndexStart = roughMeshColumnNo * 2 + 1;

                    // Determine the first row line that will intersect with the line startPoint->endPoint.
                    if (isInRowGap)
                        rowIndexStart = roughMeshRowNo * 2 + 2;
                    else
                        rowIndexStart = roughMeshRowNo * 2 + 1;


                    CalculateMeshNo(endPoint, ref roughMeshColumnNo, ref roughMeshRowNo,
                               ref isInColumnGap, ref isInRowGap);

                    // Determine the end column line that will intersect with the line startPoint->endPoint.
                    // Please take care of the index calculation which is different to that of start point.
                    if (isInColumnGap)
                        columnIndexEnd = roughMeshColumnNo * 2 + 1;
                    else
                        columnIndexEnd = roughMeshColumnNo * 2;

                    // Determine the end row line that will intersect with the line startPoint->endPoint.
                    if (isInRowGap)
                        rowIndexEnd = roughMeshRowNo * 2 + 1;
                    else
                        rowIndexEnd = roughMeshRowNo * 2;

                    // Get the lower 
                    int indexLower = Math.Min(columnIndexStart, columnIndexEnd);
                    int indexHigher = Math.Max(columnIndexStart, columnIndexEnd);



                    edgeLine = new Line2D(new Point2D(startPoint.U, startPoint.V), new Point2D(endPoint.U, endPoint.V));

                    pointLists2D[i].Add(startPoint);
                    // If the start point is on the first mesh line, there is no need to calculate for the first mesh line.    
                    if (Math.Abs(startPoint.U - mMeshGenerator.LineArrayInColumn2D[columnIndexStart].StartPoint.X) < Utility.THRESHHOLDVALUE)
                    {
                        columnIndexStart++;
                    }



                    for (int k = indexLower; k <= indexHigher; k++)
                    {
                        intersectPoint = mMeshGenerator.LineArrayInColumn2D[k].IntersectWith(edgeLine);
                        // This intersection point should not be null;
                        Contract.Assert(null != intersectPoint);

                        // The intersection point is naturally added in sequence based on distance to the start point.
                        intersectPoints.Add((Point2D) intersectPoint);
                    }

                    // If the start point is on the first mesh line, there is no need to calculate for the first mesh line.    
                    if (Math.Abs(startPoint.V - mMeshGenerator.LineArrayInRow2D[rowIndexStart].StartPoint.X) < Utility.THRESHHOLDVALUE)
                    {
                        rowIndexStart++;
                    }

                    for (int k = rowIndexStart; k <= rowIndexEnd; k++)
                    {
                        intersectPoint = mMeshGenerator.LineArrayInRow2D[k].IntersectWith(edgeLine);
                        // This intersection point should not be null;
                        Contract.Assert(null != intersectPoint);


                        intersectPoints.Add((Point2D)intersectPoint);
                    }





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
