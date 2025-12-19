using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using LilyPad.Objects;
using Rhino.Geometry;

namespace LilyPad.Components.Results
{
    public class GH_StressLine : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_StressLine class.
        /// base(Name, Nickname, Description, Folder, SubFolder)
        /// </summary>
        public GH_StressLine()
          : base("Stress Line", "Stress Line",
              "Creates a stress line on the principal mesh",
              "LilyPad", "Results")
        {
        }

        /// Register all input parameters.
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Principal Mesh", "Mesh", "Principal mesh to analyse", GH_ParamAccess.item);
            pManager.AddPointParameter("Seed", "S", "Point which the streamline passes through", GH_ParamAccess.item);
            pManager.AddNumberParameter("Step Tolerance", "T", "Size of the step between successive points", GH_ParamAccess.item);
            pManager.AddNumberParameter("Max. Error", "Err", "If value > 0 then an adaptive step procedure is used where the step size is decreased if the estimated integration error is more than the specified value", GH_ParamAccess.item, 0.5 * Math.PI);
        }

        /// Register all output parameters.
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Stress Line", "S", "Generated Stress Line", GH_ParamAccess.item);
        }


        /// <summary>
        /// Assign parameter inputs
        /// create streamlines object using principalMesh as input
        /// create stress line using CreateStreamLines method of stresslines
        /// assign stress line to the output parameter
        /// </summary>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Grasshopper.Kernel.Types.GH_ObjectWrapper objWrapPrinciMesh = new Grasshopper.Kernel.Types.GH_ObjectWrapper();

            PrincipalMesh iPrincipalMesh = new PrincipalMesh();
            Point3d iSeed = new Point3d();
            double iStepSize = 0.0;
            double iMethod = 4;
            double iMaxError = 0.0;


            DA.GetData(0, ref objWrapPrinciMesh);
            DA.GetData(1, ref iSeed);
            DA.GetData(2, ref iStepSize);
            DA.GetData(3, ref iMaxError);

            if (objWrapPrinciMesh != null)
                iPrincipalMesh = objWrapPrinciMesh.Value as PrincipalMesh;

            //_________________________________________________________________________________
            Streamlines streamlines = new Streamlines(this, iPrincipalMesh, iStepSize, Convert.ToInt32(iMethod), iMaxError, 0.0);

            Polyline oStressLine = streamlines.CreateStreamline(iSeed);

            //___________________________________________________________________________________

            DA.SetData(0, oStressLine);

        }

        /// Assign component icon
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.IconStressLine;
            }
        }

        /// Gets the unique ID for the component
        public override Guid ComponentGuid
        {
            get { return new Guid("25100798-02f9-4904-a668-22e705710ebf"); }
        }

        /// Set component to be in the SECOND group of the sub-category
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.secondary;
            }
        }
    }
}