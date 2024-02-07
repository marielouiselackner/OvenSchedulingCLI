using OvenSchedulingAlgorithm.Algorithm.SimpleGreedy.Implementation;
using OvenSchedulingAlgorithm.Interface;
using OvenSchedulingAlgorithm.Interface.Implementation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Attribute = OvenSchedulingAlgorithm.Interface.Implementation.Attribute;

namespace OvenSchedulingAlgorithm.InstanceGenerator
{
    /// <summary>
    /// Generator that creates random oven scheduling instances given the specified parameters.
    /// </summary>
    public class RandomInstanceGenerator
    {       

        /// <summary>
        /// Create random instance
        /// </summary> 
        /// <returns>Randomly created instance with given parameters.</returns>
        public IInstance GenerateInstance(RandomInstanceParameters parameters)
        {
            int n = parameters.JobCount;
            int k = parameters.MachineCount;
            int a = parameters.AttributeCount;

            string instanceName = "RandomOvenSchedulingInstance-n" + n.ToString() 
                + "-k" + k.ToString() + "-a" + a.ToString() + "-";
            DateTime creaTime = DateTime.Now;
            string instanceFileName = instanceName + "-" + creaTime.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture) + ".json";
            string randomInstanceInfoFileName = "RandomlyGeneratedInstances-WithUnAssignedJobsGreedySolution" + ".out";

            //write values of all generation parameters to .json file 
            string fileName = "GenerationParameters-" + instanceName + "-" 
                + creaTime.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture) + ".json";
            parameters.Serialize(fileName);

            //actually only needed when solvableByGreedyOnly is true. In this case, we try finding an instance that is solvable by the greedy heuristic 
            //up to 100 times (normally, such a solution should be found much earlier)
            int runs = 0;
            while (runs < 100)
            {
                IDictionary<int, IMachine> machines = new Dictionary<int, IMachine>();
                IList<IJob> jobs = new List<IJob>();
                IDictionary<int, IAttribute> attributes = new Dictionary<int, IAttribute>();

                Random rand = new Random();

                //create lists of minimum and maximum processing times of jobs
                var processingTimes = GenerateMinMaxProcessingTimes
                    (parameters.DiffProcTimes, parameters.MaxProcTime, n, parameters.ChooseMaxProcTime, rand);
                IList<int> minProcessingTimes = processingTimes.minProcessingTimes;
                IList<int> maxProcessingTimes = processingTimes.maxProcessingTimes;
                //int maxProcessingTime = processingTimes.maxProcessingTime;
                int minProcessingTime = minProcessingTimes.Min();

                //calculate total minimal processing time 
                int totalProcessingTime = minProcessingTimes.Sum();

                //initialise values for min and max of earliest start
                int minEarliestStartMinutes = totalProcessingTime;
                int maxEarliestStartMinutes = 0;
                //create list of earliest start times (in minutes) of jobs and calculate minimum earliest start time
                IList<int> earliestStartTimes = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    int time = rand.Next(0, (int)Math.Ceiling(parameters.Rho * totalProcessingTime) + 1); //as in PhD thesis Velez Gallego
                    earliestStartTimes.Add(time);
                    minEarliestStartMinutes = Math.Min(time, minEarliestStartMinutes);
                    maxEarliestStartMinutes = Math.Max(time, maxEarliestStartMinutes);
                }

                DateTime schedulingHorizonStart = new DateTime(2020, 01, 01);

                //generate arrays of setup times and costs
                int[,] setupCostsArray = GenerateSetupTimesOrCosts(a, parameters.SetupCostType, parameters.MaxProcTime, rand);
                int[,] setupTimesArray = GenerateSetupTimesOrCosts(a, parameters.SetupTimeType, parameters.MaxProcTime, rand);

                //max setup time
                int maxSetupTime = setupTimesArray.Cast<int>().Max();

                //create jobs
                //max latest end
                DateTime maxLatestEnd = new DateTime();

