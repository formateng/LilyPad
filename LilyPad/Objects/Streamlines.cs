///summary
///this class is focused on creating steamlines for a given principal mesh.
///There are two main capabilities:
///Creating single streamlines from a seed point.
///Creating a series of streamlines from a seed point for the first streamline using possible two seeding methods to create the rest of the streamlines.
///This class is reliant on Evaluate method of the PrincipalMesh class to determine the direction of the field at any one point.
///summary


using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriangleNet;
using TriangleNet.Data;

namespace LilyPad.Objects
{
    class Streamlines
    {
        //Properties_____________________________________________________________________________________________________________________________________________________________________________________________________________________________________________
        PrincipalMesh PrincipalMesh;
        public Rhino.Geometry.Mesh Mesh;
        private Polyline[] NakedEdges;
        private TriangleNet.Geometry.InputGeometry TriGeom;
        private GH_Component ParentComponent;

        //Variables_____________________________________________________________________________________________________________________________________________________________________________________________________________________________________________

        //Streamline
        private Point3d Point;
        private Point3d End;
        private Point3d NextPoint;
        private double Error;
        private double NextError;
        private Vector3d Vec0;
        private Vector3d Vec1;
        private double StepSize;
        private double IStepSize;
        private int Method;
        private double MaxError;
        private double DTest;
        private int SeedingStrategy;
        private double DSep;

        //Streamlines
        public List<Point3d> UsedSeeds;
        private Polyline Streamline;
        private List<Polyline> CompletedStreamlines;
        private List<Point3d> CheckPts;


        //Constructors_____________________________________________________________________________________________________________________________________________________________________________________________________________________________________________

        /// <summary>
        /// creates a new streamline class with empty properties
        /// </summary>
        public Streamlines()
        {
            PrincipalMesh = new PrincipalMesh();
            CompletedStreamlines = new List<Polyline>();
            UsedSeeds = new List<Point3d>();
            CheckPts = new List<Point3d>();
            TriGeom = new TriangleNet.Geometry.InputGeometry();
            NakedEdges = new Polyline[0];
        }

        ///// <summary>
        ///// creates a new streamline class, importing data of a given principalMesh
        ///// </summary>
        //public Streamlines(PrincipalMesh principalMesh)
        //{
        //    PrincipalMesh = principalMesh;
        //    Mesh = principalMesh.Mesh;
        //    Mesh.FaceNormals.ComputeFaceNormals();
        //    NakedEdges = principalMesh.NakedEdges;
        //    CompletedStreamlines = new List<Polyline>();
        //    UsedSeeds = new List<Point3d>();
        //    CheckPts = new List<Point3d>();
        //    TriGeom = new TriangleNet.Geometry.InputGeometry();
        //}

        /// <summary>
        /// creates a new streamline class, importing data of a given principalMesh
        /// </summary>
        public Streamlines(GH_Component parentComponent, PrincipalMesh principalMesh, double iStepSize, int method, double maxError, double dTest)
        {
            PrincipalMesh = principalMesh;
            Mesh = principalMesh.Mesh;
            Mesh.FaceNormals.ComputeFaceNormals();
            NakedEdges = principalMesh.NakedEdges;
            ParentComponent = parentComponent;


            CompletedStreamlines = new List<Polyline>();
            UsedSeeds = new List<Point3d>();
            CheckPts = new List<Point3d>();
            TriGeom = new TriangleNet.Geometry.InputGeometry();

            //import variables
            IStepSize = iStepSize;
            StepSize = iStepSize;
            Method = method;
            MaxError = maxError;
            DTest = dTest;
        }

