using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;

using Google.OrTools.Graph;

using OvenSchedulingAlgorithm.Converter;
using OvenSchedulingAlgorithm.Interface;
using OvenSchedulingAlgorithm.InstanceGenerator;
using OvenSchedulingAlgorithm.Converter.Implementation;
using OvenSchedulingAlgorithm.Interface.Implementation;


namespace OvenSchedulingAlgorithm.InstanceChecker
{
    public class TardinessMCF
    {
        public static long ComputeLowerBoundTardinessWithMCF(IInstance instance)
        {
            long tardiness = 0;

            MinCostFlow minCostFlow = new MinCostFlow();

            // Instance preparation

            // Data structure to store the information on the demand nodes (machine-interval). I need to store the minimum processing time of the jobs and the number of assigned jobs. This is later useful to calculate the real capacity. We use separate dictionaries and not a dictionary of tuples, because we need to be able to update the values
            IDictionary<int, int> machineIntervalNodeAssignedJobs = new Dictionary<int, int>();
            IDictionary<int, int> machineIntervalNodeMinProcessingTime = new Dictionary<int, int>();
            IDictionary<int, int> machineIntervalNodeLengthInterval = new Dictionary<int, int>();
            IDictionary<int, int> machineIntervalOriginlMachine = new Dictionary<int, int>();
            IDictionary<int, SortedSet<int>> machineIntervalNodeProcessingTimes = new Dictionary<int, SortedSet<int>>();

            // minProcessingTime is initialized with the max Min Processing time
            int maxMinTime = instance.Jobs[0].MinTime;
            foreach (IJob job in instance.Jobs)
            {
                if (job.MinTime > maxMinTime)
                {
                    maxMinTime = job.MinTime;
                }
            }
            
            // Retrieve the maximum possible setup times
            int maxSetupTime = instance.Attributes[1].SetupTimesAttribute[1];
            foreach (KeyValuePair<int, IAttribute> attribute in instance.Attributes)
            {
                for (int attributeIndex = 0;  attributeIndex < attribute.Value.SetupTimesAttribute.Count; attributeIndex++)
                {
                    if (maxSetupTime < attribute.Value.SetupTimesAttribute[attributeIndex])
                    {
                        maxSetupTime = attribute.Value.SetupTimesAttribute[attributeIndex]; 
                    }
                }
            }
            maxMinTime = maxMinTime + maxSetupTime;

            foreach (int machineId in instance.Machines.Keys)
            {
                for (int shiftId = 0; shiftId < instance.Machines[machineId].AvailabilityStart.Count; shiftId++)
                {
                    // The id of the node is calculated as: n + (currentMachine - 1) * s + currentShift + 1
                    // Example with 10 jobs, 2 machines, 3 shifts
                    // currentMachine currentShift arcId
                    //       1               0       11 --> (10 + (1-1)*3 + 0 + 1)
                    //       1               1       12 --> (10 + (1-1)*3 + 1 + 1)
                    //       1               2       13 --> (10 + (1-1)*3 + 2 + 1)
                    //       2               0       14 --> (10 + (2-1)*3 + 0 + 1)
                    //       2               1       15 --> (10 + (2-1)*3 + 1 + 1)
                    //       2               2       16 --> (10 + (2-1)*3 + 2 + 1)
                    int nodeId = instance.Jobs.Count + (machineId - 1) * instance.Machines[machineId].AvailabilityStart.Count + shiftId + 1;
                    machineIntervalNodeAssignedJobs.Add(nodeId, 0);
                    machineIntervalNodeMinProcessingTime.Add(nodeId, maxMinTime);
                    machineIntervalNodeProcessingTimes.Add(nodeId, new SortedSet<int>());
                    int lengthInterval = (int)
                    System.Math.Abs((instance.Machines[machineId].AvailabilityEnd[shiftId] - instance.Machines[machineId].AvailabilityStart[shiftId]).TotalSeconds);
                    machineIntervalNodeLengthInterval.Add(nodeId, lengthInterval);
                    machineIntervalOriginlMachine.Add(nodeId, machineId);
                }
            } 

            // Store in a dictionary all the properties of the edges. It is okay to have a dictionary of tuples, since the tuples will not be mutated after we inserted them in the dictionary.
            int arcId = 0;
            IDictionary<int, (int jobNode, int machine, int interval, int machineIntervalNode, int unitCostInteger, double unitCost, int capacity)> edgesInformarion = new Dictionary<int, (int, int, int, int, int, double, int)>();
            // For every job 
            foreach (IJob job in instance.Jobs)
            {
                // For every machine (we iterate over all machine each time just to be sure on the countings for the indexes)
                foreach (int machineId in instance.Machines.Keys)
                {
                    // Check if the machine is eligible w.r.t. the machine
                    if (!job.EligibleMachines.Contains(machineId))
                    {
                        continue;
                    }
                    // For every interval of the machine
                    for (int shiftId = 0; shiftId < instance.Machines[machineId].AvailabilityStart.Count; shiftId++)
                    {
                        // Check if the job fits or not in the interval / the interval is available for the job
                        // Not ok: If the release time of the job is after the machine end
                        if (job.EarliestStart >= instance.Machines[machineId].AvailabilityEnd[shiftId])
                        {
                            continue;
                        }
                        // Not ok: If the setup time + minimum processing time does not fit in the interval
                        int setupTime = maxSetupTime;
                        int jobAttribute = job.AttributeIdPerMachine[machineId]-1;
                        foreach(var attribute in instance.Attributes)
                        { 
                            if (setupTime > attribute.Value.SetupTimesAttribute[jobAttribute])
                            {
                                setupTime = attribute.Value.SetupTimesAttribute[jobAttribute];
                            }
                        }
                        // If you are using the setup time w.r.t. the initial state of the machine, then uncomment the following line.
                        // setupTime = instance.Attributes[instance.InitialStates[machineId]].SetupTimesAttribute[jobAttribute];
                        DateTime realStart = job.EarliestStart;
                        if (instance.Machines[machineId].AvailabilityStart[shiftId].AddSeconds(setupTime) > realStart)
                        {
                            realStart  = instance.Machines[machineId].AvailabilityStart[shiftId].AddSeconds(setupTime);
                        }
                        DateTime realEnd = realStart.AddSeconds(job.MinTime);
                        if (realEnd > instance.Machines[machineId].AvailabilityEnd[shiftId])
                        {
                            continue;
                        }
                        // If you reach this stage you know that there is an edge between the job and the machine-interval
                        // Thus you can update the machineIntervalNode dictionaries
                        int nodeId = instance.Jobs.Count + (machineId - 1) * instance.Machines[machineId].AvailabilityStart.Count + shiftId + 1;
                        machineIntervalNodeAssignedJobs[nodeId] = machineIntervalNodeAssignedJobs[nodeId] + 1;
                        int processingTime = job.MinTime + setupTime;
                        if (machineIntervalNodeMinProcessingTime[nodeId] > processingTime)
                        {
                            machineIntervalNodeMinProcessingTime[nodeId] = processingTime;
                        }
                        machineIntervalNodeProcessingTimes[nodeId].Add(processingTime);
                        
                        // You have to attribute the cost
                        double cost = 0.0;
                        if (realEnd > job.LatestEnd)
                        {
                            cost = (double) 1/job.Size;
                        }
                        int capacity = job.Size;
                        int costInteger = (int) (1000000 * cost); // FIXME: discuss with Mimi
                        // Update edges dictionary
                        edgesInformarion.Add(arcId, (job.Id, machineId, shiftId, nodeId, costInteger, cost, capacity));
                        // Update the arcId
                        arcId = arcId + 1;
                    }
                }
            }

            // Information on the supply nodes (this is the job)
            // The +1 is for the dummy nodes
            int nodesTotal = instance.Jobs.Count + machineIntervalNodeLengthInterval.Count +1;
            int[] supplies = new int[nodesTotal]; 

            // // Information on the supply nodes (jobs)
            foreach(IJob job in instance.Jobs)
            {
                supplies[job.Id-1] = job.Size;
            }
            // Information on the demand nodes (machineIntervalNode dictionaries)
            foreach(KeyValuePair<int, int>  machineIntervalNode in machineIntervalNodeMinProcessingTime)
            {
                int machineCapacity = instance.Machines[machineIntervalOriginlMachine[machineIntervalNode.Key]].MaxCap;

                // int nBatches = (int) machineIntervalNodeAssignedJobs[machineIntervalNode.Key];   
                // if ((machineIntervalNodeLengthInterval[machineIntervalNode.Key] > machineIntervalNode.Value) && nBatches > (int) (machineIntervalNodeLengthInterval[machineIntervalNode.Key] / machineIntervalNode.Value))
                // {
                //     nBatches = (int) (machineIntervalNodeLengthInterval[machineIntervalNode.Key] / machineIntervalNode.Value);
                // }
                int nBatches = 0;
                int interval = machineIntervalNodeLengthInterval[machineIntervalNode.Key];
                while(interval > 0 && machineIntervalNodeAssignedJobs[machineIntervalNode.Key]!= 0)
                {
                    int nextProcessing = machineIntervalNodeProcessingTimes[machineIntervalNode.Key].First();
                    if (interval - nextProcessing < 0)
                    {
                        break;
                    }
                    interval = interval - nextProcessing;
                    nBatches = nBatches + 1;
                }

                int supply = machineCapacity * nBatches;
                supplies[machineIntervalNode.Key-1] = -supply;
            }

            int dummySupply = supplies.Sum();
            supplies[nodesTotal-1] = -dummySupply;
        

            // arcId = corresponds to the number of edges present in the network I want to create
            // you need to have also the dummy node
            int dummyEdgesCount = instance.Machines[1].AvailabilityStart.Count * instance.Machines.Count;
            int[] startNodes = new int[arcId + dummyEdgesCount];
            int[] endNodes = new int[arcId + dummyEdgesCount]; 
            int[] capacities = new int[arcId + dummyEdgesCount]; 
            int[] unitCosts =  new int[arcId + dummyEdgesCount]; 
            // Information on the edges
            foreach(var arc in edgesInformarion)
            {
                startNodes[arc.Key] = arc.Value.jobNode-1;
                endNodes[arc.Key] = arc.Value.machineIntervalNode-1;
                capacities[arc.Key] = arc.Value.capacity;
                unitCosts[arc.Key] = arc.Value.unitCostInteger;
                //Console.WriteLine("Arc " + arc.Key + "--> job: " + arc.Value.jobNode + ", machine: " + arc.Value.machine + ", shift: " + arc.Value.interval + ", machine-interval: " + arc.Value.machineIntervalNode + ", cost (double): " + arc.Value.unitCost + ", cost (integer): " + arc.Value.unitCostInteger + ", capacity: " + arc.Value.capacity); 
            }
            int dummyEndNode = instance.Jobs.Count;
            for(int arc = arcId; arc < arcId + dummyEdgesCount; arc++)
            {
                startNodes[arc] = nodesTotal-1; // this is the dummy Node
                endNodes[arc] = dummyEndNode;
                dummyEndNode = dummyEndNode + 1;
                capacities[arc] = -dummySupply;
                unitCosts[arc] = 0;
            }

            // Building the graph
            // Add each arc.
            for (int i = 0; i < startNodes.Length; ++i)
            {
                int arc =
                    minCostFlow.AddArcWithCapacityAndUnitCost(startNodes[i], endNodes[i], capacities[i], unitCosts[i]);
                if (arc != i)
                    throw new Exception("Internal error");
            }

            // Add node supplies.
            for (int i = 0; i < supplies.Length; ++i)
            {
                minCostFlow.SetNodeSupply(i, supplies[i]);
            }

            // Find the min cost flow.
            MinCostFlow.Status status = minCostFlow.Solve();
            // Optimal status
            if (status == MinCostFlow.Status.OPTIMAL)
            {
                double myCost = (double) ((minCostFlow.OptimalCost())/1000000.0);
                tardiness = Convert.ToInt64(Math.Ceiling(myCost));
            }
            // else, LB set to the minimum, that is 0

            return tardiness;
        } // public static long ComputeLowerBoundTardinessWithMCF(IInstance instance)
    } // public class TardinessMCF
}