                for (int i = 0; i < n; i++)
                {
                    int minTime = minProcessingTimes[i];
                    int maxTime = maxProcessingTimes[i];

                    DateTime earliestStart = schedulingHorizonStart.AddMinutes(earliestStartTimes[i]);
                    int latestEndMinutes = earliestStartTimes[i]
                        + (int)Math.Floor(minTime * (rand.NextDouble() * (parameters.Phi - 1) + 1)); // as done by Malve  and  Uzsoy (2007)
                    DateTime latestEnd = schedulingHorizonStart.AddMinutes(latestEndMinutes);
                    if (latestEnd > maxLatestEnd)
                    {
                        maxLatestEnd = latestEnd;
                    }
                    int size = rand.Next(1, parameters.MaxJobSize + 1); //as in PhD thesis Velez Gallego
                    
                    //list of eligible machines
                    IList<int> eligibleMachines = new List<int>();
                    //pick one eligible machine at random (to guarantee that at least one machine is eligible)
                    int machineId = rand.Next(1, k + 1);
                    eligibleMachines.Add(machineId);
                    //for all other machines, the probability of being eligible is eligibility_proba
                    for (int j = 1; j < k + 1; j++)
                        if (j != machineId)
                        {
                            bool eligible = rand.NextDouble() >= (1 - parameters.EligibilityProba);
                            if (eligible) { eligibleMachines.Add(j); }
                        }

                    //create dictionary of attributes for every eligible machine
                    int attributeId = rand.Next(1, a + 1);
                    IDictionary<int, int> attributeIdPerMachine = new Dictionary<int, int>();
                    for (int j = 0; j < eligibleMachines.Count; j++)
                    {
                        if (parameters.DifferentAttributesPerMachine)
                        {
                            //pick a new attribute at random for every eligible machine
                            attributeId = rand.Next(1, a + 1);
                        }
                        //if parameters.DifferentAttributesPerMachine == false, the job's attribute is the same on all machines
                        attributeIdPerMachine.Add(eligibleMachines[j], attributeId);
                    }


                    IJob job = new Job((i + 1), "Job " + (i + 1).ToString(), earliestStart, latestEnd, 
                        60 * minTime, 60 * maxTime, size, attributeIdPerMachine, eligibleMachines);

                    jobs.Add(job);

                }

                // determine schedulingHorizonEnd: calculate upper bound for the termination of the last job
                // explanation: if every job is scheduled in a batch of its own, the total runtime is at most equal to the sum of all processing times 
                // plus the number of jobs * the maximal setup time
                // since machines are available at least availability_percentage of the time, all jobs should be - on average - 
                // finished after 1/availability_percentage* the total runtime (after the latest earliest start)
                // Note: it might be that the latest end date for some job is greater than this upper bound. In this case, we choose the maximum latest
                // end date as the schedulingHorizonEnd
                int timeForProcessingAllJobsInMinutes =
                    (int)Math.Ceiling(1 / parameters.AvailabilityPercentage * (totalProcessingTime + n * maxSetupTime));
                DateTime schedulingHorizonEnd = schedulingHorizonStart.AddMinutes(maxEarliestStartMinutes)
                    .AddMinutes(timeForProcessingAllJobsInMinutes);
                if (maxLatestEnd > schedulingHorizonEnd)
                {
                    schedulingHorizonEnd = maxLatestEnd;
                }
                int schedulingHorizonLengthMinutes = (int)(schedulingHorizonEnd - schedulingHorizonStart).TotalMinutes;


                //create machines
                for (int i = 1; i < k + 1; i++)
                {

                    int minCap = 0; //minimum capacity set to 0 for the time being
                    int maxCap = rand.Next(parameters.MaxCapLowerBound,parameters.MaxCapUpperBound + 1);

                    //create shifts
                    var availabilityStartAndEnd = GenerateMachineShifts
                        (parameters.MinShiftCount, parameters.MaxShiftCount, minProcessingTime + maxSetupTime, 
                        parameters.AvailabilityPercentage, schedulingHorizonStart, schedulingHorizonLengthMinutes, rand);

                    IList<DateTime> availabilityStart = availabilityStartAndEnd.availabilityStart;
                    IList<DateTime> availabilityEnd = availabilityStartAndEnd.availabilityEnd;


                    IMachine machine = new Machine(i, "Machine " + i.ToString(), minCap, maxCap, availabilityStart, availabilityEnd);

                    machines.Add(i, machine);
                }


                //create attributes

                for (int i = 0; i < a; i++)
                {
                    IList<int> setupCosts = new List<int>();
                    IList<int> setupTimes = new List<int>();


                    for (int j = 0; j < a; j++)
                    {
                        setupCosts.Add(setupCostsArray[i, j]);
                        setupTimes.Add(60*setupTimesArray[i, j]); //setup times in seconds
                    }

                    IAttribute attribute = new Attribute(i + 1, "Attribute " + (i + 1).ToString(), setupCosts, setupTimes);

                    attributes.Add(i + 1, attribute);
                }

