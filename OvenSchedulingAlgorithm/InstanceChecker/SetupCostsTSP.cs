using System;
using System.Collections.Generic;
using System.Linq;
using Google.OrTools.ConstraintSolver;
using Google.OrTools.Sat;
using OvenSchedulingAlgorithm.Converter.Implementation;
using OvenSchedulingAlgorithm.Converter;
using OvenSchedulingAlgorithm.Interface;
using OvenSchedulingAlgorithm.Interface.Implementation;
using OvenSchedulingAlgorithm.InstanceGenerator;
using System.Reflection.PortableExecutable;

namespace OvenSchedulingAlgorithm.InstanceChecker
{
    /// <summary>
    ///   Minimal TSP for the setup costs objective using distance matrix.
    /// </summary>
    public class SetupCostsTSP
    {
        public class DataModel
        {

            public long[,] DistanceMatrix { get; }

            public int VehicleNumber { get; }

            public int[] StartLocations { get; }

            public int[] EndLocations { get; }

            public Dictionary<int, int[]> EligibleVehicles { get; }

            public DataModel(long[,] distanceMatrix, int vehicleNumber, int[] startLocations, int[] endLocations, Dictionary<int, int[]> eligibleVehicles)
            {
                DistanceMatrix = distanceMatrix;
                VehicleNumber = vehicleNumber;
                StartLocations = startLocations;
                EndLocations = endLocations;
                EligibleVehicles = eligibleVehicles;
            }

        };        

        private static DataModel ManuallyCreatedData()
        {
            long[,]  distanceMatrix = {
                { 0, 0, 6, 6, 14, 14, 14, 14, 14, 14 }, //start and end of first vehicle/machine
                { 0, 0, 10, 10, 10, 10, 10, 10, 10, 10}, //start and end of second vehicle/machine
                { 0, 0, 6, 6, 14, 14, 14, 14, 14, 14},
                { 0, 0, 6, 6, 14, 14, 14, 14, 14, 14},
                { 0, 0, 10, 10, 10, 10, 10, 10, 10, 10},
                { 0, 0, 10, 10, 10, 10, 10, 10, 10, 10},
                { 0, 0, 10, 10, 10, 10, 10, 10, 10, 10},
                { 0, 0, 10, 10, 10, 10, 10, 10, 10, 10},
                { 0, 0, 10, 10, 10, 10, 10, 10, 10, 10},
                { 0, 0, 10, 10, 10, 10, 10, 10, 10, 10}
            };
           
            int vehicleNumber = 2;
            
            int[] startLocations = new int[2] { 0, 1 };
            int[] endLocations = new int[2] { 0, 1 };

            Dictionary<int, int[]> eligibleVehicles = new Dictionary<int, int[]>()
                {
                    {2,  new int[1] { 0 } },
                    {3, new int[1] { 1 }},
                    {4, new int[2] { 0, 1 } },
                    {5, new int[2] { 0, 1 }},
                    {6, new int[1] { 1 }},
                    {7, new int[1] { 1 }},
                    {8, new int[1] { 0 }},
                    {9, new int[2] { 0, 1 }}
                };

            DataModel data = new DataModel(distanceMatrix, vehicleNumber, startLocations, endLocations, eligibleVehicles);

            return data;
    }

