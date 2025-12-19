using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriangleNet.Data;

namespace LilyPad.Objects
{
    class StresslineStructure
    {
        /*Pseudo code
         * input intial stresslines ⁄
         * constuct polylines /
         * find largest deviation in each family
         * construct new stresslines
         * construct polylines
         * repeat
         */

        //Properties
        private PrincipalMesh PrincipalMesh1;
        private PrincipalMesh PrincipalMesh2;
        private GH_Component ParentComponent;
        private List<Polyline> stresslinesFamily1;
        private List<Polyline> stresslinesFamily2;
        private List<Polyline> discretisedStresslinesFamily1;
        private List<Polyline> discretisedStresslinesFamily2;
        private double StepSize;
        private double MaxError;
        private Plane MeshPlane;



        //Constructors
        public StresslineStructure(GH_Component parentComponent, PrincipalMesh principalMesh1, PrincipalMesh principalMesh2)
        {
            if (!IsPlanar(principalMesh1.Mesh, out Plane plane1) || !IsPlanar(principalMesh2.Mesh, out Plane plane2))
            {
                throw new ArgumentException("One or both of the provided meshes are not planar.");
            }

            ParentComponent = parentComponent;
            PrincipalMesh1 = principalMesh1;
            PrincipalMesh2 = principalMesh2;
            MeshPlane = plane1; // Assuming both meshes are on the same plane

            StepSize = 0.1;
            MaxError = 0.0;
        }

        public StresslineStructure()
        {
            PrincipalMesh1 = new PrincipalMesh();
            PrincipalMesh2 = new PrincipalMesh();

            if (!IsPlanar(PrincipalMesh1.Mesh, out Plane plane1) || !IsPlanar(PrincipalMesh2.Mesh, out Plane plane2))
            {
                throw new ArgumentException("One or both of the provided meshes are not planar.");
            }

            MeshPlane = plane1; // Assuming both meshes are on the same plane

            StepSize = 0.1;
            MaxError = 0.0;
        }

        //Methods
        public List<Polyline> Grow(List<Polyline> family1Intial, List<Polyline> family2Intial, int iterations1, int iterations2)
        {
            //add in the initial stress lines into each list of stresslines used
            stresslinesFamily1 = new List<Polyline>(family1Intial);
            stresslinesFamily2 = new List<Polyline>(family2Intial);

            Streamlines streamlines1 = new Streamlines(ParentComponent, PrincipalMesh1, StepSize, 4, MaxError, 0.0);
            Streamlines streamlines2 = new Streamlines(ParentComponent, PrincipalMesh2, StepSize, 4, MaxError, 0.0);

            //discretise the initial stresslines
            Discretise();

            //loop for the number of iterations from the largest of the two values
            for (int i = 0; i < Math.Max(iterations1, iterations2); i++)
            {
                //Create new stress-lines using the points of largest deviation as the seeds
                if (i < iterations1)
                {
                    stresslinesFamily1.Add(streamlines1.CreateStreamline(FindDeviation(2)));
                    Discretise();
                }

                if (i < iterations2)
                {
                    stresslinesFamily2.Add(streamlines2.CreateStreamline(FindDeviation(1)));
                    Discretise();
                }
            }

            //return all the discretised stress lines in one list
            List<Polyline> outPolylines = new List<Polyline>();
            for (int i = 0; i < discretisedStresslinesFamily1.Count; i++) outPolylines.Add(discretisedStresslinesFamily1[i]);
            for (int i = 0; i < discretisedStresslinesFamily2.Count; i++) outPolylines.Add(discretisedStresslinesFamily2[i]);
            return outPolylines;
        }

        public void SetStreamlineProperties(double stepsize, double maxError)
        {
            StepSize = stepsize;
            MaxError = maxError;
        }