                IDictionary<int, int> initialStates = new Dictionary<int, int>();
                if (parameters.InitialStates)
                {
                    for (int i = 1; i < k + 1; i++)
                    {
                        int initialState = rand.Next(1, a + 1);
                        initialStates.Add(i, initialState);
                    }

                }

                IInstance instance = new Instance(instanceName, creaTime, machines, initialStates, 
                    jobs, attributes, schedulingHorizonStart, schedulingHorizonEnd);

                //run greedy algorithm to check whether instance is feasible 
                //(instance could be infeasible e.g because machine capacities are too small) 
                //note that greedy heuristic might not be able to assign all jobs eventhough instance is feasible
                int unassignedJobs = runGreedy(instance, parameters.SolvableByGreedyOnly);

                //write info to console if not all jobs could be assigned
                if (unassignedJobs > 0)
                {
                    Console.WriteLine("{0} NonAssignmentCountGreedySolution: {1}", instanceFileName, unassignedJobs);
                }

                //accept instance if:
                //greedy algorithm was capable of assigning all jobs 
                //or we are also interested in instances that cannot be solved by the greedy heuristic (might still be feasible)
                if (!parameters.SolvableByGreedyOnly || unassignedJobs == 0)                     
                {
                    //serialize instance
                    instance.Serialize(instanceFileName.Replace(':', '-'));

                    //add info to file: how many jobs were not assigned by greedy 
                    string greedyInfoFileName = "NonAssignmentCountGreedySolution-RandomlyGeneratedInstances" + ".out";
                    string greedyInfo = instanceFileName + ", Number of jobs that were not assigned by the greedy heuristic: " + unassignedJobs + "\n";
                    using (StreamWriter sw = File.AppendText(greedyInfoFileName))
                    {
                        sw.Write(greedyInfo);
                        sw.Close();
                    }

                    if (unassignedJobs > 0)
                    {
                        //add info to file: created instance for which greedy could not assign all jobs                        
                        using (StreamWriter sw = File.AppendText(randomInstanceInfoFileName))
                        {
                            sw.Write(greedyInfo);
                            sw.Close();
                        }
                    }               
                    
                    //write actual values of all random instance parameters to file 
                    RandomInstanceParameters actualParameters = RandomInstanceParameters.GetParametersInstance(instance, parameters);
                    fileName = "ActualParameters-" + instanceName + "-"
                        + creaTime.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture) + ".json";
                    actualParameters.Serialize(fileName);

