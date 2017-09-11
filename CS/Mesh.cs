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
    /// It models a square mesh, though a mesh's geometry is usually represented by a rectangle.
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
            // A mesh has only 4 vertices.
            Contract.Assert(vertices.Length == Utility.MESHVERTEXNUMBER);

            this.vertexArray = new XYZ[vertices.Length];
            vertices.CopyTo(this.vertexArray, 0);            
        }

        /// <summary>
        /// The area is the square of side length.
        /// </summary>
        /// <returns></returns>
        double IMeshProvider.GetArea()
        {
            double sideLength = vertexArray[0].DistanceTo(vertexArray[1]);

            return sideLength * sideLength;
        }

        XYZ[] IMeshProvider.GetVertices()
        {
            return (XYZ[])vertexArray.Clone();
        }
    }
}