        //Methods_____________________________________________________________________________________________________________________________________________________________________________________________________________________________________________
        /// <summary>
        /// Creates a streamline by using multiple methods where the streamline is represented by a ployline with each vertice on the mesh
        /// </summary>
        public Polyline CreateStreamline(Point3d seed)
        {
            Streamline = new Polyline();

            //Starting varibles
            Point = seed;
            StepSize = IStepSize;

            //Creates first part of streamline from the seed in first direction
            Polyline segment1 = CreateStreamlineSegment(1);

            if (segment1.IsClosed){ return segment1; }

            //reset variables
            Point = seed;
            StepSize = IStepSize;


            //Creates second part of streamline from the seed in second direction
            Polyline segment2 = CreateStreamlineSegment(-1);
            if (segment2.IsClosed) { return segment2; }

            //attach segment2 to segment1
            segment2.RemoveAt(0);
            segment2.Reverse();
            segment1.InsertRange(0, segment2);

            return segment1;
            
        }

        /// <summary>
        /// Creates a series of streamlines by using two seeding methods to find the location of the new streamline
        /// </summary>
        public List<Polyline> CreateStreamlines(List<Point3d> startSeed, int seedingMethod, double dSep)
        {
            //Starting varibles
            SeedingStrategy = seedingMethod;
            DSep = dSep;

            CompletedStreamlines = new List<Polyline>();
            UsedSeeds = new List<Point3d>();
            CheckPts = new List<Point3d>();

            //Neighbour seeding strategy
            if (seedingMethod == 1)
            {
                NeighbourStreamlines(startSeed);
            }

            //Farthest Point Strategy
            if (seedingMethod == 2)
            {
                FarthestSteamlines(startSeed[0]);
            }
            return CompletedStreamlines;
        }

        /// <summary>
        /// streamline seeding method for finding next seed point adjecent to existing streamlines
        /// </summary>
        /// <param name="startSeed"></param>
        private void NeighbourStreamlines(List<Point3d> startSeed)
        {
            List<Point3d> seeds = new List<Point3d>();
            seeds.AddRange(startSeed);
            Polyline activeStreamline = new Polyline();
            Point3d seed = new Point3d();

            while (seeds.Count > 0)
            {

                //get new seed
                seed = seeds[0];

                //test if seed is valid for stream line seperation distance, if not remove seed
                double SeedDistFactor = Math.Max(0.6, DTest / DSep * 1.1);
                Vector3d radius1 = new Vector3d(DSep * SeedDistFactor, DSep * SeedDistFactor, DSep * SeedDistFactor);
                BoundingBox testBox = new BoundingBox(seed - radius1, seed + radius1);
                for (int i = 0; i < CheckPts.Count; i++)
                {
                    if (testBox.Contains(CheckPts[i]))
                    {
                        if ((CheckPts[i] - seed).Length < DSep * SeedDistFactor)
                        {
                            seeds.RemoveAt(0);
                            goto BREAK;
                        }
                    }

                }

                //generate streamline
                activeStreamline = CreateStreamline(seed);

                //prevents the inclusion of very short streamlines
                if (activeStreamline.Count < 3)
                {
                    seeds.RemoveAt(0);
                    goto BREAK;
                }

                //add streamline into list
                CompletedStreamlines.Add(activeStreamline);

                //add streamline vertices into list of checkpts for streamline distance detection
                for (int i = 0; i < activeStreamline.Count; i++)
                {
                    CheckPts.Add(activeStreamline[i]);
                }

                //remove current seed
                UsedSeeds.Add(seed);
                seeds.RemoveAt(0);

                //find new seeds
                for (int i = 0; i < activeStreamline.Count - 1; i += 10)
                {
                    Point3d pt = activeStreamline[i];

                    //find the streamline direction
                    Point3d ptPlus1 = activeStreamline[i + 1];
                    Vector3d streamlineDirection = ptPlus1 - pt;

                    //find the point on the mesh
                    MeshPoint meshPt = Mesh.ClosestMeshPoint(pt, 0.0);

                    //offset pt by the streamline direction rotated by 90 degrees in both directions
                    streamlineDirection.Rotate(Math.PI / 2, Mesh.FaceNormals[meshPt.FaceIndex]); ;
                    Point3d seed1 = pt + streamlineDirection / streamlineDirection.Length * DSep;
                    //streamlineDirection.Rotate(Math.PI, Mesh.FaceNormals[meshPt.FaceIndex]); ;
                    Point3d seed2 = pt - streamlineDirection / streamlineDirection.Length * DSep;

                    //move seeds onto the mesh
                    seed1 = Mesh.ClosestPoint(seed1);
                    seed2 = Mesh.ClosestPoint(seed2);

                    //insert new seeds
                    seeds.Add(seed1);
                    seeds.Add(seed2);
                    //seeds.Insert(0, seed1);
                    //seeds.Insert(0, seed2);
                }

            BREAK:;
            }

        }

