using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using LilyPad.Objects;
using Rhino.Geometry;

namespace LilyPad.Components.Results
{
    public class GH_StresslineStructure : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_StresslineStructure class.
        /// </summary>
        public GH_StresslineStructure()
          : base("Stressline Structure", "StressStruct",
              "Creates a stressline structure",
              "LilyPad", "Results")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Principal Mesh 1", "M1", "Principal mesh of the first family", GH_ParamAccess.item);
            pManager.AddGenericParameter("Principal Mesh 2", "M2", "Principal mesh of the second family", GH_ParamAccess.item);
            pManager.AddCurveParameter("stress lines 1", "S1", "Initial stress-lines in family 1", GH_ParamAccess.list);
            pManager.AddCurveParameter("stress lines 2", "S2", "Initial stress-lines in family 2", GH_ParamAccess.list);
            pManager.AddNumberParameter("Interations 1", "I1", "Number of iterations to be completed for additional stress-lines from family 1", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Interations 2", "I2", "Number of iterations to be completed for additional stress-lines from family 2", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Step Tolerance", "T", "Size of the step between successive points", GH_ParamAccess.item);
            pManager.AddNumberParameter("Max. Error", "Err", "If value > 0 then an adaptive step procedure is used where the step size is decreased if the estimated integration error is more than the specified value", GH_ParamAccess.item, 0.5 * Math.PI);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("structure", "C", "Discretised structure in the form of polylines", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            PrincipalMesh iPrincipalMesh1 = new PrincipalMesh();
            PrincipalMesh iPrincipalMesh2 = new PrincipalMesh();
            List<Curve> iInitialStressLines1 = new List<Curve>();
            List<Curve> iInitialStressLines2 = new List<Curve>();
            double iIterations1 = 0.0;
            double iIterations2 = 0.0;
            double iStepSize = 0.0;
            double iMaxAngle = 0.0;

            DA.GetData(0, ref iPrincipalMesh1);
            DA.GetData(1, ref iPrincipalMesh2);
            DA.GetDataList(2, iInitialStressLines1);
            DA.GetDataList(3, iInitialStressLines2);
            DA.GetData(4, ref iIterations1);
            DA.GetData(5, ref iIterations2);
            DA.GetData(6, ref iStepSize);
            DA.GetData(7, ref iMaxAngle);

            //____________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________
            //Convert data to the right format for the class
            List<Polyline> initialStressLines1 = iInitialStressLines1.ConvertAll(new Converter<Curve, Polyline>(curveToPolyline));
            List<Polyline> initialStressLines2 = iInitialStressLines2.ConvertAll(new Converter<Curve, Polyline>(curveToPolyline));
            int iterations1 = Convert.ToInt32(iIterations1);
            int iterations2 = Convert.ToInt32(iIterations2);

            //Create structure
            StresslineStructure stresslineStructure = new StresslineStructure(this, iPrincipalMesh1, iPrincipalMesh2);
            stresslineStructure.SetStreamlineProperties(iStepSize, iMaxAngle);
            List<Polyline> oStructure = stresslineStructure.Grow(initialStressLines1, initialStressLines2, iterations1, iterations2);

            //____________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________________

            DA.SetDataList(0, oStructure);
        }

        public static Polyline curveToPolyline(Curve d)
        {
            PolylineCurve castLine1 = d.ToPolyline(0.0001, 0.001, 0, 100000000);
            Polyline castLine2 = castLine1.ToPolyline();
            return castLine2;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("fec05fcd-8128-487f-8309-5505909d3d71"); }
        }

        /// <summary>
        /// Hide component from the user as this is a work in progress
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.hidden;
            }
        }
    }
}