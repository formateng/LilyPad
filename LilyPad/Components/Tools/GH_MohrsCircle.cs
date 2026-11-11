using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using LilyPad.Objects;
using LilyPad.Properties;
using Rhino.Geometry;


namespace LilyPad.Components.Tools
{
    public class GH_MohrsCircle : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_MohrsCircle class.
        /// base(Name, Nickname, Description, Folder, SubFolder)
        /// </summary>
        public GH_MohrsCircle()
          : base("Mohr's Circle", "Mohr",
              "Uses the mathematics of a Mohr's Circle for a given stress state to find the princpal stress state and the stress state a specific rotation angle",
              "LilyPad", "Tools")
        {
        }

        /// Register all input parameters.
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Input Plane", "IPln", "Input plate for reference of the current stress state, x axis of plane should align with Sigma X etc.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Stress σxx", "σxx", "Normal stress in the X axis", GH_ParamAccess.item);
            pManager.AddNumberParameter("Stress σyy", "σyy", "Normal stress in the Y axis", GH_ParamAccess.item);
            pManager.AddNumberParameter("Stress τxy", "τxy", "Shear stress around the point", GH_ParamAccess.item);
            pManager.AddNumberParameter("Rotation Angle", "θ", "Angle of rotation to calculate the rotated stress state", GH_ParamAccess.item);
            pManager[4].Optional = true;
        }

        /// Register all output parameters.
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Principal Plane", "P Pln", "Plane representing the orientation of the principal plane", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Rotate Plane", "R Pln", "Plane representing the orientation of the input plane rotated by θ", GH_ParamAccess.item);
            pManager.AddNumberParameter("Principal Stress 1", "σ1", "Maximum principal stress", GH_ParamAccess.item);
            pManager.AddNumberParameter("Principal Stress 2", "σ2", "Minimum principal stress", GH_ParamAccess.item);
            pManager.AddNumberParameter("Principal Angle", "θp", "Angle of the principal plane relative to the input plane", GH_ParamAccess.item);
            pManager.AddNumberParameter("Rotated Stress σxx", "σxx'", "Normal stress in the X axis of the rotated plane", GH_ParamAccess.item);
            pManager.AddNumberParameter("Rotated Stress σyy", "σyy'", "Normal stress in the Y axis of the rotated plane", GH_ParamAccess.item);
            pManager.AddNumberParameter("Rotated Stress τxy", "τxy'", "Shear stress around the point in the rotated plane", GH_ParamAccess.item);
        }


        /// <summary>
        /// Assign parameter inputs
        /// run evaluate method of principalMesh and check for errors
        /// assign resulting vector to the output parameter
        /// </summary>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Plane inputPln = new Plane();
            double SigmaX = 0;
            double SigmaY = 0;
            double TauXY = 0;
            double ITheta = 0;

            DA.GetData(0, ref inputPln);
            DA.GetData(1, ref SigmaX);
            DA.GetData(2, ref SigmaY);
            DA.GetData(3, ref TauXY);
            DA.GetData(4, ref ITheta);

            //_________________________________________________________________________________

            double sigmam = (SigmaX + SigmaY) / 2;
            double radius = Math.Sqrt(Math.Pow(0.5 * (SigmaX - SigmaY), 2) + Math.Pow(TauXY, 2));
            Console.WriteLine(sigmam.ToString() + ", " + radius.ToString());

            double Sigma1 = sigmam + radius;
            double Sigma2 = sigmam - radius;

            //calculate theta
            double Theta;
            if (SigmaX == SigmaY) { Theta = 0.0; }
            else
            {
                Theta = Math.Atan(2 * TauXY / (SigmaX - SigmaY)) / 2;

                if (SigmaY > (SigmaX + SigmaY) / 2)
                {
                    if (TauXY > 0) Theta -= Math.PI / 2;
                    else Theta += Math.PI / 2;
                }
            }

            double PrincipleTheta = Theta;

            Vector3d PVector1 = orientVector(new Vector3d(Math.Cos(Theta), Math.Sin(Theta), 0), Plane.WorldXY, inputPln);
            Vector3d PVector2 = orientVector(new Vector3d(-Math.Sin(Theta), Math.Cos(Theta), 0), Plane.WorldXY, inputPln);

            Plane principlePln = new Plane(inputPln.Origin, PVector1, PVector2);
            Plane PrinciplePln = principlePln;

            // Plane rotatedPln = principlePln;
            Plane rotatedPln = inputPln;
            rotatedPln.Rotate(ITheta, principlePln.Normal);
            Plane RotatedPln = rotatedPln;

            double fromPrinipleTheta = ITheta - Theta;
            double oSigma = Math.Cos(2 * fromPrinipleTheta) * radius;
            double oTau = Math.Sin(2 * fromPrinipleTheta) * radius;

            double OSigmaX = sigmam + oSigma;
            double OSigmaY = sigmam - oSigma;
            double OTauXY = oTau;
            //___________________________________________________________________________________

            DA.SetData(0, PrinciplePln);
            DA.SetData(1, RotatedPln);
            DA.SetData(2, Sigma1);
            DA.SetData(3, Sigma2);
            DA.SetData(4, PrincipleTheta);
            DA.SetData(5, OSigmaX);
            DA.SetData(6, OSigmaY);
            DA.SetData(7, OTauXY);

        }

        //perform a cross project on the vector and each axis of the target plane so that the returned vector is given by the planes coordinate system
        private Vector3d orientVector(Vector3d vector, Plane plane0, Plane plane1)
        {
            Transform orient = Transform.PlaneToPlane(plane0, plane1);
            vector.Transform(orient);
            return vector;
        }

        /// Assign component icon
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.MohrCircle;
            }
        }

        /// Gets the unique ID for the component
        public override Guid ComponentGuid
        {
            get { return new Guid("f9e23d6a-368e-4d72-bf6f-6b42fe1220d7"); }
        }

        /// Set component to be in the FIRST group of the sub-category
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }
    }
}