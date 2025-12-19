using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using LilyPad.Objects;
using Rhino.Geometry;

namespace LilyPad.Components.Results
{
    public class GH_StressLines : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_StressLines class.
        /// base(Name, Nickname, Description, Folder, SubFolder)
        /// </summary>
        public GH_StressLines()
          : base("Stress Lines", "SLs",
              "Creates a series of stress lines on the principal mesh",
              "LilyPad", "Results")
        {
        }

        /// Register all input parameters.
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Principal Mesh", "Mesh", "Principal mesh to analyse", GH_ParamAccess.item);
            pManager.AddPointParameter("Seed", "S", "Point which the streamline passes through", GH_ParamAccess.item);
            pManager.AddNumberParameter("Step Tolerance", "T", "Size of the step between successive points", GH_ParamAccess.item);
            pManager.AddNumberParameter("Max. Error", "Err", "If value > 0 then an adaptive step procedure is used where the step size is decreased if the estimated integration error is more than the specified value", GH_ParamAccess.item, 0.0);
            //pManager.AddNumberParameter("Seeding Method", "SM", "Seeding strategy used to generate new seeding point: 'Neighbour =1', 'Fill Gap=2'", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Seperation", "dSep", "The seperation distance of new seed points", GH_ParamAccess.item);
            pManager.AddNumberParameter("Test Dist.", "dTest", "The distance which a streamline is ended if it is closer than this distance to a existing streamline, this is only applied if the specified value is >0", GH_ParamAccess.item);
        }

        /// Register all output parameters.
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Stress Lines", "Ss", "Generated stress lines", GH_ParamAccess.list);
        }

        /// <summary>
        /// Assign parameter inputs
        /// create streamlines object using principalMesh as input
        /// create stress lines using CreateStreamLines method of stresslines
        /// assign stress lines to the output parameter
        /// </summary>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Grasshopper.Kernel.Types.GH_ObjectWrapper objWrapPrinciMesh = new Grasshopper.Kernel.Types.GH_ObjectWrapper();


            PrincipalMesh iPrincipalMesh = new PrincipalMesh();
            Point3d iSeed = new Point3d();
            double iStepSize = 0.0;
            double iMethod = 4; //the core code allows a choice of integration method but as the Runge Kutta 4 method is superior, this method is used by the component
            double iMaxAngle = 0.0;
            double iStrategy = 1;
            double iDSep = 0.0;
            double iDTest = 0.0;

            DA.GetData(0, ref objWrapPrinciMesh);
            DA.GetData(1, ref iSeed);
            DA.GetData(2, ref iStepSize);
            DA.GetData(3, ref iMaxAngle);
            //DA.GetData(4, ref iStrategy);
            DA.GetData(4, ref iDSep);
            DA.GetData(5, ref iDTest);

            if (objWrapPrinciMesh != null)
                iPrincipalMesh = objWrapPrinciMesh.Value as PrincipalMesh;


            //_________________________________________________________________________________
            Streamlines streamlines = new Streamlines(this, iPrincipalMesh, iStepSize, Convert.ToInt32(iMethod), iMaxAngle, iDTest);

            List<Polyline> oStressLines = streamlines.CreateStreamlines(iSeed, Convert.ToInt32(iStrategy), iDSep);

            //___________________________________________________________________________________

            DA.SetDataList(0, oStressLines);

        }

        /// Assign component icon
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.IconStressLines;
            }
        }

        /// Gets the unique ID for the component
        public override Guid ComponentGuid
        {
            get { return new Guid("31cc7a61-5877-4d3e-8627-3d653e415f54"); }
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