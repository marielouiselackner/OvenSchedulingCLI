using OvenSchedulingAlgorithm.Converter.Implementation;
using OvenSchedulingAlgorithm.Interface;
using OvenSchedulingAlgorithm.Interface.Implementation;
using OvenSchedulingAlgorithm.Objective;
using OvenSchedulingAlgorithm.Objective.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OvenSchedulingAlgorithm.InstanceChecker
{
    public static class LowerBoundsCalculator
    {
        /// <summary>
        /// Calculates lower bounds on the value of the objective function for a given instance
        /// </summary>
        /// <param name="instance">the given instance</param>
        /// <param name="instanceData">the given instance data obtained from preprocessing</param>
        /// <param name="config">algorithm configuration (contains weights, optionally initial states of machines)</param>
         /// <param name=""></param>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <returns></returns>
        public static LowerBounds CalculateLowerBounds(IInstance instance, InstanceData instanceData,
            IAlgorithmConfig config)
        {
            //lower bounds on batch count and processing time
            DateTime beforebatchCount = DateTime.Now;
            int lowerBoundBatchCount = 0;
            int lowerBoundTotalRuntimeSeconds = 0;
            List<(int, IList<int>)> eligMachBatches = new List<(int, IList<int>)>();
            for (int i = 0; i < instance.Attributes.Count; i++)
            {
                int attributeId = instance.Attributes.Keys.ToList()[i];
                var bounds = CalculateMinBatchCountProcTime(instance, attributeId);
                lowerBoundBatchCount += bounds.minbatchCount;
                lowerBoundTotalRuntimeSeconds += bounds.minProcTimeSeconds;
                eligMachBatches.AddRange(bounds.eligibleMachBatches);
            }
            int lowerBoundTotalRuntimeMinutes = (lowerBoundTotalRuntimeSeconds + 59) / 60;
            int lowerBoundTotalSetupTimesSeconds = 0;
            int lowerBoundTotalSetupTimesMinutes = 0;
            DateTime afterBatchCount = DateTime.Now;
            TimeSpan batchAndProcTimeLowerBounds = afterBatchCount - beforebatchCount;

            //bounds on setup costs based on TSP model
            DateTime beforeSetupCosts = DateTime.Now;
            int lowerBoundTotalSetupCosts = (int)SetupCostsTSP.ComputeLowerBoundSetupCostsWithTSP(instance, eligMachBatches);
            //CalculateSimpleLowerBoundSetupCost(instance, eligMachBatches);
            DateTime afterSetupCosts = DateTime.Now;
            TimeSpan setupCostsRuntimeLowerBounds = afterSetupCosts - beforeSetupCosts;

            //bounds on tardiness based on minimum cost flow problem
            DateTime beforeTardiness = DateTime.Now;
            int lowerBoundTardyJobs = (int) TardinessMCF.ComputeLowerBoundTardinessWithMCF(instance); //TODO: integrate Francesca's code here 
            DateTime afterTardiness = DateTime.Now;
            TimeSpan tardinessRuntimeLowerBounds = afterTardiness - beforeTardiness;


            IOutput solution = new Output();
            CalculateObjective calcObjective = new CalculateObjective(instance, solution, config.WeightsObjective, instanceData);
            IObjectiveComponents components = calcObjective.CalculateObjectiveFromComponents(
                lowerBoundTotalRuntimeMinutes * 60, //needed in seconds
                lowerBoundTotalSetupTimesMinutes * 60, //needed in seconds
                lowerBoundTotalSetupCosts,
                instanceData.LowerBoundTardyJobs,
                0,
                0,
                new Dictionary<int, IJob>());

            double upperBoundAverageNumberOfJobsPerBatch = (double)instanceData.NumberOfJobs / lowerBoundBatchCount;
            double upperBoundRuntimeReduction = (double)instanceData.UpperBoundTotalRuntimeSeconds / lowerBoundTotalRuntimeSeconds;

            LowerBounds lb = new LowerBounds(
                lowerBoundBatchCount,
                lowerBoundTotalRuntimeSeconds,
                lowerBoundTotalRuntimeMinutes,
                lowerBoundTotalSetupTimesSeconds,
                lowerBoundTotalSetupTimesMinutes,
                lowerBoundTotalSetupCosts,
                lowerBoundTardyJobs,
                instanceData.LowerBoundTardyJobs,
                0,
                components.ObjectiveValue,
                upperBoundAverageNumberOfJobsPerBatch,
                upperBoundRuntimeReduction,
                new TimeSpan(),
                batchAndProcTimeLowerBounds,
                tardinessRuntimeLowerBounds,
                setupCostsRuntimeLowerBounds
                );

            return lb;

            
        }

        /// <summary>
        /// Given an instance together with a list of initial states of machines and a list of batches (with their eligible machines),
        /// calculate lower bounds on the cumulative setup costs.
        /// </summary>
        /// <param name="instance">the given instance</param>
        /// <param name="eligMachBatches">the list of batches to be scheduled (entries consist of an attribute Id
        /// and a list of IDs of eligible machines; eligibale machines are not needed here)</param>
        /// <returns></returns>
        private static int CalculateSimpleLowerBoundSetupCost(IInstance instance, List<(int, IList<int>)> eligMachBatches)
        {
            IDictionary<int, int> minSetupAfter = new Dictionary<int, int>(); //keys are attribute IDs
            IDictionary<int, int> minSetupBefore = new Dictionary<int, int>(); //keys are attribute IDs

            //in case attribute IDs are not 1, ..., a
            var sortedAttributeIDs = instance.Attributes.Keys.OrderBy(x => x).ToList();
            for (int i = 0; i <= instance.Attributes.Count-1; i++)
            {
                //determine minimal setup cost after and before this attribute 
                int attributeId = sortedAttributeIDs[i];
                int minAfter = instance.Attributes[attributeId].SetupCostsAttribute.Min();
                minSetupAfter.Add(attributeId, minAfter);
                IList<int> setupBefore = new List<int>();
                for (int j = 0; j <= instance.Attributes.Count-1; j++)
                {   
                    int attributeId2 = sortedAttributeIDs[j];
                    setupBefore.Add(instance.Attributes[attributeId2].SetupCostsAttribute[i]);
                }
                int minBefore = setupBefore.Min();
                minSetupBefore.Add(attributeId, minBefore);
            }

            //bound based on taking the minimum setup cost before every batch
            int setupCostBefore = 0;
            for (int i = 0; i < eligMachBatches.Count; i++)
            {
                int att = eligMachBatches[i].Item1;
                setupCostBefore += minSetupBefore[att];
            }

            //bound based on taking the minimum setup cost after every batch
            List<int> setupCostAfterList = new List<int>();
            for (int i = 0; i < eligMachBatches.Count; i++)
            {
                int att = eligMachBatches[i].Item1;
                setupCostAfterList.Add(minSetupAfter[att]);
            }
            //for first setups on every machine
            foreach (int machineId in instance.Machines.Keys)
            {
                int att = instance.InitialStates[machineId];
                setupCostAfterList.Add(minSetupAfter[att]);
            }
            //sort the list of setp costs after batches in increasing order
            setupCostAfterList.Sort();
            int setupCostAfter = 0;
            //take the sum of the first b elements
            for (int i = 0; i < eligMachBatches.Count; i++)
            {
                setupCostAfter += setupCostAfterList[i];
            }
            //TODO: also for setup after

            return Math.Max(setupCostBefore, setupCostAfter);
        }

        /// <summary>
        /// Calculate the minimum number of batches required and the minimum processing time of these batches for all jobs 
        /// of a given instance that have a given attribute.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="attributeId"></param>
        /// <returns>The calculated minimum number of batches required, the calculated minimal processing time of batches,
        /// and the list of batches together with their eligible machines.</returns>
        public static (int minbatchCount, int minProcTimeSeconds, IList<(int, IList<int>)> eligibleMachBatches)
            CalculateMinBatchCountProcTime(IInstance instance, int attributeId)
        {
            //list of eligible machines of batches (required for calculation of minimal setup costs/times)
            IList<(int, IList<int>)> eligibleMachBatches = new List<(int, IList<int>)>();

            //jobs with the given attribute Id 
            //note: we assume that attributes are the same on all machines
            var jobs = instance.Jobs
                .Where(job => job.AttributeIdPerMachine[job.EligibleMachines[0]] == attributeId);
            if (!jobs.Any())
            {
                return (0, 0, eligibleMachBatches);
            }
            int minSize = jobs.Select(job => job.Size).Min();
            int maxCap = instance.Machines.Values.Select(m => m.MaxCap).Max();
            //jobs that are so large that eligible machine with maximal machine capacity 
            //cannot process any other jobs at the same time
            var largeJobs = jobs.Where(job => instance.Machines
            .Where(m => job.EligibleMachines.Contains(m.Key))
            .Select(m => m.Value.MaxCap).Max() - job.Size < minSize);
            //one batch needed per large job
            int minbatchCount = largeJobs.Count();
            int minProcTimeSeconds = 0;
            foreach (var job in largeJobs)
            {
                minProcTimeSeconds += job.MinTime;
                eligibleMachBatches.Add((attributeId, job.EligibleMachines));
            }

            //jobs that are not large are small
            var smallJobs = jobs.Where(job => !largeJobs.Contains(job));

            //calculate number of batches and minimal processing time needed for small jobs 
            //based on eligible machines
            var boundsSmallJobsEligMachines = GetBoundsSmallJobsEligMachines(smallJobs, instance.Machines, maxCap);
            //calculate number of batches and minimal processing time needed for small jobs 
            //based on compatible processing times
            var boundsSmallJobsCompProcTimes = GetBoundsSmallJobsCompProcTimes(smallJobs, maxCap);

            //add max number of batches and processing times to min
            minbatchCount += Math.Max(boundsSmallJobsEligMachines.minbatchCount, boundsSmallJobsCompProcTimes.minbatchCount);
            minProcTimeSeconds += Math.Max(boundsSmallJobsEligMachines.minProcTimeSeconds, boundsSmallJobsCompProcTimes.minProcTimeSeconds);

            //add batches for small jobs to eligibleMachBatches: take maximum value of batches 
            //if value from boundsSmallJobsCompProcTimes is chosen, take all eligible machines, 
            //otherwise take list provided by boundsSmallJobsEligMachines
            if (boundsSmallJobsEligMachines.minbatchCount < boundsSmallJobsCompProcTimes.minbatchCount)
            {
                IList<int> allMach = instance.Machines.Keys.ToList();
                for (int i = 0; i < boundsSmallJobsCompProcTimes.minbatchCount; i++)
                {
                    eligibleMachBatches.Add((attributeId, allMach));
                }
            }
            else
            {
                for (int i = 0; i < boundsSmallJobsEligMachines.eligibleMachBatches.Count; i++)
                {
                    eligibleMachBatches.Add((attributeId, boundsSmallJobsEligMachines.eligibleMachBatches[i]));
                }
            }

            return (minbatchCount, minProcTimeSeconds, eligibleMachBatches);
        }

        /// <summary>
        /// Given a set of small jobs, a dictionary of machines together with the overall max machine capacity, 
        /// find the minimum number of batches required to schedule all jobs based on the eligible machines of the jobs.
        /// For these batches, compute the minimal processing time as well.
        /// </summary>
        /// <param name="smallJobs"></param>
        /// <param name="machines"></param>
        /// <param name="maxCap"></param>
        /// <returns>(minimum number of batches required, minimal batch processing time, list of eligible machines of batches)</returns>
        private static (int minbatchCount, int minProcTimeSeconds, IList<IList<int>> eligibleMachBatches) 
            GetBoundsSmallJobsEligMachines(IEnumerable<IJob> smallJobs, IDictionary<int, IMachine> machines, int maxCap)
        {
            int minbatchCount = 0;
            int minProcTimeSeconds = 0;

            //list of eligible machines of batches (required for calculation of minimal setup costs)
            IList<IList<int>> eligibleMachBatches = new List<IList<int>>();

            var smallJobsSingleMachine = smallJobs.Where(job => job.EligibleMachines.Count == 1);
            int totalFreeCapacity = 0;
            List<int> procTimes = new List<int>();
            //all jobs that can be processed on a single machine, are scheduled on this machine
            if (smallJobsSingleMachine.Any())
            {
                foreach (int machine in machines.Keys)
                {
                    var smallJobsThisMachine = smallJobsSingleMachine.Where(job => job.EligibleMachines.Contains(machine));
                    if (!smallJobsThisMachine.Any())
                    {
                        continue;
                    }

                    //number of batches for small jobs that need to be scheduled on this machine
                    int batchSize = 0;
                    foreach (IJob job in smallJobsThisMachine)
                    {
                        batchSize += job.Size;
                        
                    }
                    double batchBoundReal = (double)batchSize / machines[machine].MaxCap;
                    int batchBoundInt = (int)Math.Ceiling(batchBoundReal);

                    minbatchCount += batchBoundInt;
                    //add batches with their eligible machine to list
                    List<int> eligMach = new List<int>();
                    eligMach.Add(machine);
                    for (int i = 0; i < batchBoundInt; i++)
                    {
                        eligibleMachBatches.Add(eligMach);
                    }

                    int freeCapacityThisMachine = batchBoundInt * machines[machine].MaxCap - batchSize;
                    totalFreeCapacity += freeCapacityThisMachine;


                    //list of all minimal processing times of jobs that need to be processed on this machine 
                    var allProcTimesThisMachine = smallJobsThisMachine.Select(j => j.MinTime).ToList();
                    allProcTimesThisMachine.Sort();
                    //add batchBoundInt many proc times to list of proc times
                    procTimes.Add(allProcTimesThisMachine.Max());
                    for (int i = 0; i < batchBoundInt - 1; i++)
                    {
                        procTimes.Add(allProcTimesThisMachine[i]);
                    }
                }
            }
            
            //what to do with the jobs that can be scheduled on more than one machine?
            //use the free capacity for all other jobs and calculate number of extra batches needed 
            //assuming that all other jobs can be scheduled on machine with largest capacity
            //total size of jobs that still need to be scheduled (or 0, if all can been added to previous batches)
            int smallJobsMultipleMachinesSize = 0;
            foreach (IJob job in smallJobs.Where(job => !smallJobsSingleMachine.Contains(job)))
            {
                smallJobsMultipleMachinesSize += job.Size;
            }
            int remainingJobsSize = Math.Max(0, smallJobsMultipleMachinesSize - totalFreeCapacity);
            int extraBatches = (int)Math.Ceiling((double)remainingJobsSize / maxCap);
            minbatchCount += extraBatches;

            //list of all minimal processing times of jobs that can be processed on multiple machines
            var allProcTimes = smallJobs
                .Where(job => !smallJobsSingleMachine.Contains(job))
                .Select(j => j.MinTime).ToList();
            allProcTimes.Sort();
            procTimes.Sort();
            //add extraBatches many proc times to list of proc times
            int batchesToAdd = extraBatches;
            //check if max of allProcTimes is larger than max(procTimes)
            if (allProcTimes.Any())
            {
                int maxProcTime = allProcTimes[allProcTimes.Count - 1];
                if (procTimes.Any() && maxProcTime > procTimes[procTimes.Count - 1])
                {
                    procTimes[procTimes.Count - 1] = maxProcTime;
                    batchesToAdd -= 1;
                }
                else if (!procTimes.Any())
                {
                    procTimes.Add(maxProcTime);
                    batchesToAdd -= 1;
                }

                for (int i = 0; i < batchesToAdd; i++)
                {
                    procTimes.Add(allProcTimes[i]);
                }
            }
            
            //add processing times of all small batches to minProcTimeSeconds
            foreach (int time in procTimes)
            {
                minProcTimeSeconds += time;
            }

            //add extraBatches many entries to list of eligible machines;
            //for these entries, consider all machines to be eligible
            IList<int> allMach = machines.Keys.ToList();
            for (int i = 0; i < extraBatches; i++)
            {
                eligibleMachBatches.Add(allMach);
            }


            return (minbatchCount, minProcTimeSeconds, eligibleMachBatches);
        }

        /// <summary>
        /// Given a set of small jobs, a dictionary of machines together with the overall max machine capacity, 
        /// find the minimum number of batches required to schedule all jobs based on the minimal and maximal processing times of the jobs.
        /// For these batches, compute the minimal processing time as well.
        /// </summary>
        /// <param name="smallJobs"></param>
        /// <param name="machines"></param>
        /// <param name="maxCap"></param>
        /// <returns>(minimum number of batches required, minimal batch processing time)</returns>
        private static (int minbatchCount, int minProcTimeSeconds)
            GetBoundsSmallJobsCompProcTimes(IEnumerable<IJob> smallJobs, int maxCap)
        {
            int minbatchCount = 0;
            int minProcTimeSeconds = 0;
            List<int> procTimes = new List<int>();

            //create list of unit size-jobs that are characterized by their minimal and maximal processing times
            IList<(int minTime, int maxTime)> unitJobs = new List<(int, int)>();
            foreach (IJob job in smallJobs)
            {
                for (int i = 0; i < job.Size; i++)
                {
                    unitJobs.Add((job.MinTime, job.MaxTime));
                }
            }

            var sortedUnitJobs = unitJobs.OrderByDescending(j => j.minTime).ToList();

            while (sortedUnitJobs.Any())
            {
                int batchTime = sortedUnitJobs[0].minTime;
                int batchSize = 0;
                //a batch with this processing time is created
                procTimes.Add(batchTime);

                //go through list and add up to maxCap many jobs that are compatible with batchMinTime and batchMaxTime
                int j = 0;
                while (batchSize < maxCap && sortedUnitJobs.Any() && j < sortedUnitJobs.Count)
                {
                    if (sortedUnitJobs[j].minTime <= batchTime
                        && batchTime <= sortedUnitJobs[j].maxTime)
                    {
                        //remove job from list
                        sortedUnitJobs.RemoveAt(j);
                        batchSize += 1;
                    }
                    else
                    {
                        //increase index if no job has been removed
                        j += 1;
                    }

                }

                minbatchCount += 1;
            }

            //add processing times of all small batches to minProcTimeSeconds
            foreach (int time in procTimes)
            {
                minProcTimeSeconds += time;
            }

            return (minbatchCount, minProcTimeSeconds);
        }        

    }
}