        private void Discretise()
        {
            //create discretised stresslines for family 1
            discretisedStresslinesFamily1 = new List<Polyline>();
            foreach (var stressLine in stresslinesFamily1)
            {
                List<double> pointPara = new List<double>();
                for (int i = 0; i < stresslinesFamily2.Count; i++)
                {
                    CurveIntersections intersections = Intersection.CurveCurve(stressLine.ToPolylineCurve(), stresslinesFamily2[i].ToPolylineCurve(), 0.00001, 0.00001);
                    foreach (var intersection in intersections) pointPara.Add(intersection.ParameterA);
                }
                pointPara.Sort();

                //create polyline
                Polyline descretisedStressLine = new Polyline();
                descretisedStressLine.Add(stressLine[0]);
                for (int i = 0; i < pointPara.Count; i++) descretisedStressLine.Add(stressLine.PointAt(pointPara[i]));
                descretisedStressLine.Add(stressLine.Last);

                //add to list
                discretisedStresslinesFamily1.Add(descretisedStressLine);
            }

            //create discretised stresslines for family 2
            discretisedStresslinesFamily2 = new List<Polyline>();
            foreach (var stressLine in stresslinesFamily2)
            {
                List<double> pointPara = new List<double>();
                for (int i = 0; i < stresslinesFamily1.Count; i++)
                {
                    CurveIntersections intersections = Intersection.CurveCurve(stressLine.ToPolylineCurve(), stresslinesFamily1[i].ToPolylineCurve(), 0.00001, 0.00001);
                    foreach (var intersection in intersections) pointPara.Add(intersection.ParameterA);
                }
                pointPara.Sort();

                //create polyline
                Polyline descretisedStressLine = new Polyline();
                descretisedStressLine.Add(stressLine[0]);
                for (int i = 0; i < pointPara.Count; i++) descretisedStressLine.Add(stressLine.PointAt(pointPara[i]));
                descretisedStressLine.Add(stressLine.Last);

                //add to list
                discretisedStresslinesFamily2.Add(descretisedStressLine);
            }
        }

        /*
        private Point3d findDeviation(int family)
        {
            List<Polyline> polylines = new List<Polyline>();
            List<Polyline> stressLines = new List<Polyline>();

            if (family == 1)
            {
                polylines = discretisedStresslinesFamily1;
                stressLines = stresslinesFamily1;
            }
            else
            {
                polylines = discretisedStresslinesFamily2;
                stressLines = stresslinesFamily2;
            }

            //find the point of the largest deviation from the stressline within the selected family
            double largestDevi = 0.0;
            Point3d locationOfLargestDevi = new Point3d();
            //for each polyline in the list
            for (int k = 0; k < polylines.Count; k++)
            {
                Polyline polyline = polylines[k];

                //for each segment of the polyline
                for (int i = 0; i < polyline.Count - 1; i++)
                {
                    //start new segment
                    Point3d segmentpoint1 = polyline[i];
                    Point3d segmentpoint2 = polyline[i + 1];

                    //assign the segment distance variables used to find the two points with the largest deviation
                    double segmentDist1 = 0.0;
                    double segmentDist2 = 0.0;


                    //progressively sample 10 points all the segment and find the 2 points with the largest deviation and use them as the segments points in the next loop; loop ends when the change in deviation per loop is small.
                    double lastLoopDist = 1.0;
                    int count = 0;
                    while(Math.Abs(lastLoopDist-segmentDist1)>0.001 && count<50)
                    {
                        lastLoopDist = segmentDist1;

                        //calculate stepping vector
                        Vector3d vector = (segmentpoint2 - segmentpoint1) / 9;
                        Point3d point = segmentpoint1;

                        //evaluate all 10 points including segmentpoint1 and segmentpoint2
                        for (int j = 0; j < 10; j++)
                        {
                            //find the deviation for this point
                            Point3d pointOnCurve = stressLines[k].ClosestPoint(point);
                            double dist = pointOnCurve.DistanceTo(point);

                            //check if new points has a larger distance then the previously selected two largest points
                            if(dist > segmentDist1)
                            {
                                segmentDist2 = segmentDist1;
                                segmentpoint2 = segmentpoint1;
                                segmentDist1 = dist;
                                segmentpoint1 = point;
                            }
                            else if(dist> segmentDist2)
                            {
                                segmentDist2 = dist;
                                segmentpoint2 = point;
                            }

                            //create new point
                            point += vector;
                        }
                        count++;
                    }

                    //if the larges deviation for the segment is greater than the largest deviation overall then replace the point with this one.
                    if(segmentDist1 > largestDevi)
                    {
                        largestDevi = segmentDist1;
                        locationOfLargestDevi = stressLines[k].ClosestPoint(segmentpoint1);
                    }
                }
            }

            return locationOfLargestDevi;
        }
        */