                    return instance;
                }
                else
                {
                    runs++;
                    continue;
                }
            }

            //if no feasible instance was found after 100 runs, return empty instance
            IInstance emptyInstance = new Instance("Empty Instance", DateTime.Now, new Dictionary<int, IMachine>(),
                new Dictionary<int, int>(), new List<IJob>(),
                new Dictionary<int, IAttribute>(), DateTime.Now, DateTime.Now);
            
            //add info to file and to console that no instance could be found
            string randomInstanceInfo = emptyInstance.Name + "-" + emptyInstance.CreationDate.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture) 
                + ", Even after 100 trial runs, no random instance could be found that uses " +
                "given random instance parameters and for which the greedy heuristic can assign all jobs. \n";
            using (StreamWriter sw = File.AppendText(randomInstanceInfoFileName))
            {
                sw.Write(randomInstanceInfo);
                sw.Close();
            }
            Console.WriteLine(randomInstanceInfo);

            return emptyInstance;
        }

        /// <summary>
        /// Given an instance of the Oven Schedulign problem, create a sub-instance with given number of jobs and machines by randomly picking elements of the instance
        /// </summary>
        /// <param name="instance">the given instance</param>
        /// <param name="n"></param>
        /// <param name="k"></param>
        /// <param name="sameAttributeOnAllMachines">boolean indicating whether a job's attribute should be the same across all eligible machines</param>
        /// <param name="initialStates">boolean indicating whether machines should have initial states</param>
        /// <returns>Random sub-instance of the given instance</returns>
        public static IInstance CreateRandomSubInstance(IInstance instance, int n, int k, bool sameAttributeOnAllMachines, bool initialStates)
        {
            int bigN = instance.Jobs.Count;

            Random rand = new Random();

            IDictionary<int, IMachine> machines = PickMachineSubset(k, instance.Machines, rand);
            IList<IJob> jobs = new List<IJob>();
            List<int> attributeIds = new List<int>();            
            
            //list of jobs that can be scheduled on one of the selected machines
            var possibleJobs = instance.Jobs.Where(job => job.EligibleMachines.Intersect(machines.Keys).Any());
            int neededJobs = n;
            int remainingJobs = bigN;
            foreach (IJob job in possibleJobs)
            {
                int random = rand.Next(1, remainingJobs + 1);
                if (random <= neededJobs)
                {
                    //need to make new list of eligible machines (remove all eligible machines that are not in instance)
                    IList<int> newEligibleMachines = job.EligibleMachines.Intersect(machines.Keys).ToList();

                    //need to make new attribute dictionary (only include entries for machines which are part of the sub-instance)
                    IDictionary<int, int> newAttributePerMachine = new Dictionary<int, int>();
                    if (sameAttributeOnAllMachines)
                    {
                        //job's attribute on first machine
                        int attributeId = job.AttributeIdPerMachine[newEligibleMachines[0]];
                        foreach (int machineID in newEligibleMachines)
                        {
                            newAttributePerMachine.Add(machineID, attributeId);
                        }
                        //add attribute to list of actually used attribute Ids (if not yet present)
                        if (!attributeIds.Contains(attributeId)) 
                        {
                            attributeIds.Add(attributeId);
                        }
                        
                    }
                    else
                    {
                        foreach (var entry in job.AttributeIdPerMachine)
                        {
                            if (machines.Keys.Contains(entry.Key))
                            {
                                newAttributePerMachine.Add(entry);
                                //add attribute to list of actually used attribute Ids (if not yet present)
                                int attributeId = entry.Value;                                
                                if (!attributeIds.Contains(attributeId))
                                {
                                    attributeIds.Add(attributeId);
                                }
                            }
                        }
                    }
                                      
                    IJob newJob = new Job(job.Id, job.Name, job.EarliestStart, job.LatestEnd, job.MinTime, job.MaxTime, job.Size, newAttributePerMachine, newEligibleMachines);
                    jobs.Add(newJob);
                    neededJobs -= 1;
                }
                remainingJobs -= 1;

            }

            //create dictionary of attributes that actually appear in sub-instance
            IDictionary<int, IAttribute> attributes = CreateSubInstanceAttributes(instance.Attributes, attributeIds);

            //initial states of machines
            IDictionary<int, int> initialStatesDict = new Dictionary<int, int>();
            //if instance already has initial states, limit initialStates to selected machines
            if (initialStates && instance.InitialStates != null && instance.InitialStates.Count > 0)
            {
                foreach (var entry in instance.InitialStates)
                {
                    if (machines.ContainsKey(entry.Key))
                    {
                        initialStatesDict.Add(entry);
                    }
                }

            }
            //if initial states should be chosen for sub-instance, pick these at random
            else if (initialStates)
            {
                List<int> attributIDs = attributes.Keys.ToList();
                List<int> machineIDs = machines.Keys.ToList();
                for (int i = 0; i < machineIDs.Count; i++)
                {
                    int initialState = attributIDs[rand.Next(0, attributIDs.Count)];
                    int machineId = machineIDs[i];
                    initialStatesDict.Add(machineId, initialState);
                }
            }


            IInstance subInstance = new Instance(
                "subinstance of " + instance.Name,
                DateTime.Now,
                machines,
                initialStatesDict,
                jobs,
                attributes,
                instance.SchedulingHorizonStart,
                instance.SchedulingHorizonEnd
                );

            return subInstance;
        }

        /// <summary>
        /// Given a dictionary of attributes, construct the dictionary of attributes for those attributes appearing in a given list of attribute Ids
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="attributeIds"></param>
        /// <returns></returns>
        private static IDictionary<int, IAttribute> CreateSubInstanceAttributes
            (IDictionary<int, IAttribute> attributes, List<int> attributeIds)
        {
            IDictionary<int, IAttribute> newAttributes = new Dictionary<int, IAttribute>();

            attributeIds.Sort();
            for (int i = 0; i < attributeIds.Count; i++)
            {
                //old attribute with current ID
                IAttribute attribute = attributes[attributeIds[i]];
                IList<int> setupCostsAttribute = new List<int>();
                IList<int> setupTimesAttribute = new List<int>();
                for (int j = 0; j < attributeIds.Count; j++)
                {
                    //if attribute ids start with 1
                    int otherAttributeId = attributeIds[j] - 1;
                    //if attribute ids start with 0
                    if (attributes.ContainsKey(0))
                    {
                        otherAttributeId = attributeIds[j];
                    }                    
                    //TODO this only works if the original attributes have IDs 1, 2, ..., a
                    //for current Opcenter instances this doesn't matter because setup times and costs are 0 anyway
                    setupCostsAttribute.Add(attribute.SetupCostsAttribute[otherAttributeId]);
                    setupTimesAttribute.Add(attribute.SetupTimesAttribute[otherAttributeId]);
                }

                IAttribute newAttribute = new Attribute(attribute.Id,
                    "sub-attribute " + attribute.Name,
                    setupCostsAttribute,
                    setupTimesAttribute);
                newAttributes.Add(newAttribute.Id, newAttribute);
            }

            return newAttributes;
        }

        /// <summary>
        /// Given a dictionary of machines, pick a random subset of machines of given size
        /// </summary>
        /// <param name="k"></param>
        /// <param name="machines"></param>
        /// <param name="rand"></param>
        /// <returns></returns>
            private static IDictionary<int, IMachine> PickMachineSubset(int k, IDictionary<int, IMachine> machines, Random rand)
        {
            IDictionary<int, IMachine> newMachines = new Dictionary<int, IMachine>();
            int bigK = machines.Count;

            //explanation of what we are doing here
            //Iterate through and for each element make the probability of selection = (number needed)/ (number left)
            //So if you had 40 items, the first would have a 5 / 40 chance of being selected. If it is, the next has a 4 / 39 chance, 
            //otherwise it has a 5 / 39 chance. By the time you get to the end you will have your 5 items.
            //This technique is called selection sampling, a special case of Reservoir Sampling.
            int neededMachines = k;
            int remainingMachines = bigK;
            foreach (IMachine machine in machines.Values)
            {
                int random = rand.Next(1, remainingMachines + 1);
                if (random <= neededMachines)
                {
                    newMachines.Add(machine.Id, machine);
                    neededMachines -= 1;
                }
                if (neededMachines == 0)
                {
                    break;
                }
                remainingMachines -= 1;
            }

            return newMachines;
        }

        /// <summary>
        /// Given an instance of the Oven Schedulign problem, create a sub-instance with given number of jobs and machines 
        /// by picking elements of the instance so that the earliest start dates of jobs form an interval in the original instance
        /// </summary>
        /// <param name="instance">the given instance</param>
        /// <param name="n"></param>
        /// <param name="k"></param>
        /// <param name="sameAttributeOnAllMachines">boolean indicating whether a job's attribute should be the same across all eligible machines</param>
        /// <param name="initialStates"></param>
        /// <returns>Random sub-instance of the given instance</returns>
        public static IInstance CreateIntervalSubInstance(IInstance instance, int n, int k, bool sameAttributeOnAllMachines, 
            bool initialStates)
        {
            int bigN = instance.Jobs.Count;

            Random rand = new Random();

            IDictionary<int, IMachine> machineSubset = PickMachineSubset(k, instance.Machines, rand);
            IList<IJob> jobs = new List<IJob>();
            List<int> attributeIds = new List<int>();
            DateTime schedStart = new DateTime();

            //list of jobs that can be scheduled on one of the selected machines
            var possibleJobs = instance.Jobs.Where(job => job.EligibleMachines.Intersect(machineSubset.Keys).Any())
                .OrderBy(job => job.EarliestStart).ToList();
            //TODO what if possibleJobsCount is less than neededJobs?
            int possibleJobsCount = possibleJobs.Count;
            int firstJobPos = rand.Next(0, possibleJobsCount - n);

            for (int i = firstJobPos; i < firstJobPos + n; i++)
            {               

                IJob job = possibleJobs[i];

                if (i == firstJobPos)
                {
                    schedStart = job.EarliestStart;
                }

                //need to make new list of eligible machines(remove all eligible machines that are not in instance)
                IList<int> newEligibleMachines = job.EligibleMachines.Intersect(machineSubset.Keys).ToList();

                //need to make new attribute dictionary (only include entries for machines which are part of the sub-instance)
                IDictionary<int, int> newAttributePerMachine = new Dictionary<int, int>();
                if (sameAttributeOnAllMachines)
                {
                    //job's attribute on first machine
                    int attributeId = job.AttributeIdPerMachine[newEligibleMachines[0]];
                    foreach (int machineID in newEligibleMachines)
                    {
                        newAttributePerMachine.Add(machineID, attributeId);
                    }
                    //add attribute to list of actually used attribute Ids (if not yet present)
                    if (!attributeIds.Contains(attributeId))
                    {
                        attributeIds.Add(attributeId);
                    }

                }
                else
                {
                    foreach (var entry in job.AttributeIdPerMachine)
                    {
                        if (machineSubset.Keys.Contains(entry.Key))
                        {
                            newAttributePerMachine.Add(entry);
                            //add attribute to list of actually used attribute Ids (if not yet present)
                            int attributeId = entry.Value;
                            if (!attributeIds.Contains(attributeId))
                            {
                                attributeIds.Add(attributeId);
                            }
                        }
                    }
                }

                IJob newJob = new Job(job.Id, job.Name, job.EarliestStart, job.LatestEnd, job.MinTime, job.MaxTime, job.Size, newAttributePerMachine, newEligibleMachines);
                jobs.Add(newJob);
            }

            //adapt machine availability times to new start of scheduling horizon
            IDictionary<int, IMachine> machines = new Dictionary<int, IMachine>();
            foreach (var machine in machineSubset)
            {
                IList<DateTime> newStartTimes = new List<DateTime>();
                IList<DateTime> newEndTimes = new List<DateTime>();

                for (int i = 0; i < machine.Value.AvailabilityStart.Count; i++)
                {
                    //adapt start times
                    if (machine.Value.AvailabilityStart[i] < schedStart)
                    {
                        newStartTimes.Add(schedStart);
                    }
                    else
                    {
                        newStartTimes.Add(machine.Value.AvailabilityStart[i]);
                    }
                    //adapt end times
                    if (machine.Value.AvailabilityEnd[i] < schedStart)
                    {
                        newEndTimes.Add(schedStart);
                    }
                    else
                    {
                        newEndTimes.Add(machine.Value.AvailabilityEnd[i]);
                    }
                }

                IMachine newMachine = new Machine(
                    machine.Value.Id,
                    machine.Value.Name,
                    machine.Value.MinCap,
                    machine.Value.MaxCap,
                    newStartTimes,
                    newEndTimes);

                machines.Add(newMachine.Id, newMachine);
            }

            //create dictionary of attributes that actually appear in sub-instance
            IDictionary<int, IAttribute> attributes = CreateSubInstanceAttributes(instance.Attributes, attributeIds);

            //initial states of machines
            IDictionary<int, int> initialStatesDict = new Dictionary<int, int>();
            //if instance already has initial states, limit initialStates to selected machines
            if (initialStates && instance.InitialStates != null && instance.InitialStates.Count > 0)
            {
                foreach (var entry in instance.InitialStates)
                {
                    if (machines.ContainsKey(entry.Key))
                    {
                        initialStatesDict.Add(entry);
                    }
                }

            }
            //if initial states should be chosen for sub-instance, pick these at random
            else if (initialStates)
            {
                List<int> attributIDs = attributes.Keys.ToList();
                List<int> machineIDs = machines.Keys.ToList();
                for (int i = 0; i < machineIDs.Count; i++)
                {
                    int initialState = attributIDs[rand.Next(0, attributIDs.Count)];
                    int machineId = machineIDs[i];
                    initialStatesDict.Add(machineId, initialState);
                }
            }

            IInstance subInstance = new Instance(
                "subinstance of " + instance.Name,
                DateTime.Now,
                machines,
                initialStatesDict,
                jobs,
                attributes,
                schedStart,
                instance.SchedulingHorizonEnd
                );

            return subInstance;
        }

        /// <summary>
        /// Randomly create lists of minimum and maximum processing times of jobs (in minutes)
        /// </summary>
        /// <param name="diff_times"></param>
        /// <param name="p"></param>
        /// <param name="n"></param>
        /// <param name="max_time"></param>
        /// <returns>List of minimum processing times, list of maximum processing times, overall maximum processing time</returns>
        private (IList<int> minProcessingTimes, IList<int> maxProcessingTimes, int maxProcessingTime) 
            GenerateMinMaxProcessingTimes(int diff_times, int p, int n, bool max_time, Random rand)
        {
            //create list of minimal and maximal processing times of jobs             
            IList<int> minProcessingTimes = new List<int>();
            IList<int> maxProcessingTimes = new List<int>();
            int maxProcessingTime = p;

            // if diff_times >= p, we sample min and max times at random in (1,p)
            // (if p is less than diff_times, we cannot create diff_times many different values in [1,p])
            // otherwise, we create a list of diff_times many different processing times
            if (diff_times >= p)
            {
                for (int i = 0; i < n; i++)
                {
                    //minimal processing time of job i
                    int minTime = rand.Next(1, p+1);
                    minProcessingTimes.Add(minTime);

                    //maximal processing time of job i
                    if (max_time)
                    {
                        int maxTime = rand.Next(minTime, p+1);
                        maxProcessingTimes.Add(maxTime);
                    }
                    else
                    {
                        maxProcessingTimes.Add(p);
                    }
                }
                maxProcessingTime = maxProcessingTimes.Max();
            }
            else
            {
                //create list of diff_times many different processing times among which job processing times are chosen
                List<int> diffProcessingTimes = new List<int>();
                int time = rand.Next(1, p + 1);
                for (int i = 0; i < diff_times; i++)
                {
                    while (diffProcessingTimes.Contains(time))
                    {
                        time = rand.Next(1, p + 1); //as in PhD thesis Velez Gallego
                    }
                    diffProcessingTimes.Add(time);
                }
                diffProcessingTimes.Sort();
                //maximal possible processing time
                maxProcessingTime = diffProcessingTimes[diff_times - 1];


                for (int i = 0; i < n; i++)
                {
                    //minimal processing time of job i
                    int pos_min = rand.Next(diff_times);
                    minProcessingTimes.Add(diffProcessingTimes[pos_min]);

                    //maximal processing time of job i
                    if (max_time)
                    {
                        int pos_max = rand.Next(pos_min, diff_times);
                        maxProcessingTimes.Add(diffProcessingTimes[pos_max]);
                    }
                    else
                    {
                        maxProcessingTimes.Add(maxProcessingTime);
                    }
                }
            }           
            return (minProcessingTimes, maxProcessingTimes, maxProcessingTime);
        }

        /// <summary>
        /// Randomly create lists of DateTimes availabilityStart and availabilityEnd for the start and end of on-shifts of a machine in an oven scheduling instance
        /// </summary>
        /// <param name="min_shift"></param>
        /// <param name="max_shift"></param>
        /// <param name="maxBatchPlusSetupTime"></param>
        /// <param name="maxProcessingTime"></param>
        /// <param name="availability_percentage"></param>
        /// <param name="schedulingHorizonStart"></param>
        /// <param name="schedulingHorizonLengthMinutes"></param>
        /// <param name="rand"></param>
        /// <returns>Lists of DateTimes availabilityStart and availabilityEnd</returns>
        private static (IList<DateTime> availabilityStart, IList<DateTime> availabilityEnd) 
            GenerateMachineShifts(int min_shift, int max_shift, int minBatchPlusMaxSetupTime, 
            double availability_percentage, DateTime schedulingHorizonStart, int schedulingHorizonLengthMinutes, Random rand)
        {
            // explanation:
            // every shift consists of an on-shift followed by an off-shift
            // first decide randomly how many shifts
            // then generate corresponding number of start/endpoints between schedulingHorizonStart and schedulingHorizonEnd
            // sort these in increasing order and add to list availabilityStart or availabilityEnd
                       
            IList<DateTime> availabilityStart = new List<DateTime>();
            IList<DateTime> availabilityEnd = new List<DateTime>();

            //minimum length of a shift: 
            //on-shift must be at least (minProcessingTime + maxSetupTime) minutes long (and on-shift is at least availability_percentage* the length of the entire shift)
            int minShiftLength = minBatchPlusMaxSetupTime;            

            //number of shifts
            //may not be larger than the length of the scheduling horizon divided by the minimum shift length
            int shiftCount = Math.Min(rand.Next(min_shift, max_shift + 1), schedulingHorizonLengthMinutes / minShiftLength);

            //find start time of first shift
            //1) In order to guarantee that every machine is available at least a fraction $\tau$ of the time, 
            //the start time of the first interval needs to be between 0 and $\lfloor l \cdot (1-\tau) \rfloor$
            //2) In order to leave enough time for all shiftCount-many shifts, 
            //the start time of the first interval needs to be between 0 and $ l - shiftCount*minShiftLength $
            int maxStartFirstShift = Math.Min(
                (int)Math.Floor(schedulingHorizonLengthMinutes * (1 - availability_percentage)),
                schedulingHorizonLengthMinutes - shiftCount * minShiftLength
                );
            int startFirstShift = rand.Next(maxStartFirstShift + 1);
            
            //for remaining shifts, find shiftCount-1 many random numbers between startFirstShift + minShiftLength
            //and schedulingHorizonLengthMinutes - minShiftLength that are at least minShiftLength apart
            //first, find shiftCount-1 many random numbers between K and L below and then shift them
            int K = startFirstShift + minShiftLength;
            int L = schedulingHorizonLengthMinutes - (shiftCount -1) * minShiftLength;
            List<int> randomNumbers = new List<int>();
            for (int j = 0; j < shiftCount - 1; j++)
            {
                randomNumbers.Add(rand.Next(K, L + 1));
            }
            randomNumbers.Sort();
            //shifted random numbers are start dates of shifts 
            //(can also be seen as the end of the previous off-shift)
            for (int j = 0; j < shiftCount - 1; j++)
            {
                randomNumbers[j] += j * minShiftLength;
            }
            //last shift ends at the end of the scheduling horizon
            randomNumbers.Add(schedulingHorizonLengthMinutes);
            //find starting and end point of on-shifts
            int start = startFirstShift;
            for (int j = 0; j < shiftCount; j++)
            {
                //add start to availabilityStart times
                availabilityStart.Add(schedulingHorizonStart.AddMinutes(start));

                //calculate end time of on-shift in minutes
                //end time is at start time + minShiftLength
                // and at least at availability_percentage of end time of entire shift, which is randomNumbers[j]
                double percent = 1 - rand.NextDouble() * (1 - availability_percentage);
                int end = start + Math.Max(
                    minShiftLength,
                    (int)Math.Ceiling((randomNumbers[j] - start) * percent)
                    );                
                availabilityEnd.Add(schedulingHorizonStart.AddMinutes(end));

                //get start value for next interval
                start = randomNumbers[j];             
            }

            return (availabilityStart, availabilityEnd);
        }


        /// <summary>
        /// Create matrix of setup times or setup costs for random insatnce
        /// </summary>
        /// <param name="a">number of attributes</param>
        /// <param name="setupType">which type of setup times/costs should be used</param>
        /// <param name="p">maximum processing time of jobs (setup times should be between 0 and ceil(p/4) </param>
        /// <param name="rand">random element</param>
        /// <returns>Matrix of setup times or costs.</returns>
        private int[,] GenerateSetupTimesOrCosts(int a, SetupType setupType, int p, Random rand)
        {
            int[,] setupTimes = new int[a, a];
            int max = (p + 3)/ 4; //rounds p/4 up to next integer
            int max_half = (p + 7) / 8; ; //rounds p/8 up to next integer
            int constant = rand.Next(0, max+1);

            for (int i = 0; i < a; i++)
            {
                for (int j = 0; j < a; j++)
                {
                    switch (setupType)
                    {
                        case SetupType.none:
                            setupTimes[i, j] = 0;
                            break;
                        case SetupType.constant:
                            setupTimes[i, j] = constant;
                            break;
                        case SetupType.arbitrary:
                            setupTimes[i, j] = rand.Next(max+1);
                            break;
                        case SetupType.realistic:
                            if (i == j)
                            {
                                setupTimes[i, j] = rand.Next(max_half +1);
                            }
                            else
                            {
                                setupTimes[i, j] = rand.Next(max_half + 1, max + 1);
                            }
                            break;
                        case SetupType.symmetric:
                            if (i<=j)
                            {
                                setupTimes[i, j] = rand.Next(max+1);
                            }
                            else
                            {
                                setupTimes[i, j] = setupTimes[j, i];
                            }
                            
                            break;
                    }
                }
            }

            return setupTimes;
        }

        /// <summary>
        /// Run Greedy algorithm to check whether all jobs can be assigned
        /// </summary>
        /// <param name="instance">instance to be solved</param>
        /// <param name="solvableByGreedyOnly">boolean indicating whether we are only interested in solutions were greedy can assign all jobs. 
        /// If false, greedy solution will always be serialized to file; if true, greedy solution is only serialized if all jobs were assigned. </param>
        /// <returns>Number of jobs that were not assigned</returns>
        private int runGreedy(IInstance instance, bool solvableByGreedyOnly)
        {
            int unassignedJobs = instance.Jobs.Count;
            
            SimpleGreedyAlgorithm algo = new SimpleGreedyAlgorithm();
            IAlgorithmConfig config = new AlgorithmConfig(0, false, "", new WeightObjective(1));

            IOutput solution = algo.Solve(instance, config);
            int assignedJobsGreedy = solution.BatchAssignments.Count;

            unassignedJobs -= assignedJobsGreedy;

            if (!solvableByGreedyOnly || unassignedJobs == 0 ) 
            {
                //serialize solution
                string solutionFileName = "greedySolution"+ instance.Name + "-" + 
                    instance.CreationDate.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture) + ".json";
                solution.Serialize(solutionFileName.Replace(':', '-'));
            }                 
            
            return unassignedJobs;
        }


    }
}
