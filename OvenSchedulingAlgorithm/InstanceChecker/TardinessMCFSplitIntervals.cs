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
    public class TardinessMCFSplitIntervals
    {
        public static long ComputeLowerBoundTardinessWithMCFSplitIntervals(IInstance instance)
        {
            long tardiness = 0;

            MinCostFlow minCostFlow = new MinCostFlow();

            // Data structure to store the information on the demand nodes (machine-interval).
            IDictionary<int, SortedSet<int>> machineIntervalNodeAssignedJobs = new Dictionary<int, SortedSet<int>>();
            IDictionary<int, int> machineIntervalNodeMinSetupAndProcessingTime = new Dictionary<int, int>();
            IDictionary<int, int> machineIntervalNodeLengthInterval = new Dictionary<int, int>();
            IDictionary<int, int> machineIntervalOriginalMachine = new Dictionary<int, int>();
            IDictionary<int, int> machineIntervalOriginalInterval = new Dictionary<int, int>();
            
            // minProcessingTime is initialized with the max Min Processing time
            int maxMinTime = instance.Jobs[0].MinTime;
            foreach (IJob job in instance.Jobs)
            {
                if (job.MinTime > maxMinTime)
                {
                    maxMinTime = job.MinTime;
                }
            }

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

            int machineIntervals = 0;
            int maxShiftNumber = 0;
            foreach (int machineId in instance.Machines.Keys)
            {
                if (instance.Machines[machineId].AvailabilityStart.Count > maxShiftNumber)
                {
                    maxShiftNumber = instance.Machines[machineId].AvailabilityStart.Count;
                }
            }
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
                    machineIntervals = machineIntervals + 1;
                    int nodeId = instance.Jobs.Count + (machineId - 1) * maxShiftNumber + shiftId + 1;
                    machineIntervalNodeAssignedJobs.Add(nodeId, new SortedSet<int>()); //TODO Francesca: error "An item with the same key has already been added."
                    machineIntervalNodeMinSetupAndProcessingTime.Add(nodeId, maxMinTime);
                    int lengthInterval = (int)
                    System.Math.Abs((instance.Machines[machineId].AvailabilityEnd[shiftId] - instance.Machines[machineId].AvailabilityStart[shiftId]).TotalSeconds);
                    machineIntervalNodeLengthInterval.Add(nodeId, lengthInterval);
                    machineIntervalOriginalMachine.Add(nodeId, machineId);
                    machineIntervalOriginalInterval.Add(nodeId, shiftId);
                }
            } 

            // Detect the properties of the first level graph -> no subdivision of machine-interval in machine-sub-intervals
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
                        // Thus you can update the machineIntervalNode data structures
                        int nodeId = instance.Jobs.Count + (machineId - 1) * maxShiftNumber + shiftId + 1;
                        machineIntervalNodeAssignedJobs[nodeId].Add(job.Id);
                        int processingTime = job.MinTime + setupTime;
                        if (machineIntervalNodeMinSetupAndProcessingTime[nodeId] > processingTime)
                        {
                            machineIntervalNodeMinSetupAndProcessingTime[nodeId] = processingTime;
                        }
                        // Console.WriteLine("job: " + job.Id + " machineId: " + machineId + " nodeId: " + nodeId);
                    }
                }
            }

            // At this point you can operate with the second graph and work on the edges
            int edgeIndex = 0;
            int subIntervalIndex = instance.Jobs.Count();
            IDictionary<int, int> subIntervalSupply = new Dictionary<int, int>();
            // edgesInformation.Add(edgeIndex, (jobId, subIntervalIndex, machineId, shiftId, machineIntervalId, costInteger, cost, capacity));
            IDictionary<int, (int jobId,int subIntervalId,int machineId,int shiftId,int machineIntervalId,int costInteger, double cost,int capacity)> edgesInformation = new Dictionary<int, (int,int,int,int,int,int,double,int)>();

            // foreach(IJob job in instance.Jobs)
            // {
            //     Console.WriteLine("jobId: " + job.Id + " start: " + job.EarliestStart);
            // }
            
            foreach(KeyValuePair<int, SortedSet<int>>  machineIntervalNode in machineIntervalNodeAssignedJobs)
            {
                int machineIntervalId = machineIntervalNode.Key;
                SortedSet<int> assignedJobs = machineIntervalNode.Value;
                int machineId = machineIntervalOriginalMachine[machineIntervalId];
                int shiftId = machineIntervalOriginalInterval[machineIntervalId];
                DateTime startInterval = instance.Machines[machineId].AvailabilityStart[shiftId];
                DateTime endInterval = instance.Machines[machineId].AvailabilityEnd[shiftId];
                DateTime subIntervalStart = instance.Machines[machineId].AvailabilityStart[shiftId];
                DateTime subIntervalEnd = instance.Machines[machineId].AvailabilityStart[shiftId];
                int durationInterval = machineIntervalNodeLengthInterval[machineIntervalId];
                int subIntervalDuration = machineIntervalNodeMinSetupAndProcessingTime[machineIntervalId];
                // Console.WriteLine("machineId: " + machineId + " intervalId: " + shiftId + " durationInterval: " + durationInterval);
                while(durationInterval > 0)
                {
                    subIntervalIndex = subIntervalIndex + 1;
                    subIntervalSupply.Add(subIntervalIndex,instance.Machines[machineId].MaxCap);
                    int realSubIntervalDuration = Math.Min(durationInterval,subIntervalDuration);
                    subIntervalStart = subIntervalEnd;
                    subIntervalEnd = subIntervalStart.AddSeconds(realSubIntervalDuration);
                    durationInterval = durationInterval - realSubIntervalDuration;
                    // Console.WriteLine("Subinterval // start: " + subIntervalStart + " end: " + subIntervalEnd);
                    // for the jobs assigned to the machineInterval
                    foreach(int jobId in assignedJobs)
                    {
                        int accessId = jobId - 1;
                        // FIXME: jobId as selector.... idem for machineId
                        DateTime earliestStart = instance.Jobs[accessId].EarliestStart;
                        DateTime latestEnd = instance.Jobs[accessId].LatestEnd;
                        int setupTime = maxSetupTime;
                        int jobAttribute = instance.Jobs[accessId].AttributeIdPerMachine[machineId]-1;
                        foreach(var attribute in instance.Attributes)
                        {  
                            if (setupTime > attribute.Value.SetupTimesAttribute[jobAttribute])
                            {
                                setupTime = attribute.Value.SetupTimesAttribute[jobAttribute];
                            }
                        }
                        int minTime = instance.Jobs[accessId].MinTime;
                        DateTime realStart = instance.Jobs[accessId].EarliestStart;
                        if (subIntervalStart.AddSeconds(setupTime) > realStart)
                        {
                            realStart = subIntervalStart.AddSeconds(setupTime);
                        }
                        // if (releaseDateJob - SetUpJob < endSubInterval) AND (actualStartJob + minProcessingtime < EndInterval)
                        if( (earliestStart.AddSeconds(-setupTime) > subIntervalEnd) || (realStart.AddSeconds(minTime)> endInterval))
                        {
                            continue;
                        }
                        // if you are here, this means that an edge exist
                        // is the job tardy?
                        double cost = 0.0;
                        if(realStart.AddSeconds(minTime) > latestEnd)
                        {
                            cost = (double) 1/instance.Jobs[accessId].Size;
                        }
                        int capacity = instance.Jobs[accessId].Size;
                        int costInteger = (int) (1000000 * cost);
                        // updateInformation on the edges
                        edgesInformation.Add(edgeIndex, (jobId, subIntervalIndex, machineId, shiftId, machineIntervalId, costInteger, cost, capacity));
                        edgeIndex = edgeIndex + 1;
                    }
                }
            }

            // Instance format
            // Information on the supply nodes (this is the job)
            // The +1 is for the dummy nodes
            int nodesTotal = subIntervalSupply.Count() + instance.Jobs.Count() + 1;
            int dummyIndex = subIntervalSupply.Count() + instance.Jobs.Count();
            // Console.WriteLine("nodesTotal: " + nodesTotal);
            
            // supplies
            int[] supplies = new int[nodesTotal]; 
            // Information on the supply nodes (jobs)
            foreach(IJob job in instance.Jobs)
            {
                supplies[job.Id-1] = job.Size;
            }
            foreach(KeyValuePair<int, int> subIntervalNode in subIntervalSupply)
            {
                supplies[subIntervalNode.Key - 1] = -subIntervalNode.Value;
            }
            supplies[dummyIndex] = -supplies.Sum();
            int dummyEdges = subIntervalSupply.Count();

            int[] startNodes = new int[edgeIndex + dummyEdges];
            int[] endNodes = new int[edgeIndex + dummyEdges]; 
            int[] capacities = new int[edgeIndex + dummyEdges]; 
            int[] unitCosts =  new int[edgeIndex + dummyEdges];

            // populate the edges with information
            foreach(var arc in edgesInformation)
            {
                startNodes[arc.Key] = arc.Value.jobId-1;
                endNodes[arc.Key] = arc.Value.subIntervalId-1;
                capacities[arc.Key] = arc.Value.capacity;
                unitCosts[arc.Key] = arc.Value.costInteger;
            }
            // populate dummy indexes
            foreach(KeyValuePair<int, int> subIntervalNode in subIntervalSupply)
            {
                startNodes[edgeIndex] = dummyIndex;
                endNodes[edgeIndex] = subIntervalNode.Key-1;
                capacities[edgeIndex] = subIntervalNode.Value;
                unitCosts[edgeIndex] = 0;
                edgeIndex = edgeIndex + 1;
            }


            // printing
            // Console.WriteLine("Start nodes --> [{0}]", string.Join(", ", startNodes));
            // Console.WriteLine("End nodes --> [{0}]", string.Join(", ", endNodes));
            // Console.WriteLine("Unit costs --> [{0}]", string.Join(", ", unitCosts));
            // Console.WriteLine("capacities --> [{0}]", string.Join(", ", capacities));
            // Console.WriteLine("Supplies --> [{0}]", string.Join(", ", supplies));


            // // Building the graph
            // // Add each arc.
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
            // else, LB set to the minimum

            return tardiness;
        } // public static long ComputeLowerBoundTardinessWithMCF(IInstance instance)
    } // public class TardinessMCFSplitIntervals
}