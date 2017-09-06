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
    class Utility
    {

        public static List<Solid> GetSolid(Element element, Options option)
        {
            Contract.Requires(null != element && null != option);

            GeometryElement geometryElement = element.get_Geometry(option);
            List<Solid> solidList = new List<Solid>();
            GeometryInstance geomInstance = null;

            foreach (GeometryObject geoObj in geometryElement)
            {
                geomInstance = geoObj as GeometryInstance;
                if (null == geomInstance)
                    continue;

                foreach (GeometryObject geom in geomInstance.SymbolGeometry)
                {
                    Solid solid = geom as Solid;
                    if (null == solid || 0 == solid.Faces.Size || 0 == solid.Edges.Size)
                    {
                        continue;
                    }
                    solidList.Add(solid);
                }
            }

            return solidList;
        }

        public static List<Face> GetFace(Solid solid)
        {
            Contract.Requires(null != solid);
            List<Face> faceList = new List<Face>();

            foreach (Face geomFace in solid.Faces)
            {
                if (geomFace.Area > 0)
                    faceList.Add(geomFace);
            }
            return null;
        }
    }
}
