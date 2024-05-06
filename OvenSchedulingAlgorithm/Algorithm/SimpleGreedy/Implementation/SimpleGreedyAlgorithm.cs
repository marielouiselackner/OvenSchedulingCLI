using OvenSchedulingAlgorithm.Converter;
using OvenSchedulingAlgorithm.Converter.Implementation;
using OvenSchedulingAlgorithm.Interface;
using OvenSchedulingAlgorithm.Interface.Implementation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OvenSchedulingAlgorithm.Algorithm.SimpleGreedy.Implementation
{
    /// <summary>
    /// Simple Greedy Algorithm interface that can be used to run a simple greedy heuristic
    ///  that finds an Oven Schedule using the given jobs, machines and attributes information
    /// </summary>
    public class SimpleGreedyAlgorithm: ISimpleGreedyAlgorithm
    {

        /// <summary>
        /// Trigger the simple greedy algorithm with the given instance
        /// </summary>
        /// <param name="instance">The given instance</param>
        /// <param name="algorithmConfig">Parameters of the algorithm (SerializeInputOutput and SerializeOutputDestination needed and mathematical model)</param>
        /// <returns>The output of the algorithm</returns>
        public IOutput Solve(IInstance instance, IAlgorithmConfig algorithmConfig)
        {
            DateTime startGreedy = DateTime.Now;

            //solution of the oven scheduling problem produced by the greedy heuristic
            IOutput solution = RunSimpleGreedy(instance);

            DateTime endGreedy = DateTime.Now;
            TimeSpan runtimeGreedy = endGreedy - startGreedy;          

            //serialize solution
            if (algorithmConfig.SerializeInputOutput)
            {
                //where to store the solution
                string solutionFileName = algorithmConfig.SerializeOutputDestination +
                    "ovenScheduling_GreedySolution"  + DateTime.Now.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture) 
                    + instance.Name + instance.CreationDate.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture) + ".json";

                solution.Serialize(solutionFileName.Replace(':', '-'));

                //write runtime info to file 
                string logFileName = "runtimeGreedyAlgorithm" + instance.Name + "-" + instance.CreationDate.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture) + ".txt";
                string runtime = "runtime greedy algorithm: " + runtimeGreedy.ToString("c") + " (hh:mm:ss.xxxxxxx);";

                File.WriteAllText(logFileName.Replace(':', '-'), runtime);
            }

            return solution;
        }



        /// <summary>
        /// Find a solution of the oven scheduling problem using a simple greedy algorithm
        /// </summary>
        /// <param name="instance">instance of the oven scheduling problem</param>
        /// <returns>solution of the oven scheduling problem</returns>
        public IOutput RunSimpleGreedy(IInstance instance)
        {
            IList<IBatchAssignment> batchAssignments = new List<IBatchAssignment>();

            int m = instance.Machines.Count;
            int maxMachineId = instance.Machines.Keys.Max();
            int n = instance.Jobs.Count;
            DateTime start = instance.SchedulingHorizonStart;
            int s = instance.Machines.Values.Max(x => x.AvailabilityStart.Count);
            int l = (int)instance.SchedulingHorizonEnd.Subtract(start).TotalMinutes;

            //calculate parameters for filling batches
            // timespan between first and last earliest start time
            DateTime minEarliestStart = instance.Jobs[0].EarliestStart;
            DateTime maxEarliestStart = instance.Jobs[0].EarliestStart;
            foreach (IJob job in instance.Jobs)
            {
                if (job.EarliestStart < minEarliestStart)
                {
                    minEarliestStart = job.EarliestStart;
                }
                if (job.EarliestStart > maxEarliestStart)
                {
                    maxEarliestStart = job.EarliestStart;
                }
            }
            int minJobSize = instance.Jobs.Select(j => j.Size).Min();
            //parameters needed for the function FillBatch 
            //(which jobs do we consider when trying to fill up a batch/how far do we look ahead?)
            TimeSpan earliestStartSpan = maxEarliestStart.Subtract(minEarliestStart);
            int maxTimeWindow = 1;
           
            //convert attribute IDs to 1..a
            IMiniZincConverter converter = new MiniZincConverter();
            Func<int, int> convertedAttributeId = converter.ConvertAttributeIdToMinizinc(instance.Attributes);

            //dictionary of current shift on machines, plus info whether machine is currently in on or off shift
            //keys are machine IDs
            IDictionary<int, (int shift, bool onshift)> currentShiftDict = new Dictionary<int, (int, bool)>(0);
            foreach (int machineId in instance.Machines.Keys)
            {
                int shift = GetCurrentShiftOnMachine(instance.Machines[machineId], start);
                bool onShift = true;
                if (shift == -1 || instance.Machines[machineId].AvailabilityEnd[shift] < start)
                {
                    onShift = false;
                }
                currentShiftDict.Add(machineId, (shift, onShift));
            }

            //dictionary of current last batch assignment that was scheduled on machine (key = machineId)
            IDictionary<int,IBatchAssignment> lastBatchAssignmentOnMachine = new Dictionary<int, IBatchAssignment>();
            int batchCount = 0;

            //List of jobs that have not yet been scheduled
            IList<IJob> unscheduledJobs = new List<IJob>(instance.Jobs);
            //dictionary of assigned machine for (splitted) jobs that have already been scheduled
            IDictionary<string, int> jobScheduledtoMachineDict = new Dictionary<string, int>();

            //earliest possible time for any job to be scheduled
            int earliestShiftStart = (int)instance.Machines.Values.Select(x => x.AvailabilityStart.Min()).Min().Subtract(start).TotalMinutes;
            //current time in minutes (calculated as of SchedulingHorizonStart)
            int time = earliestShiftStart - 1;

            IEnumerable<IJob> availableJobs = Enumerable.Empty<IJob>();

            while (unscheduledJobs.Count > 0 && time <= l)
            {
                //if no jobs found, increase time
                while (!availableJobs.Any())
                {
                    time++;
#if DEBUG
                    Console.WriteLine("time: {0}", time);
#else
#endif
                    //all jobs that are currently available
                    availableJobs = unscheduledJobs.Where(job => (job.EarliestStart <= start.AddMinutes(time)));
                    
                }

#if DEBUG
                IList<string> availableJobsIds = availableJobs.Select(j => j.Id.ToString()).ToList();
                //for comparability with Francesca's output: job ids start at 0, i.e are shifted by -1
                //IList<string> availableJobsIds = availableJobs.Select(j => (j.Id -1).ToString()).ToList();
                Console.WriteLine("available jobs: {0}", string.Join(", ", availableJobsIds));
#else
#endif

                //update current shift dict
                foreach (int machineId in instance.Machines.Keys)
                {
                    int oldShift = currentShiftDict[machineId].shift;
                    int newShift = oldShift;
                    int i = 1;
                    //check whether we are now in a shift after oldShift
                    while (instance.Machines[machineId].AvailabilityStart.Count > oldShift + i
                        && instance.Machines[machineId].AvailabilityStart[oldShift + i] <= start.AddMinutes(time))
                    {
                        newShift += 1;
                        i++;
                    }
                    bool onShift = true;
                    if (newShift == -1 || instance.Machines[machineId].AvailabilityEnd[newShift] < start)
                    {
                        onShift = false;
                    }
                    currentShiftDict[machineId] = (newShift, onShift);
                }

                //create list of unavailable machines (job currently processing or off-shift)
                IEnumerable<IMachine> unavailableMachines = instance.Machines.Values.Where(machine =>
                    (lastBatchAssignmentOnMachine.ContainsKey(machine.Id) //batch has been scheduled to this machine
                    &&
                    lastBatchAssignmentOnMachine[machine.Id].AssignedBatch.EndTime > start.AddMinutes(time)) //last batch is still processing
                    || currentShiftDict[machine.Id].onshift == false //machine is currently in off-shift
                    );

                IList<int> availableMachineIds = instance.Machines.Values
                    .Where(machine => !unavailableMachines.Contains(machine)).Select(machine => machine.Id).ToList();
                //available jobs need to have eligible machine that is currently available
                availableJobs = availableJobs.Where(j => j.EligibleMachines.Intersect(availableMachineIds).Any());

                if (unavailableMachines.Count() == m || !availableJobs.Any())
                {
                    availableJobs = Enumerable.Empty<IJob>();
#if DEBUG
                    Console.WriteLine("currently no machines available (that are eligible for available jobs), need to increase time", string.Join(", ", availableJobsIds));
#else
#endif
                }

                while (availableJobs.Any())
                {


                    //next job to be scheduled: sort first by Due Date, then by descending size 
                    IJob nextJob = availableJobs.OrderBy(job => job.LatestEnd).ThenByDescending(job => job.Size).FirstOrDefault();

#if DEBUG
                    string nextJobId = nextJob.Id.ToString();
                    //for comparability with Francesca's output: job ids start at 0, i.e are shifted by -1
                    //string nextJobId = (nextJob.Id - 1).ToString();
                    Console.WriteLine("selected job: {0}", nextJobId);
#else
#endif

                    IEnumerable<IMachine> availableMachines = instance.Machines.Values;

                    //among eligible machines for this job, find machines that are available 
                    //note that machine min_cap is ignored 
                        availableMachines = availableMachines.Where(machine =>
                        !unavailableMachines.Contains(machine) //machine is available
                        && nextJob.EligibleMachines.Contains(machine.Id) //machine is eligible for nextJob
                        && nextJob.Size <= machine.MaxCap //size of nextJob does not exceed machine capacity
                        );

                    

                    if (!availableMachines.Any())
                    {
#if DEBUG
                        Console.WriteLine("No machine available for selected job");
#else
#endif
                        availableJobs = availableJobs.Where(job => job.Id != nextJob.Id);
                        continue;
                    }

                    //calculate setup times from previous job for all available machines (in seconds)
                    IDictionary<int, int> setupTimesForMachine = GetSetupTimes(availableMachines, nextJob, convertedAttributeId,
                        instance.Attributes, lastBatchAssignmentOnMachine, instance.InitialStates);

                    //look for machine with minimal setup time on which job can be scheduled
                    IMachine bestMachine = FindBestMachine(setupTimesForMachine, start.AddMinutes(time), availableMachines, currentShiftDict, nextJob.MinTime);
                    
                    //if there is no machine that can process job (due to setup + processing times), remove job from list of available jobs and continue
                    if (bestMachine == null) //no machine could be found
                    {
#if DEBUG
                        Console.WriteLine("No machine found that can process selected job (due to setup + processing times)");
#else
#endif
                        availableJobs = availableJobs.Where(job => job.Id != nextJob.Id);
                        continue;
                    }

                    //schedule job to machine
                    unscheduledJobs.Remove(nextJob);
                    jobScheduledtoMachineDict[nextJob.Name]= bestMachine.Id;
                    IMachine assignedMachine = bestMachine;


                    //create batch assignment
                    //attribute of next job
                    instance.Attributes.TryGetValue(nextJob.AttributeIdPerMachine[assignedMachine.Id], out Interface.IAttribute attributeNextJob);
                    batchCount++;
                    int setupTime = setupTimesForMachine[bestMachine.Id];
                    IBatch assignedBatch = new Batch(batchCount, assignedMachine, start.AddMinutes(time).AddSeconds(setupTime),
                        start.AddMinutes(time).AddSeconds(setupTime + nextJob.MinTime), attributeNextJob);
                    IBatchAssignment batchAssignment = new BatchAssignment(nextJob, assignedBatch);
                    batchAssignments.Add(batchAssignment);

#if DEBUG
                    string assignedMachineId = assignedMachine.Id.ToString();
                    //for comparability with Francesca's output: machine ids start at 0, i.e are shifted by -1
                    //string assignedMachineId = (assignedMachine.Id - 1).ToString();
                    Console.WriteLine("Schedule selected job on machine {0}, batch number {1}", assignedMachineId, batchCount);
#else
#endif

                    lastBatchAssignmentOnMachine[bestMachine.Id] = batchAssignment;

                    int batchSize = nextJob.Size;
         
                    //add all other jobs to batch that can fit in same batch (in the order of their latest end date)
                    FillBatch(batchAssignments, batchAssignment, currentShiftDict[assignedMachine.Id].shift, batchSize,
                        jobScheduledtoMachineDict, minJobSize,
                        unscheduledJobs, maxTimeWindow);

#if DEBUG
                    //print start and end time of the batch 
                    //in datetime format and in minutes since start of the scheduling horizon
                    DateTime startTime = batchAssignment.AssignedBatch.StartTime;
                    Console.WriteLine("Batch starts at time {0} (={1})",
                    startTime, startTime.Subtract(instance.SchedulingHorizonStart).TotalMinutes
                    );

                    DateTime endTime = batchAssignment.AssignedBatch.EndTime;
                    Console.WriteLine("Batch ends at time {0} (={1})",
                    endTime, endTime.Subtract(instance.SchedulingHorizonStart).TotalMinutes
                    );
#else
#endif
                }
            }

            IList<SolutionType> solutionTypes = new List<SolutionType>();
            solutionTypes.Add(SolutionType.UnvalidatedSolution);


            DateTime creaTime = DateTime.Now;
            string outputName = "Oven Scheduling simple greedy solution for instance " + instance.Name + 
                instance.CreationDate.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture);
            IOutput output = new Output(outputName, creaTime, batchAssignments, solutionTypes);

            return output;

        }

        /// <summary>
        /// For an instance consisting of a single job, find a solution of the oven scheduling problem using the simple greedy algorithm
        /// </summary>
        /// <param name="instance">instance of the oven scheduling problem</param>
        /// <returns>solution of the oven scheduling problem</returns>
        public IOutput RunSimpleGreedySingleJob(IInstance instance)
        {
            IList<IBatchAssignment> batchAssignments = new List<IBatchAssignment>();

            DateTime start = instance.SchedulingHorizonStart;
            int l = (int)instance.SchedulingHorizonEnd.Subtract(start).TotalMinutes;

            IJob job = instance.Jobs[0];

            //dictionary of current shift on machines, plus info whether machine is currently in on or off shift
            //keys are machine IDs
            IDictionary<int, (int shift, bool onshift)> currentShiftDict = new Dictionary<int, (int, bool)>(0);
            foreach (int machineId in instance.Machines.Keys)
            {
                int shift = GetCurrentShiftOnMachine(instance.Machines[machineId], start);
                bool onShift = true;
                if (shift == -1 || instance.Machines[machineId].AvailabilityEnd[shift] < start)
                {
                    onShift = false;
                }
                currentShiftDict.Add(machineId, (shift, onShift));
            }

            //current time in minutes (calculated as of SchedulingHorizonStart)
            int time = -1;

            IEnumerable<IMachine> availableMachinesForJob = Enumerable.Empty<IMachine>(); ;

            while (time <= l)
            {
                time++;

                //update current shift dict
                foreach (int machineId in instance.Machines.Keys)
                {
                    int oldShift = currentShiftDict[machineId].shift;
                    int newShift = oldShift;
                    int i = 1;
                    //check whether we are now in a shift after oldShift
                    while (instance.Machines[machineId].AvailabilityStart.Count > oldShift + i
                        && instance.Machines[machineId].AvailabilityStart[oldShift + i] <= start.AddMinutes(time))
                    {
                        newShift += 1;
                        i++;
                    }
                    bool onShift = true;
                    if (newShift == -1 || instance.Machines[machineId].AvailabilityEnd[newShift] < start)
                    {
                        onShift = false;
                    }
                    currentShiftDict[machineId] = (newShift, onShift);
                }

                //create list of available machines (eligible for job and in on-shift)
                availableMachinesForJob = instance.Machines.Values.Where(machine =>
                    job.EligibleMachines.Contains(machine.Id)
                    && currentShiftDict[machine.Id].onshift == true
                    && job.Size <= machine.MaxCap //size of job does not exceed machine capacity
                    );

                ////if no machines available, increase time
                if (!availableMachinesForJob.Any())
                {                   
                    continue;
                }

                IMachine assignedMachine = availableMachinesForJob.First();
                IBatch assignedBatch = new Batch(1, assignedMachine, start.AddMinutes(time),
                        start.AddMinutes(time).AddSeconds(job.MinTime), instance.Attributes[job.AttributeIdPerMachine[assignedMachine.Id]]);
                IBatchAssignment batchAssignment = new BatchAssignment(job, assignedBatch);
                batchAssignments.Add(batchAssignment);

                break;
            }

            IList<SolutionType> solutionTypes = new List<SolutionType>
            {
                SolutionType.UnvalidatedSolution
            };


            DateTime creaTime = DateTime.Now;
            string outputName = "Oven Scheduling simple greedy solution for instance " + instance.Name +
                instance.CreationDate.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture);
            IOutput output = new Output(outputName, creaTime, batchAssignments, solutionTypes);

            return output;

        }

        /// <summary>
        /// Given current time and a machine, find in which shift of the machine one currently is
        /// </summary>
        /// <param name="machine">Machine for which shift should be determined</param>
        /// <param name="time">current time</param>
        /// <returns>Index of current shift on machine</returns>
        private int GetCurrentShiftOnMachine(IMachine machine, DateTime time)
        {
            // this is the maximal shift for which the current time is >= the start time of the shift
            int i = machine.AvailabilityStart.Select((v, i) => new { v, i })
                .Where(x => x.v <= time)
                .Select(x => x.i)
                .DefaultIfEmpty(-1)
                .Max();

            return i;
        }

        /// <summary>
        ///  For a collection of machines and a job to be scheduled, find the setup costs that the job will incur on every one of the machines
        /// </summary>
        /// <param name="machines">Collection of machines</param>
        /// <param name="job">Job to be scheduled</param>
        /// <param name="convertedAttributeId">function that gets the converted attribute Ids of jobs (converted to integers 1...number of attributes)</param>
        /// <param name="attributes">Dictionary of attributes</param>
        /// <param name="lastBatchAssignmentOnMachine">Dictionary of last batch assignments (key = machineID)</param>
        /// <param name="initStates">optional dictionary of initial states of machines</param>
        /// <returns>Dictionary of setup times incurred (key = machineId)</returns>
        private IDictionary<int, int>  GetSetupTimes(IEnumerable<IMachine> machines, IJob job, Func<int, int> convertedAttributeId, 
            IDictionary<int,IAttribute> attributes, IDictionary<int, IBatchAssignment> lastBatchAssignmentOnMachine,
            IDictionary<int, int>? initStates)
        {
            IDictionary<int, int> setupTimesForMachine = new Dictionary<int, int>();
            foreach (IMachine machine in machines)
            {
                int setupTime = 0;
                //this is not first batch on machine
                if (lastBatchAssignmentOnMachine.ContainsKey(machine.Id))
                {
                    //attribute of previous job on machine
                    attributes.TryGetValue(lastBatchAssignmentOnMachine[machine.Id].Job.AttributeIdPerMachine[machine.Id], out Interface.IAttribute attributePreviousJob);
                    //setup time between previous job and currently chosen job
                    setupTime = attributePreviousJob.SetupTimesAttribute[convertedAttributeId(job.AttributeIdPerMachine[machine.Id]) - 1];
                }
                //this is first batch on machine and we have initial states
                else if (initStates != null)
                {
                    //initial state attribute of this machine
                    attributes.TryGetValue(initStates[machine.Id], out Interface.IAttribute initAttribute);
                    //initial setup before currently chosen job
                    setupTime = initAttribute.SetupTimesAttribute[convertedAttributeId(job.AttributeIdPerMachine[machine.Id]) - 1];
                }
            
                setupTimesForMachine.Add(machine.Id, setupTime);
            }

            return setupTimesForMachine;
        }

        /// <summary>
        /// Given a Dictionary of setup times for machines, find the machine with minimal setup time on which a job can be scheduled
        /// </summary>
        /// <param name="setupTimesForMachine">Dictionary of setup times incurred by scheduling the job to one of the machines (key = machineId)</param>
        /// <param name="time">the current time</param>
        /// <param name="machines">collection of machines</param>
        /// <param name="processingTime">processing time of the job to be scheduled</param>
        /// <returns></returns>
        private IMachine FindBestMachine(IDictionary<int, int> setupTimesForMachine, DateTime time, IEnumerable<IMachine> machines,
            IDictionary<int, (int shift, bool onshift)> currentShiftDict, int processingTime)
        {       
            IMachine bestMachine = null;
            int minSetupTime = 0;
            bool machineFound = false;
            while (!machineFound)
            {
                //if no machines are left, return null-machine
                if (!setupTimesForMachine.Any())
                {
                    return null;
                }

                //pick machine with minimal setup time
                minSetupTime = setupTimesForMachine.Values.Min();
                int machineId = setupTimesForMachine.First(x => x.Value == minSetupTime).Key;

                bestMachine = machines.Where(machine => machine.Id==machineId).FirstOrDefault();

                //current shift on machine
                int shift = currentShiftDict[bestMachine.Id].shift;
                    //GetCurrentShiftOnMachine(, time);

                //check whether setup time and processing can be done within current shift of machine 
                if (time.AddSeconds(minSetupTime + processingTime) > bestMachine.AvailabilityEnd[shift])
                {
                    //job cannot be finished within current shift
                    //we can try on a different machine
                    setupTimesForMachine.Remove(machineId);
                }
                else
                {
                    machineFound = true;                   
                }
            }
            return bestMachine;
        }

        /// <summary>
        /// Given a batch assignment, fill the batch with other matching jobs 
        /// (=same attribute, eligible for same machine, min time <= batch processing time <= max time)
        /// </summary>
        /// <param name="batchAssignments">List of previously made batch assignments</param>
        /// <param name="batchAssignment">Batch assignment for batch to which jobs can be added</param>
        /// <param name="availableJobs">Collection of currently available jobs</param>
        /// <param name="unscheduledJobs">List of jobs that are currently not scheduled yet</param>
        private void FillBatch(IList<IBatchAssignment> batchAssignments, IBatchAssignment batchAssignment, 
            int shift, int batchSize,
            IDictionary<string, int> jobScheduledtoMachineDict, int minJobSize,
            IList<IJob> unscheduledJobs, int maxTimeWindow)
        {
            IJob jobinBatch = batchAssignment.Job;
            IBatch batch = batchAssignment.AssignedBatch;

            //minimal and maximal processing times of jobs in batch (in seconds)
            int batchMinTime = jobinBatch.MinTime;
            int batchMaxTime = jobinBatch.MaxTime;

            IEnumerable<IJob> jobsAvailableForBatch = Enumerable.Empty<IJob>();

            //while time window is not yet exceeded, add jobs to batch
            int timeWindow = -1;           
            //if no job can be added without exceeding machine capacity, we can stop
            while (batchSize + minJobSize <= batch.AssignedMachine.MaxCap)
            {

                // first check if any compatible jobs are available at the start of the batch, 
                // then look at all other jobs
                //only consider compatible jobs that will not force jobinBatch to finish late
                //(unless it was already finishing late anyway)
                
                //if no jobs found, increase time window
                //note: this is only done once, since maxTimeWindow = 1 for these models
                while (!jobsAvailableForBatch.Any() && timeWindow < maxTimeWindow)
                {
                    timeWindow++;

                    //check if there are other jobs available that can be scheduled in the same batch
                    jobsAvailableForBatch = unscheduledJobs.Where(job =>
                    job.EarliestStart <= batch.StartTime.AddMinutes(timeWindow) //job is available at batch start time + look ahead window
                    && job.EligibleMachines.Contains(batch.AssignedMachine.Id) //assigned machine is eligible
                    && job.AttributeIdPerMachine[batch.AssignedMachine.Id] == jobinBatch.AttributeIdPerMachine[batch.AssignedMachine.Id] //matching attributes
                    && job.MaxTime >= batchMinTime //min batch processing time not too long
                    && job.MinTime <= batchMaxTime //max batch processing time not too short
                    && job.Size + batchSize <= batch.AssignedMachine.MaxCap//combined size does not exceed machine capacity
                    && job.EarliestStart.AddSeconds(batchMinTime) <= batch.AssignedMachine.AvailabilityEnd[shift]//batch can be scheduled in assigned machine in current shift
                    && job.EarliestStart.AddSeconds(job.MinTime) <= batch.AssignedMachine.AvailabilityEnd[shift]//job can be scheduled in assigned machine in current shift
                    && batch.StartTime.AddSeconds(batchMinTime) <= batch.AssignedMachine.AvailabilityEnd[shift]//batch can be scheduled in assigned machine in current shift
                    && batch.StartTime.AddSeconds(job.MinTime) <= batch.AssignedMachine.AvailabilityEnd[shift]//job can be scheduled in assigned machine in current shift
                    && (batch.EndTime > jobinBatch.LatestEnd//unless jobInBatch already finishes too late, 
                    //only consider jobs that will not force the batch to end late
                        || (job.EarliestStart.AddSeconds(job.MinTime) <= jobinBatch.LatestEnd
                            && job.EarliestStart.AddSeconds(batchMinTime) <= jobinBatch.LatestEnd
                            && batch.StartTime.AddSeconds(job.MinTime) <= jobinBatch.LatestEnd))
                    );
                }             

                if (!jobsAvailableForBatch.Any())
                {
                    return;
                }
                

                //pick job with earliest due date, then largest size
                IJob jobForBatch = jobsAvailableForBatch.OrderBy(job => job.LatestEnd).ThenByDescending(job => job.Size).FirstOrDefault();
                unscheduledJobs.Remove(jobForBatch);
                jobScheduledtoMachineDict[jobForBatch.Name] = batch.AssignedMachine.Id;
                batchSize += jobForBatch.Size;
                
                //update batch start time if necessary
                if (jobForBatch.EarliestStart > batch.StartTime)
                {
                    batch.StartTime = jobForBatch.EarliestStart;
                }               

                //update minimal and maximal processing times of jobs in batch  if necessary
                if (jobForBatch.MinTime > batchMinTime)
                {
                    batchMinTime = jobForBatch.MinTime;
                }
                if (jobForBatch.MaxTime < batchMaxTime)
                {
                    batchMaxTime = jobForBatch.MaxTime;
                }

                //update batch processing time 
                batch.EndTime = batch.StartTime.AddSeconds(batchMinTime);

                //add job to batch
                batchAssignment = new BatchAssignment(jobForBatch, batch);
                batchAssignments.Add(batchAssignment);

#if DEBUG
                string addedJobId = jobForBatch.Id.ToString();
                //for comparability with Francesca's output: job ids start at 0, i.e are shifted by -1
                //string addedJobId = (jobForBatch.Id - 1).ToString();
                Console.WriteLine("Job {0} added to same batch", addedJobId);
#else
#endif

            }

            return;
        }

    }
}
