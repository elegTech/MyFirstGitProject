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
    /// <summary>
    /// This is a mesh interface.
    /// </summary>
    interface IMeshProvider
    {
        /// <summary>
        /// Get mesh vertices, which depends on detailed implementation. 
        /// As a protocol, note that the vertices should be in anticlockwise sequence.
        /// </summary>
        /// <returns>The vertices of the mesh.</returns>
        XYZ[] GetVertices();

        /// <summary>
        /// Get mesh area.
        /// </summary>
        /// <returns>The area of the mesh</returns>
        double GetArea();         
    }

    /// <summary>
    /// It models a rectangle mesh, though a mesh's geometry is usually represented 
    /// by a square. In this context, the bottom left vertex is No.0, the bottom right  
    /// vertex is No.1, the top right vertex is No.2, and the top left vertex is No.3.
    /// Such vertex coding rule is consistent with the usual guideline.
    /// 3 ........ 2
    ///   .      .
    ///   .      .
    /// 0 ........ 1
    /// </summary>
    public class SquareMesh:IMeshProvider
    {
        /// <summary>
        /// Four vertices of a square mesh. VertexArray[0] represents the bottom left vertex.
        /// VertexArray[1] represents the bottom right vertex.
        /// </summary>
        private XYZ[] vertexArray;

        /// <summary>
        /// Please note that the input vertex list should be in anticlockwise sequence 
        /// to obey interface protocol.
        /// </summary>
        /// <param name="vertices"></param>
        public SquareMesh(XYZ[] vertices)
        {
            // A mesh has specific vertices.
            Contract.Assert(vertices.Length == Utility.MESHVERTEXNUMBER);

            vertexArray = new XYZ[Utility.MESHVERTEXNUMBER];
            vertices.CopyTo(vertexArray, 0);            
        }


        /// <summary>
        /// Create a new square mesh using location (No.0 vertex), length and width parameters.
        /// The lengthDirection represents the dimension No.0 vertex -> No.1 vertex, while the 
        /// widthDirection the dimension No.1 -> No.2 vertex.
        /// </summary>
        /// <param name="location">The position of No.0 vertex of the mesh.</param>
        /// <param name="lengthDirection">The direction that No.0 vertex points to No.1 vertex.</param>
        /// <param name="widthDirection">The direction that No.1 vertex points to No.2 vertex.</param>
        /// <param name="length">The dimension along lengthDirection.</param>
        /// <param name="width">The dimension along widthDirection.</param>
        public SquareMesh(XYZ location, XYZ lengthDirection, XYZ widthDirection, double length, double width)
        {
            Contract.Assert(null != location && null != lengthDirection && null != widthDirection && length > 0 && width > 0);

            vertexArray = new XYZ[Utility.MESHVERTEXNUMBER];

            vertexArray[0] = new XYZ(location.X, location.Y, location.Z);
            vertexArray[1] = new XYZ(location.X, location.Y, location.Z) + lengthDirection.Normalize() * length;
            vertexArray[2] = new XYZ(vertexArray[1].X, vertexArray[1].Y, vertexArray[1].Z) + widthDirection.Normalize() * width;
            vertexArray[3] = new XYZ(vertexArray[0].X, vertexArray[0].Y, vertexArray[0].Z) + widthDirection.Normalize() * width;
        }


        public XYZ GetVertex(int vertexNo)
        {
            Contract.Assert(vertexNo > 0 && vertexNo < Utility.MESHVERTEXNUMBER);
            return vertexArray[vertexNo];
        }

        /// <summary>
        /// The side length multiplies the width is the area.
        /// </summary>
        /// <returns></returns>
        double IMeshProvider.GetArea()
        {
            double length = vertexArray[0].DistanceTo(vertexArray[1]);
            double width = vertexArray[1].DistanceTo(vertexArray[2]);

            return length * width;
        }

        /// <summary>
        /// Returning the clone of vertices prevents modification of original data since it's private. 
        /// </summary>
        /// <returns>Clone of vetices</returns>
        XYZ[] IMeshProvider.GetVertices()
        {
            return (XYZ[])vertexArray.Clone();
        }
    }
}