        public static DataModel CreateTSPData(IInstance instance, List<(int attId, IList<int> eligMachines)> eligMachBatches)
        {
            //check if triangle inequality holds for setup costs, if not adapt setup costs so that it holds
            IInstance corrInstance = FulfillTriangleInequalitySetpCosts(instance);

            int vehicleNumber = corrInstance.Machines.Count;
            int[] startLocations = Enumerable.Range(0, vehicleNumber).ToArray();
            int[] endLocations = Enumerable.Range(0, vehicleNumber).ToArray();

            //lower bounds       
            //no longer needed, we get lower bounds from LowerBoundsCalculator
            //List<(int attId, IList<int> eligMachines)> eligMachBatches = new List<(int, IList<int>)>();
            //for (int i = 0; i < corrInstance.Attributes.Count; i++)
            //{
            //    int attributeId = corrInstance.Attributes.Keys.ToList()[i];
            //    IList<(int, IList<int>)> eligibleMachBatchesForAtt = LowerBoundsCalculator.CalculateMinBatchCountProcTime(corrInstance, attributeId).eligibleMachBatches;
            //    eligMachBatches.AddRange(eligibleMachBatchesForAtt);
            //}

            Dictionary<int, int[]> eligibleVehicles = new Dictionary<int, int[]>();
            for  (int i = 0; i < eligMachBatches.Count; i++)
            {
                //vehicle numbers start at 1
                eligibleVehicles[i+ vehicleNumber] = eligMachBatches[i].eligMachines.Select(x => x - 1).ToArray();
            }

            List<int> attributeList = corrInstance.InitialStates.Values.ToList()
                .Concat(eligMachBatches.Select(x => x.Item1).ToList()).ToList();
            //create setupcost-matrix = distance matrix
            long[,] distanceMatrix = new long[attributeList.Count, attributeList.Count];
            for (int i = 0; i < attributeList.Count; i++)
            {
                IAttribute attribute1 = corrInstance.Attributes[attributeList[i]];
                //setup costs to end locations are =0
                for (int j = 0; j < vehicleNumber; j++)
                {
                    distanceMatrix[i, j] = 0;
                }
                for (int j = vehicleNumber; j < attributeList.Count; j++)
                {
                    IAttribute attribute2 = corrInstance.Attributes[attributeList[j]];
                    distanceMatrix[i,j] = attribute1.SetupCostsAttribute[attribute2.Id-1];
                }
            }

            DataModel data = new DataModel(distanceMatrix, vehicleNumber, startLocations, endLocations, eligibleVehicles);

            return data;

        }

        /// <summary>
        ///   Print the solution.
        /// </summary>
        static void PrintSolution(DataModel data, in RoutingModel routing, in RoutingIndexManager manager, in Assignment solution)
        {
            int status = routing.GetStatus();
            if (status == 1 || status ==2 || status == 7) //found an optimal solution or found a feasible solution without optimality proof
                //don't know whta status 7 is (solution with value=0?)
            {
                Console.WriteLine("Objective: {0}", solution.ObjectiveValue());
                // Inspect solution.
                long totalRouteDistance = 0;
                for (int i = 0; i < data.VehicleNumber; ++i)
                {
                    Console.WriteLine("Route for Vehicle {0}:", i);
                    long routeDistance = 0;
                    var index = routing.Start(i);
                    while (routing.IsEnd(index) == false)
                    {
                        Console.Write("{0} -> ", manager.IndexToNode((int)index));
                        var previousIndex = index;
                        index = solution.Value(routing.NextVar(index));
                        routeDistance += routing.GetArcCostForVehicle(previousIndex, index, 0);
                    }
                    Console.WriteLine("{0}", manager.IndexToNode((int)index));
                    Console.WriteLine("Distance of the route: {0}", routeDistance);
                    totalRouteDistance += routeDistance;
                }
                Console.WriteLine("Total Route distance: {0}", totalRouteDistance);
            }
            else
            {
                Console.WriteLine("No solution found.");
                if (status == 6)
                {
                    Console.WriteLine("Instance is infeasible.");
                }
            }            
        }

        public static long ComputeLowerBoundSetupCostsWithTSP(IInstance instance, List<(int, IList<int>)> eligMachBatches)
        {
            // Instantiate the data problem.
            DataModel data = CreateTSPData(instance, eligMachBatches);

            // Create Routing Index Manager
            RoutingIndexManager manager =
                new RoutingIndexManager(data.DistanceMatrix.GetLength(0), data.VehicleNumber, data.StartLocations, data.EndLocations);

            // Create Routing Model.
            RoutingModel routing = new RoutingModel(manager);


            //assign vehicles to their starting and end position
            for (int i = 0; i < data.VehicleNumber; ++i)
            {
                routing.SetAllowedVehiclesForIndex(new int[1] { i }, i);
            }
            //add constraints for eligible machines/vehicles
            foreach (var entry in data.EligibleVehicles)
            {
                routing.SetAllowedVehiclesForIndex(entry.Value, entry.Key);
            }            


            int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
            {
                // Convert from routing variable Index to
                // distance matrix NodeIndex.
                var fromNode = manager.IndexToNode(fromIndex);
                var toNode = manager.IndexToNode(toIndex);
                return data.DistanceMatrix[fromNode, toNode];
            });

            // Define cost of each arc.
            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            // Setting first solution heuristic.
            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
            searchParameters.LogSearch = true;

            // Solve the problem.
            Assignment solution = routing.SolveWithParameters(searchParameters);

            //Print solver status
            Console.WriteLine("Solver status: {0}", routing.GetStatus().ToString());

            // Print solution on console.
            PrintSolution(data, routing, manager, solution);