        private Point3d FindDeviation(int family)
        {
            List<Polyline> polylines = new List<Polyline>();
            List<Polyline> stressLines = new List<Polyline>();

            if (family == 1)
            {
                polylines = discretisedStresslinesFamily1;
                stressLines = stresslinesFamily1;
            }
            else
            {
                polylines = discretisedStresslinesFamily2;
                stressLines = stresslinesFamily2;
            }

            //find the point of the largest deviation from the stressline within the selected family
            double largestDevi = 0.0;
            Point3d locationOfLargestDevi = new Point3d();
            //for each polyline in the list
            for (int k = 0; k < polylines.Count; k++)
            {
                Polyline polyline = polylines[k];

                //for each segment of the polyline
                for (int i = 0; i < polyline.Count - 1; i++)
                {
                    //get stress-line segement for this polyline segment
                    Curve stressLineSegement = stressLines[k].ToPolylineCurve().Trim(stressLines[k].ClosestParameter(polyline[i]), stressLines[k].ClosestParameter(polyline[i + 1]));

                    //assign the segment points used to find where the curve should be split for the next while loop
                    double segmentDist1 = 0.0;
                    double segmentDist2 = 0.0;
                    double segmentParam1 = 0.0;
                    double segmentParam2 = 1.0;


                    //assign the segment distance variables used to find the two points with the largest deviation
                    Point3d segmentpoint1 = new Point3d();
                    Point3d segmentpoint2 = new Point3d();


                    //progressively sample 10 points all the segment and find the 2 points with the largest deviation and use them as the segments points in the next loop; loop ends when the change in deviation per loop is small.
                    double lastLoopDist = 1.0;
                    int count = 0;
                    while (Math.Abs(lastLoopDist - segmentDist1) > 0.001 && count < 50)
                    {
                        lastLoopDist = segmentDist1;
                        stressLineSegement.Domain = new Interval(0.0, 1.0);

                        //calculate the 10 sampling points
                        double[] pointParams = stressLineSegement.DivideByCount(10, true);

                        //evaluate all 10 points including segmentpoint1 and segmentpoint2
                        for (int j = 0; j < 10; j++)
                        {
                            //point location
                            Point3d pointOnStressLine = stressLineSegement.PointAt(pointParams[j]);
                            //find the deviation for this point
                            Point3d pointOnPolyline = polyline.ClosestPoint(pointOnStressLine);
                            double dist = pointOnPolyline.DistanceTo(pointOnStressLine);

                            //check if new points has a larger distance then the previously selected two largest points
                            if (dist > segmentDist1)
                            {
                                //replace the values of the 2nd largest with the first larges
                                segmentDist2 = segmentDist1;
                                segmentpoint2 = segmentpoint1;
                                segmentParam2 = segmentParam1;

                                //assign new 1st largest values
                                segmentDist1 = dist;
                                segmentpoint1 = pointOnStressLine;
                                segmentParam1 = pointParams[j];
                            }
                            else if (dist > segmentDist2)
                            {
                                //assign new 2nd largest values
                                segmentDist2 = dist;
                                segmentpoint2 = pointOnStressLine;
                                segmentParam2 = pointParams[j];

                            }
                        }

                        //create new segment using the two points with the largest distance
                        if (segmentParam1 > segmentParam2)
                        {
                            double store = segmentParam1;
                            segmentParam1 = segmentParam2;
                            segmentParam2 = store;
                        }
                        stressLineSegement = stressLineSegement.Trim(segmentParam1, segmentParam2);

                        count++;
                        if (count > 49) new Exception("invite loop detected");
                    }

                    //if the larges deviation for the segment is greater than the largest deviation overall then replace the point with this one.
                    if (segmentDist1 > largestDevi)
                    {
                        largestDevi = segmentDist1;
                        locationOfLargestDevi = segmentpoint1;
                    }
                }
            }

            return locationOfLargestDevi;
        }

        private bool IsPlanar(Mesh mesh, out Plane plane)
        {
            plane = new Plane();
            if (mesh.Vertices.Count < 3)
                return false;

            Point3d[] vertices = mesh.Vertices.ToPoint3dArray();
            plane = new Plane(vertices[0], vertices[1], vertices[2]);

            for (int i = 3; i < vertices.Length; i++)
            {
                double distToPlane = Math.Abs(plane.DistanceTo(vertices[i]));
                if (!(distToPlane<0.001))
                    return false;
            }

            return true;
        }

    }
}
