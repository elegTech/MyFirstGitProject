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
    class FamilyInstanceResizer
    {
        private ElementId elementID;

        private UIApplication app;

        private Element element;
        public FamilyInstanceResizer(UIApplication app, ElementId id)
        {
            Contract.Requires(null != app && null != id);
            this.app = app;
            this.elementID = id;

            InitializeComponent();
        }

        public FamilyInstanceResizer(UIApplication app, Element element)
        {
            Contract.Requires(null != app && null != element);
            this.app = app;
            this.element = element;

            InitializeComponent();
        }

        private bool InitializeComponent()
        {
            if (null != element)
                return true;

            element = app.ActiveUIDocument.Document.GetElement(elementID);
            if (null == element)
                return false;

            return true;
        }

        private Options GetGeometryOption()
        {
            Options option = app.Application.Create.NewGeometryOptions();
            if (null == option)
                return null;

            option.ComputeReferences = true;
            option.DetailLevel = ViewDetailLevel.Fine;
            
            return option;
        }

        private List<Solid> GetSolid()
        {
            Options option = GetGeometryOption();
            GeometryElement geometryElement = element.get_Geometry(option);

            List<Solid> solidList = new List<Solid>();
            GeometryInstance geomInstance = null;

            foreach (GeometryObject geoObj in geometryElement)
            {
                geomInstance = geoObj as GeometryInstance;
                if (null == geomInstance)
                    continue;

                foreach(GeometryObject geom in geomInstance.SymbolGeometry)
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

        private List<Face> GetFace(Solid solid)
        {
            Contract.Requires(null != solid);
            List<Face> faceList = new List<Face>();

            foreach(Face geomFace in solid.Faces)
            { }
            return null;
        }

        
        public bool ResizeElement()
        {
           
            List<Solid> solidList = GetSolid();
            if (solidList.Count == 0)
                return false;


            FaceArray faceArray = solidList[0].Faces;
            List<double> areaList = new List<double>(faceArray.Size);

            int top = 0;
            int bottom = 0;
            for (int i = 0; i < faceArray.Size; i++)
            {
                areaList.Add(faceArray.get_Item(i).Area);
                if (i > 0)
                {
                    if (areaList[i] > areaList[top])
                        top = i;
                }
            }
            for (int i = 0; i < areaList.Count; i++)
            {
                if (i == top)
                    continue;
                if (Math.Abs(areaList[i]-areaList[top]) < 0.001)
                {
                    bottom = i;
                }
            }

            Face topFace = faceArray.get_Item(top);
            Face bottomFace = faceArray.get_Item(bottom);

            FamilyInstance instance = element as FamilyInstance;
            if (null != instance)
            {
                Transform transform = instance.GetTransform();
                transform.ScaleBasis(2);
            }




            return true;
        
        }


    }
}