        /// <summary>
        /// steamline seeding method for finding next seed in largest gap between existing streamlines
        /// </summary>
        /// <param name="seed"></param>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private void FarthestSteamlines(Point3d seed)
        {
            // Error test: test if mesh is planar and in the WORLDXY plane
            for (int i = 0; i < Mesh.Faces.Count; i++)
            {
                MeshFace face = Mesh.Faces[i];
                Point3d[] vertices = new Point3d[]
                {
                    Mesh.Vertices[face.A],
                    Mesh.Vertices[face.B],
                    Mesh.Vertices[face.C],
                    face.IsQuad ? Mesh.Vertices[face.D] : Mesh.Vertices[face.C]
                };

                Plane facePlane;
                double planeDeviation=0.001;
                PlaneFitResult planeFitResult = Plane.FitPlaneToPoints(vertices, out facePlane, out planeDeviation);
                if (planeFitResult != 0 || planeDeviation>0.001)
                {
                    throw new NotImplementedException("Farthest point seeding method is not yet implemented for non-planar mesh faces.");
                }

                if ((facePlane.Normal.IsParallelTo(Vector3d.ZAxis) == 0))
                {
                    throw new NotImplementedException("Farthest point seeding method is not yet implemented for mesh faces not in the WORLDXY plane.");
                }
            }

            //new parameters
            Polyline activeStreamline = new Polyline();
            PolylineCurve boundary = new PolylineCurve();

            //Create boundary
            if (NakedEdges.Length > 1)
            {
                throw new ArgumentException("Field element boundaries cannot be assembled into a single boundary curve");
            }
            else boundary = NakedEdges[0].ToPolylineCurve();

            //Transform the boundary into an offset polyline curve
            // Offset curve by the seperation distance
            //boundary.Offset(MeshPlane, dSep, stepSize / 100, CurveOffsetCornerStyle.Sharp);

            //convert the curve to a polyline with vertices at approximately dSep apart
            int n = Convert.ToInt32(Math.Ceiling(boundary.GetLength() / DSep));
            Point3d[] boundPointsArr = new Point3d[n];
            double[] boundT = boundary.DivideByCount(n, false, out boundPointsArr);
            List<Point3d> boundPoints = boundPointsArr.OfType<Point3d>().ToList();
            boundPoints.Add(boundary.PointAtEnd);
            boundPoints.Insert(0, boundary.PointAtStart);
            Polyline boundPolyline = new Polyline(boundPoints);


            // InitialTriangulate boundary polyline
            TriangleNet.Mesh tmesh = InitialTriangulate(boundPolyline);

            List<Circle> circles = new List<Circle>();
            int limit = 100;

            for (int i = 0; i < limit; i++) //REMOVE HARD STOP
            {

                //generate streamline
                activeStreamline = CreateStreamline(seed);
                CompletedStreamlines.Add(activeStreamline);

                /*                    Vertex vertex1 = new Vertex();*/
                tmesh = InsertStreamline(activeStreamline);

                /*                    //add streamline points into the delaunay mesh
                                    for (int j = 1; j < activeStreamline.Count - 1; j++)
                                    {
                                        vertex1 = new TriangleNet.Data.. Vertex(activeStreamline[j].X, activeStreamline[j].Y);
                                        bool test = tmesh.InsertVertex(vertex1);
                                    }*/
                //Add seed to used seed list
                UsedSeeds.Add(seed);

                // Find largest circumcircle
                circles = Circumcircles(tmesh);
                circles.Sort((c1, c2) => c2.Radius.CompareTo(c1.Radius));

            RETRY:
                //determine if saturation has been reached
                if (circles.Count == 0 || circles[0].Diameter < DSep * 2) break;
                //if (!tmesh.Bounds.Contains(new TriangleNet.Geometry.Point(circles[0].Center.X, circles[0].Center.Y)))
                //PointContainment containment = boundPolyline.Contains(circles[0].Center, MeshPlane, 0.00001);
                Point3d centerOnMesh = Mesh.ClosestPoint(circles[0].Center);
                if (centerOnMesh.DistanceTo(circles[0].Center) > 0.01)
                {
                    circles.RemoveAt(0);
                    goto RETRY;
                }

                //get new seed and check pts
                seed = circles[0].Center;
                tmesh = new TriangleNet.Mesh();
            }
            TriGeom = null;
        }