            //return value of solution as lower bound for setup costs
            long bound = 0;
            int status = routing.GetStatus();
            if (status == 1 || status == 2 || status == 7) //found an optimal solution or found a feasible solution without optimality proof
                                                           //don't know whta status 7 is (solution with value=0?)
            {
                bound = solution.ObjectiveValue(); 
            }
            return bound;
        }

        public static IInstance FulfillTriangleInequalitySetpCosts(IInstance instance)
        {
            int a = instance.Attributes.Count;
            int[,] correctedSetupCosts = new int[a,a];
            //convert attribute IDs to 1..a
            IMiniZincConverter converter = new MiniZincConverter();
            Func<int, int> convertedAttributeId = converter.ConvertAttributeIdToMinizinc(instance.Attributes);
            //initialise correctedSetupCosts with values from instance
            foreach (int i in instance.Attributes.Keys)
            {
                //converted attribute ids (converted attribute Ids start at 1)
                int i2 = convertedAttributeId(i) - 1;

                foreach (int j in instance.Attributes.Keys) 
                    {
                        //converted attribute ids (converted attribute Ids start at 1)
                        int j2 = convertedAttributeId(j) - 1;
                        correctedSetupCosts[i2,j2]= instance.Attributes[i].SetupCostsAttribute[j2];
                    }
            }



            //check if triangle inequality holds for all triples
            for (int i = 0; i < a; i++)
            {
                for (int j = i+1; j < a; j++) 
                {                        
                     for (int k = j+1; k < a; k++) 
                    {
                        int ij = correctedSetupCosts[i, j];
                        int ik = correctedSetupCosts[i, k];
                        int jk = correctedSetupCosts[j, k];
                        int ji = correctedSetupCosts[j, i];
                        int kj = correctedSetupCosts[k, j];
                        int ki = correctedSetupCosts[k, i];

                        Console.WriteLine("Checking triangle inequality for {0}, {1}, {2}", i, j, k);
                        if (ik > ij + jk)
                        {
                            Console.WriteLine("Triangle inequality is not fulfilled for {0}, {1}, {2}", i, j, k);
                            ik = ij + jk;
                        }
                        if (ij > ik + kj)
                        {
                            Console.WriteLine("Triangle inequality is not fulfilled for {0}, {1}, {2}", i, k, j);
                            ij = ik + kj;
                        }
                        if (jk > ji + ik)
                        {
                            Console.WriteLine("Triangle inequality is not fulfilled for {0}, {1}, {2}", j, i, k);
                            jk = ji + ik;
                        }
                        if (ji > jk + ki)
                        {
                            Console.WriteLine("Triangle inequality is not fulfilled for {0}, {1}, {2}", j, k, i);
                            ji = jk + ki;
                        }
                        if (kj > ki + ij)
                        {
                            Console.WriteLine("Triangle inequality is not fulfilled for {0}, {1}, {2}", k,i,j);
                            kj = ki + ij;
                        }
                        if (ki > kj + ji)
                        {
                            Console.WriteLine("Triangle inequality is not fulfilled for {0}, {1}, {2}", k,j,i);
                            ki = kj + ji;
                        }

                        //set corrected setup cost values 
                        correctedSetupCosts[i, j] = ij;
                        correctedSetupCosts[i, k] = ik;
                        correctedSetupCosts[j, k] = jk;
                        correctedSetupCosts[j, i] = ji;
                        correctedSetupCosts[k, j] = kj;
                        correctedSetupCosts[k, i] = ki;
                    }
                }
            }

            //create instance with corrected setup costs
            IDictionary<int, IAttribute> correctedAttributes = new Dictionary<int, IAttribute>();
            foreach (int i in instance.Attributes.Keys)
            {
                IAttribute att = instance.Attributes[i];
                var correctedSetupCostsList = Enumerable.Range(0, correctedSetupCosts.GetLength(0))
                    .Select(x => correctedSetupCosts[convertedAttributeId(i) - 1, x]).ToList();
                correctedAttributes[i] = new Interface.Implementation.Attribute(att.Id, att.Name, correctedSetupCostsList, att.SetupTimesAttribute);
            }

            IInstance correctedInstance = new Instance(instance.Name + "With corrected setup costs",
                instance.CreationDate,
                instance.Machines,
                instance.InitialStates,
                instance.Jobs,
                correctedAttributes,
                instance.SchedulingHorizonStart,
                instance.SchedulingHorizonEnd
                );

            //correctedInstance.Serialize("corrected instance.json");


            return correctedInstance;
        }
    }
}



