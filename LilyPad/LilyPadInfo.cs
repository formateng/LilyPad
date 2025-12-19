using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;
using LilyPad.Properties;

namespace LilyPad
{
  public class LilyPadInfo : GH_AssemblyInfo
  {
        public override string Name => "LilyPad";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => Resources.LilyPadIcon24x24_02;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "A plugin for generating smooth evenly distributed stress lines to analyse structural shells and slabs";

        public override Guid Id => new Guid("b03fd707-57b0-4a02-8d9b-1cd305b79e07");

        //Return a string identifying you or your company.
        public override string AuthorName => "Matt Church";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "mc@formatengineers.com";

        //Set plugin version
        public override string Version => "0.2.2";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
  }

    public class LilyPadCategoryIcon : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.ComponentServer.AddCategoryIcon("LilyPad", Resources.LilyPadIcon16x16_1);
            Instances.ComponentServer.AddCategorySymbolName("LilyPad", 'L');
            return GH_LoadingInstruction.Proceed;
        }
    }
}