        /// <summary>
        /// Creates part of the streamline in a single direction from the seed
        /// </summary>
        private Polyline CreateStreamlineSegment(int direction)
        {
            Polyline streamlineSegment = new Polyline();

            //boolean value to end looping
            bool test = true;
            //records the current level of stepSize subdivision
            int level = 0;
            //tracks the last time the stepsize was decreased
            int track1 = -2;

            Vec0 = new Vector3d();
            Vec1 = new Vector3d();

            //start the streamline with the first step
            streamlineSegment.Add(Point);
            Error = SolveStep(Point, out End, Method, direction);
            Vec0 = End - Point;
            Vec0 = Vec0 / Vec0.Length;
            Vector3d vecFirst = Vec0 * direction;

            int i = 0;
            while (test)
            {
                //double error = solveStep(Point, out End, Method, direction);
                End = Mesh.ClosestPoint(End);

                if(i % 100 == 0)
                {
                    if (GH_Document.IsEscapeKeyDown())
                    {
                        ParentComponent.OnPingDocument()?.RequestAbortSolution(); //Abort the calculations
                        throw new ArgumentException("Solution aborted due to ESC press");

                    }
                }

                //tests if end is located at a naked edge; if it is then test is false
                test = true;
                foreach (var edge in NakedEdges)
                {
                    Point3d testPoint = edge.ClosestPoint(End);
                    double dist = testPoint.DistanceTo(End);
                    if (dist < 0.001)
                    {
                        test = false;
                        Error = SolveStep(Point, out End, Method, direction);
                        Line edgeSegment = new Line(Point, End);
                        edge.ToNurbsCurve().ClosestPoints(edgeSegment.ToNurbsCurve(), out End, out Point3d point3);
                    }
                }
                if (!test && Point.DistanceTo(End) > 0.0001) streamlineSegment.Add(End);

                //Only run rest of script if the solvestep has 
                if (test)
                {
                    //evaluate the next step for angle control between adjecent steps
                    NextError = SolveStep(End, out NextPoint, Method, direction);
                    Vec1 = NextPoint - End;
                    Vec1 = Vec1 / Vec1.Length;

                    //find angle between previous vector and new vector before the point is added
                    double angle = Math.Abs(Vector3d.VectorAngle(Vec0, Vec1));

                    //only run additional computation if adaptive step is activated by the max error being greater than 0
                    if (MaxError > 0)
                    {
                        //decrease step size
                        if ((Error > MaxError || angle > 0.1 * Math.PI) && level < 7)
                        {
                            //make the stepsize smaller
                            StepSize /= 2;
                            level++;
                            //prevents repeating increasing and decreasing of the stepsize
                            track1 = i;
                            //recalculate step
                            Error = SolveStep(Point, out End, Method, direction);
                            Vec0 = End - Point;
                            Vec0 = Vec0 / Vec0.Length;
                        }
                        //increase step size
                        else if (Error < MaxError / 2 && angle < 0.05 * Math.PI && level > 0 && track1 + 1 != i)
                        {
                            //make the stepsize larger
                            StepSize *= 2;
                            level--;
                            //recalculate step
                            Error = SolveStep(Point, out End, Method, direction);
                            Vec0 = End - Point;
                            Vec0 = Vec0 / Vec0.Length;
                        }
                        //test to see if 180 degree fliping has occurred and only flip it if the level is up to 7
                        else if (angle > Math.PI * 0.7)
                        {
                            if (level == 7)
                            {
                                direction *= -1;
                                Vec1 = -Vec1;
                                NextError = SolveStep(End, out NextPoint, Method, direction);
                                //add in new point
                                test = AddInNewPoint(streamlineSegment);
                            }
                            else
                            {
                                //make the stepsize smaller
                                StepSize /= 2;
                                level++;
                                //prevents repeating increasing and decreasing of the stepsize
                                track1 = i;
                                //recalculate step
                                Error = SolveStep(Point, out End, Method, direction);
                                Vec0 = End - Point;
                                Vec0 = Vec0 / Vec0.Length;
                            }
                        }
                        else
                        {
                            //add in new point
                            test = AddInNewPoint(streamlineSegment);
                        }
                    }
                    else
                    {
                        //test to see if 180 degree fliping has occurred
                        if (angle > Math.PI * 0.7)
                        {
                            direction *= -1;
                            Vec1 = -Vec1;
                            NextError = SolveStep(End, out NextPoint, Method, direction);
                        }
                        test = AddInNewPoint(streamlineSegment);
                    }

                    //test for looping stresslines based on latest point getting close too the start point
                    if (streamlineSegment.Length > 3 * IStepSize && Math.Abs(Vector3d.VectorAngle(vecFirst, Vec1)) < 0.1 * Math.PI && End.DistanceTo(streamlineSegment[0]) < IStepSize)
                    {
                        streamlineSegment.Add(streamlineSegment[0]);
                        return streamlineSegment;
                    }

                    //second test for looping which detects spiraling. If the latest point gets too close to any previous point (excluding the previous 2*StepSize/IStepSize points), then remove all the points before the closest point and close the streamline.
                    else if (streamlineSegment.Length > 3 * IStepSize && streamlineSegment.Count >= 10)
                    {
                        int rangeStart = streamlineSegment.Count - 10;
                        for (int j = rangeStart; j >= 0; j--)
                        {
                            double dist = streamlineSegment[j].DistanceTo(End);
                            // if the end of the streamline is within the step size of the streamline carry on testing
                            if (dist < StepSize)
                            {
                                Vector3d vecJ = new Vector3d(streamlineSegment[j + 1] - streamlineSegment[j]);
                                // if the point within the stepsize of the end is also nearly parallel with the latest vector then:
                                if (Math.Abs(Vector3d.VectorAngle(vecJ, Vec0)) < 0.1 * Math.PI)
                                {
                                    //remove all points before the point which is within the stepsize
                                    streamlineSegment.RemoveRange(0, j);
                                    streamlineSegment.Add(streamlineSegment[0]);
                                    return streamlineSegment;
                                }
                            }
                        }
                    }

                }

                //prevent infinte loops
                if (i > 10000) test = false;
                i++;
            }
            return streamlineSegment;

        }

