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
    interface IMeshElement
    {
        #region Data accessor

        /// <summary>
        /// Get mesh vertices, which depends on detailed implementation. 
        /// As a protocol, note that the vertices should be in anticlockwise sequence.
        /// </summary>
        /// <returns>The vertices of the mesh.</returns>
        XYZ[] GetVertices3D();
        UV[] GetVertices2D();

        XYZ GetVertex3D(int number);
        UV GetVertex2D(int number);

        XYZ GetMeshCenter3D();
        UV GetMeshCenter2D();

        #endregion

        #region Interface operations
       
        /// <summary>
        /// Get mesh area.
        /// </summary>
        /// <returns>The area of the mesh</returns>
        double GetArea();
        
        #endregion
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
    public class SquareMesh : IMeshElement
    {
        #region Private variables.

        /// <summary>
        /// Four vertices of a square mesh. VertexArray[0] represents the bottom left vertex.
        /// VertexArray[1] represents the bottom right vertex.
        /// </summary>
        private XYZ[] vertex3DArray;

        private UV[] vertex2DArray;

        #endregion

        #region Constructors
        /// <summary>
        /// Please note that the input vertex list should be in anticlockwise sequence 
        /// to obey interface protocol.
        /// </summary>
        /// <param name="vertices"></param>
        public SquareMesh(XYZ[] vertices)
        {
            // A mesh has specific vertices.
            Contract.Assert(vertices.Length == Utility.MESHVERTEXNUMBER);

            vertex3DArray = new XYZ[Utility.MESHVERTEXNUMBER];
            vertices.CopyTo(vertex3DArray, 0);            
        }


        public SquareMesh(UV[] vertices)
        {
            // A mesh has specific vertices.
            Contract.Assert(vertices.Length == Utility.MESHVERTEXNUMBER);

            vertex2DArray = new UV[Utility.MESHVERTEXNUMBER];
            vertices.CopyTo(vertex2DArray, 0);
        }

        


        /// <summary>
        /// Create a new 3D square mesh using location (No.0 vertex), length and width parameters.
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

            vertex3DArray = new XYZ[Utility.MESHVERTEXNUMBER];

            vertex3DArray[0] = new XYZ(location.X, location.Y, location.Z);
            vertex3DArray[1] = new XYZ(location.X, location.Y, location.Z) + lengthDirection.Normalize() * length;
            vertex3DArray[2] = new XYZ(vertex3DArray[1].X, vertex3DArray[1].Y, vertex3DArray[1].Z) + widthDirection.Normalize() * width;
            vertex3DArray[3] = new XYZ(vertex3DArray[0].X, vertex3DArray[0].Y, vertex3DArray[0].Z) + widthDirection.Normalize() * width;
        }

        /// <summary>
        /// Create a new 2D square mesh using location (No.0 vertex), length and width parameters.
        /// The lengthDirection represents the dimension No.0 vertex -> No.1 vertex, while the 
        /// widthDirection the dimension No.1 -> No.2 vertex.
        /// </summary>
        /// <param name="location">The position of No.0 vertex of the mesh.</param>
        /// <param name="lengthDirection">The direction that No.0 vertex points to No.1 vertex.</param>
        /// <param name="widthDirection">The direction that No.1 vertex points to No.2 vertex.</param>
        /// <param name="length">The dimension along lengthDirection.</param>
        /// <param name="width">The dimension along widthDirection.</param>
        public SquareMesh(UV location, UV lengthDirection, UV widthDirection, double length, double width)
        {
            Contract.Assert(null != location && null != lengthDirection && null != widthDirection && length > 0 && width > 0);

            vertex2DArray = new UV[Utility.MESHVERTEXNUMBER];

            vertex2DArray[0] = new UV(location.U, location.V);
            vertex2DArray[1] = new UV(location.U, location.V) + lengthDirection.Normalize() * length;
            vertex2DArray[2] = new UV(vertex2DArray[1].U, vertex2DArray[1].V) + widthDirection.Normalize() * width;
            vertex2DArray[3] = new UV(vertex2DArray[0].U, vertex2DArray[0].V) + widthDirection.Normalize() * width;
        }
        #endregion

        #region Interface implementation
        /// <summary>
        /// The side length multiplies the width is the area.
        /// </summary>
        /// <returns></returns>
        double IMeshElement.GetArea()
        {
            if(null == vertex3DArray && null == vertex2DArray)
            {
                return Utility.ZERO;
            }
            double length = Utility.ZERO;
            double width = Utility.ZERO;
            if(null != vertex3DArray && vertex3DArray.Length != 0)
            {
                length = vertex3DArray[0].DistanceTo(vertex3DArray[1]);
                width = vertex3DArray[1].DistanceTo(vertex3DArray[2]);                
            }
            if(null != vertex2DArray && vertex3DArray.Length != 0)
            {
                length = vertex2DArray[0].DistanceTo(vertex2DArray[1]);
                width = vertex2DArray[1].DistanceTo(vertex2DArray[2]);
            }

            return length * width;
        }

        /// <summary>
        /// Returning the clone of 3D vertices prevents modification of original data since it's private. 
        /// </summary>
        /// <returns>Clone of vetices</returns>
        XYZ[] IMeshElement.GetVertices3D()
        {
            if (null != vertex3DArray)
                return (XYZ[])vertex3DArray.Clone();

            return null;
        }
        

        /// <summary>
        /// Return 2D vertices.
        /// </summary>
        /// <returns></returns>
        UV[] IMeshElement.GetVertices2D()
        {
            if(null != vertex2DArray)
                return (UV[])vertex2DArray.Clone();

            return null;
        }


        XYZ IMeshElement.GetVertex3D(int number)
        {
            if (null != vertex3DArray || vertex3DArray.Length == 0)
                return null;

            if (number >= 0 && number <= vertex3DArray.Length)
                return vertex3DArray[number];

            return null;
        }


        UV IMeshElement.GetVertex2D(int number)
        {
            if (null != vertex2DArray || vertex2DArray.Length == 0)
                return null;

            if (number >= 0 && number <= vertex2DArray.Length)
                return vertex2DArray[number];

            return null;
        }


        XYZ IMeshElement.GetMeshCenter3D()
        {
            if (null == vertex3DArray || vertex3DArray.Length == 0)
                return null;
            
            XYZ tempPoint = new XYZ();
            foreach (XYZ point in vertex3DArray)
            {
                tempPoint += point;
            }

            return tempPoint.Divide(vertex3DArray.Length);
        }


        /// <summary>
        /// Get the center of all mesh vertices.
        /// </summary>
        /// <returns></returns>
        UV IMeshElement.GetMeshCenter2D()
        {
            if (null == vertex2DArray || vertex2DArray.Length == 0)
                return null;

            UV tempPoint = new UV();
            foreach (UV point in vertex2DArray)
            {
                tempPoint += point;
            }

            return tempPoint.Divide(vertex2DArray.Length);
        }

        #endregion
    }
}