        /// <summary>
        ///Adds new point to streamline and changes variables
        /// </summary>
        private bool AddInNewPoint(Polyline polyline)
        {
            bool dTestTest;
            if (DTest > 0)
            {
                dTestTest = CheckDTest(End);
                if (dTestTest)
                {
                    polyline.Add(End);
                    Error = NextError;
                    Point = End;
                    End = NextPoint;
                    Vec0 = Vec1;
                    return true;
                }
                else return false;
            }
            else
            {
                polyline.Add(End);
                Error = NextError;
                Point = End;
                End = NextPoint;
                Vec0 = Vec1;
                return true;
            }
        }

        /// <summary>
        ///Tests if point is within the specified distance of dTest to the testPoints
        /// </summary>
        private bool CheckDTest(Point3d point)
        {
            BoundingBox testBox = new BoundingBox(point - new Vector3d(DTest * 0.99, DTest * 0.99, DTest * 0.99), point + new Vector3d(DTest * 0.99, DTest * 0.99, DTest * 0.99));
            for (int i = 0; i < CheckPts.Count; i++)
            {
                if (testBox.Contains(CheckPts[i]))
                {
                    if (CheckPts[i].DistanceTo(point) > DTest) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Evaluates the direction of the field using the method within PrincipalMesh
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private Vector3d Evaluate(Point3d point)
        {
            Vector3d vector = new Vector3d();
            PrincipalMesh.Evaluate(point, ref vector);
            return vector;
        }

        /// <summary>
        /// solves a single step for the integration streamline method
        /// </summary>
        private double SolveStep(Point3d start, out Point3d end, int method, int direction)
        {

            if (method == 1)
            {
                throw new NotImplementedException("Euler integration is no longer implemented");
                //return SolveEuler(start, out end, direction);
            }

            if (method == 2)
            {
                throw new NotImplementedException("RK 2 integration is no longer implemented");
                //test = solveRK2(start, ref end, stepSize, direction);
            }

            if (method == 3)
            {
                throw new NotImplementedException("RK 3 integration is no longer implemented");
                //test = solveRK3(start, ref end, stepSize, direction);
            }

            if (method == 4)
            {
                return SolveRK4(start, out end, direction);
            }
            end = new Point3d();
            return -1;
        }

        /* THIS HAS BEEN MOTHBALLED AS ONLY THE RK4 INTEGRATOR IS USEFUL AT THIS POINT
        /// <summary>
        /// solves a single step using the Euler Method, and estimates the error using trapezoidal rule to compare the estimated results
        /// </summary>
        private double SolveEuler(Point3d start, out Point3d end, int direction)
        {
            //evaluate start point
            Vector3d vectorAtStart = Evaluate(start);
            Vector3d vectorFromStart = vectorAtStart / vectorAtStart.Length * StepSize * direction;

            //find end point
            end = start + vectorFromStart;

            //evaluate end point
            Vector3d vectorAtEnd = Evaluate(end);
            Vector3d vectorFromEnd = vectorAtEnd / vectorAtEnd.Length * StepSize * direction;

            //calculate error
            return ((vectorFromStart - vectorFromEnd) / 2).Length;
        }


        /// <summary>
        ///solves a single step for the RK4 Heun's method integration method
        /// <summary>
        public bool solveRK2(Point3d start, ref Point3d end, double stepSize, int direction)
        {
            Vector3d k1 = new Vector3d();
            Vector3d k2 = new Vector3d();
            Vector3d final = new Vector3d();
            double angle;

            //find vector k1
            bool test1 = evaluate(start, ref k1);

            //find vector k2
            Point3d p1 = start + (k1 / k1.Length) * (stepSize) * direction;
            bool test2 = evaluate(p1, ref k2);
            angle = Math.Abs(Vector3d.VectorAngle(k1, k2));
            if (angle > Math.PI * 0.75) k2 = -k2;

            if (test1 && test2)
            {
                final = (k1 + k2) / 2;
                end = start + (final / final.Length) * (stepSize) * direction;
                return true;
            }
            else return false;
        }

        /// <summary>
        ///solves a single step for the RK3 Heun's method integration method
        /// <summary>
        public bool solveRK3(Point3d start, ref Point3d end, double stepSize, int direction)
        {
            Vector3d k1 = new Vector3d();
            Vector3d k2 = new Vector3d();
            Vector3d k3 = new Vector3d();
            Vector3d final = new Vector3d();
            double angle;

            //find vector k1
            bool test1 = evaluate(start, ref k1);

            //find vector k2
            Point3d p1 = start + (k1 / k1.Length) * (stepSize / 3) * direction;
            bool test2 = evaluate(start, ref k2);
            angle = Math.Abs(Vector3d.VectorAngle(k1, k2));
            if (angle > Math.PI * 0.75) k2 = -k2;

            //find vector k3
            Point3d p2 = start + (k2 / k2.Length) * (stepSize * 2 / 3) * direction;
            bool test3 = evaluate(start, ref k3);
            angle = Math.Abs(Vector3d.VectorAngle(k1, k3));
            if (angle > Math.PI * 0.75) k3 = -k3;

            if (test1 && test2 && test3)
            {
                final = (k1 + 3 * k3) / 4;
                end = start + (final / final.Length) * (stepSize) * direction;
                return true;
            }
            else return false;
        }
         */

        /// <summary>
        /// solves a single step using the Runge-Kutta 4th order Method and estimates the error by comparing the result to that produced from a third order method;
        /// See: Fast and Resolution Independent Line Integral Convolution; by Detlev Stalling and Hans-Christian Hege
        /// </summary>
        private double SolveRK4(Point3d start, out Point3d end, int direction)
        {
            double angle;

            //find vector at the start
            Vector3d vectorAtStart = Evaluate(start);
            Vector3d vectorFromStart = vectorAtStart / vectorAtStart.Length * StepSize * direction;

            //find point1 and the vector at point1
            Point3d point1 = start + vectorFromStart / 2;
            Vector3d vectorAtPoint1 = Evaluate(point1);
            angle = Math.Abs(Vector3d.VectorAngle(vectorAtStart, vectorAtPoint1));
            if (angle > Math.PI * 0.75) vectorAtPoint1 = -vectorAtPoint1;
            Vector3d vectorFromPoint1 = vectorAtPoint1 / vectorAtPoint1.Length * StepSize * direction;

            //find vector k3
            Point3d point2 = start + vectorFromPoint1 / 2;
            Vector3d vectorAtPoint2 = Evaluate(point2);
            angle = Math.Abs(Vector3d.VectorAngle(vectorAtStart, vectorAtPoint2));
            if (angle > Math.PI * 0.75) vectorAtPoint2 = -vectorAtPoint2;
            Vector3d vectorFromPoint2 = vectorAtPoint2 / vectorAtPoint2.Length * StepSize * direction;

            //find vector k4
            Point3d point3 = start + vectorFromPoint2;
            Vector3d vectorAtPoint3 = Evaluate(point3);
            angle = Math.Abs(Vector3d.VectorAngle(vectorAtStart, vectorAtPoint3));
            if (angle > Math.PI * 0.75) vectorAtPoint3 = -vectorAtPoint3;
            Vector3d vectorFromPoint3 = vectorAtPoint3 / vectorAtPoint3.Length * StepSize * direction;

            //find final location
            end = start + (vectorFromStart + 2 * vectorFromPoint1 + 2 * vectorFromPoint2 + vectorFromPoint3) / 6;
            end = Mesh.ClosestPoint(end); //this is to prevent the streamline from going off the mesh

            //evaluate end point
            Vector3d vectorAtEnd = Evaluate(end);
            angle = Math.Abs(Vector3d.VectorAngle(vectorAtStart, vectorAtEnd));
            if (angle > Math.PI * 0.75) vectorAtEnd = -vectorAtEnd;
            Vector3d vectorFromEnd = vectorAtEnd / vectorAtEnd.Length * StepSize * direction;

            return ((vectorFromPoint3 - vectorFromEnd) / 6).Length;
        }


        /// <summary>
        /// Performs the initial triangulation of the boundary
        /// </summary>
        /// <param name="poly">A boundary polyline.</param>
        /// <returns>A delaunay triangulation.</returns>
        private TriangleNet.Mesh InitialTriangulate(Polyline poly)
        {
            if (!poly.IsClosed)
            {
                throw new ArgumentException("Polyline must be closed");
            }

            // Create input geometry from perimeter polyline
            TriGeom = new TriangleNet.Geometry.InputGeometry();
            // Add the points of the polyline into the geom
            for (int i = 0; i < poly.Count; i++)
            {
                TriGeom.AddPoint(Math.Round(poly[i].X, 6), Math.Round(poly[i].Y, 6));
                //Add new points into the checkPts list so that the streamline ends if it is too close to another streamline - THIS NEEDS UPDATING FOR MESHS WHICH ARE NOT A ON A PLANE 
                CheckPts.Add(poly[i]);
            }
            // Create the connectivity between the points in geom
            for (int i = 0; i < poly.Count; i++)
            {
                TriGeom.AddSegment(i, (i + 1) % poly.Count);
            }

            // Create triangulated mesh from input geometry
            var mesh = new TriangleNet.Mesh();
            mesh.Triangulate(TriGeom);

            return mesh;
        }

        /// <summary>
        /// Inserts a new streamline and performs triangulation
        /// </summary>
        /// <param name="poly">A boundary polyline.</param>
        /// <returns>A delaunay triangulation.</returns>
        private TriangleNet.Mesh InsertStreamline(Polyline poly)
        {
            int ptsStartCount = TriGeom.Points.Count();

            // Add the points of the polyline into the geom
            for (int i = 0; i < poly.Count; i++)
            {
                TriGeom.AddPoint(Math.Round(poly[i].X, 6), Math.Round(poly[i].Y, 6));
            }
            //// Create the connectivity between the points in geom
            //for (int i = 0; i < poly.Count - 1; i++)
            //{
            //    TriGeom.AddSegment(ptsStartCount + i, ptsStartCount+ i + 1);
            //}

            // Create triangulated mesh from input geometry
            var mesh = new TriangleNet.Mesh();
            mesh.Triangulate(TriGeom);

            return mesh;
        }

        /// <summary>
        /// Creates a circumcircle for every triangle in the mesh
        /// </summary>
        /// <param name="tmesh"></param>
        /// <returns></returns>
        private List<Circle> Circumcircles(TriangleNet.Mesh tmesh)
        {
            // Circumcircles
            var circles = new List<Circle>(tmesh.Triangles.Count);
            var vs = tmesh.Vertices.ToArray();
            Vertex v0, v1, v2;
            Circle c;
            foreach (TriangleNet.Geometry.ITriangle tri in tmesh.Triangles)
            {
                v0 = vs[tri.P0];
                v1 = vs[tri.P1];
                v2 = vs[tri.P2];

                //Test for isosceles triangles
                //create vector for each side
                Vector3d side0 = new Vector3d(v0.X - v2.X, v0.Y - v2.Y, 0.0);
                Vector3d side1 = new Vector3d(v1.X - v0.X, v1.Y - v0.Y, 0.0);
                Vector3d side2 = new Vector3d(v2.X - v1.X, v2.Y - v1.Y, 0.0);
                Vector3d[] sides = { side0, side1, side2 };

                //measure angle between adjecent sides, break if angle is greather than PI*3/5
                bool largeAngle = false;
                for (int i = 0; i < sides.Length; i++)
                {
                    double angle = Vector3d.VectorAngle(-1*sides[(i + sides.Length - 1) % sides.Length], sides[i]);
                    if(angle > Math.PI*3/5) largeAngle = true;
                }
                if (largeAngle) continue;

                //create circumscribed circle
                c = new Circle(
                  new Point3d(v0.X, v0.Y, 0.0),
                  new Point3d(v1.X, v1.Y, 0.0),
                  new Point3d(v2.X, v2.Y, 0.0)
                  );

                circles.Add(c);
            }

            return circles;
        }


    }
}
