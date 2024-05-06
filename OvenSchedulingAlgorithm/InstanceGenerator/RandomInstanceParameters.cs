using OvenSchedulingAlgorithm.Algorithm;
using OvenSchedulingAlgorithm.InstanceChecker;
using OvenSchedulingAlgorithm.Interface;
using OvenSchedulingAlgorithm.Interface.Implementation;
using OvenSchedulingAlgorithm.Objective;
using OvenSchedulingAlgorithm.Objective.Implementation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OvenSchedulingAlgorithm.InstanceGenerator
{
    public class RandomInstanceParameters
    {
        /// <summary>
        /// The number of jobs
        /// </summary>
        public int JobCount { get; }

        /// <summary>
        /// The number of machines
        /// </summary>
        public int MachineCount { get; }

        /// <summary>
        /// The number of attributes
        /// </summary>
        public int AttributeCount { get; }

        /// <summary>
        /// The overall maximum processing time (in minutes)
        /// </summary>
        public int MaxProcTime { get; }

        /// <summary>
        /// The number of different processing times among which to choose
        /// </summary>
        public int DiffProcTimes { get; }

        /// <summary>
        /// Whether a maximum processing time should be chosen for every job or not
        /// </summary>
        public bool ChooseMaxProcTime { get; }

        /// <summary>
        /// Maximum size of job
        /// </summary>
        public int MaxJobSize { get; }

        /// <summary>
        /// Lower bound for the maximum capacity of machines
        /// </summary>
        public int MaxCapLowerBound { get; }

        /// <summary>
        /// Upper bound for the maximum capacity of machines
        /// </summary>
        public int MaxCapUpperBound { get; }

        /// <summary>
        /// Minimum number of shifts per machine
        /// </summary>
        public int MinShiftCount { get; }

        /// <summary>
        /// Maximum number of shifts per machine
        /// </summary>
        public int MaxShiftCount { get; }

        /// <summary>
        /// Lower bound for the fraction of time that every machine should be available - between 0 and 1.
        /// </summary>
        public double AvailabilityPercentage { get; }

        /// <summary>
        /// Probability of an additional machine to be selected 
        /// as eligible machine for a job (one machine will always be selected) - between 0 and 1.
        /// </summary>
        public double EligibilityProba { get; }

        /// <summary>
        /// Factor for the creation of earliest start dates - between 0 and 1.
        /// If rho = 0, all jobs are available right at the beginning and as rho grows, the jobs are released over a longer interval.
        /// </summary>
        public double Rho { get; }

        /// <summary>
        /// Factor used for the creation of latest end dates - greater or equal to 1.
        /// If phi = 1, the latest end time is equal to the sum of the earliest start time and the minimum processing time.
        /// As phi grows, the latest end date can be further away from the earliest start date.
        /// </summary>
        public double Phi { get; }

        /// <summary>
        /// Which type of setup costs should be used
        /// </summary>
        public SetupType SetupCostType { get; }

        /// <summary>
        /// Which type of setup times should be used.
        /// </summary>
        public SetupType SetupTimeType { get; }

        /// <summary>
        /// Boolean indicating whether instances that can not be solved by greedy heuristic should be thrown away.
        /// If false, generated instance might be infeasible.
        /// Null if parameters are not used for generation of a new instance but calculated for existing instance. 
        /// </summary>
        public bool? SolvableByGreedyOnly { get; }

        /// <summary>
        /// Boolean indicating whether jobs may have different attributes on different eligible machines.
        /// If false, attributes of a job are the same on all eligible machines.
        /// </summary>
        public bool DifferentAttributesPerMachine { get; }

        /// <summary>
        /// Boolean indicating whether initial states should be chosen for every machine.
        /// </summary>
        public bool InitialStates { get; }

        public static RandomInstanceParameters GetParametersInstance(IInstance instance) 
        {
            InstanceData instanceData = Preprocessor.DoPreprocessing(instance, new WeightObjective(1));
 
            int n = instance.Jobs.Count;
            int k = instance.Machines.Count; 
            int a = instance.Attributes.Count;
            int maxProcTime = instance.Jobs.Select(x => x.MaxTime).Max() / 60 ;

            //calculate the overall number of dictint processing times (min and max processing times together)
            var minProcessingTimes = instance.Jobs.Select(x => x.MinTime);
            var maxProcessingTimes = instance.Jobs.Select(x => x.MaxTime);
            int diffProcTimes = minProcessingTimes.Union(maxProcessingTimes).Count();

            bool chooseMaxProcTime = instance.Jobs.Select(j => j.MaxTime).Distinct().Count() > 1;
            int maxJobSize = instance.Jobs.Select(x => x.Size).Max();
            int maxCapLowerBound = instance.Machines.Select(x => x.Value.MaxCap).Min();
            int maxCapUpperBound = instance.Machines.Select(x => x.Value.MaxCap).Max();
            int minShiftCount = instance.Machines.Select(x => x.Value.AvailabilityStart.Count).Min();
            int maxShiftCount = instance.Machines.Select(x => x.Value.AvailabilityStart.Count).Max();

            //calculate proportion of time a machine is available (averaged over all machines)
            double availabilityPercentage = 0;
            foreach (IMachine machine in instance.Machines.Values)
            {
                double totalAvailabilityTimeMachine = 0;
                for (int i = 0; i < machine.AvailabilityStart.Count; i++)
                {
                    totalAvailabilityTimeMachine +=
                        (machine.AvailabilityEnd[i] - machine.AvailabilityStart[i]).TotalSeconds;
                }
                availabilityPercentage += totalAvailabilityTimeMachine;
            }
            double lengthSchedulingHorizon = (instance.SchedulingHorizonEnd - instance.SchedulingHorizonStart).TotalSeconds;
            availabilityPercentage /= k * lengthSchedulingHorizon;

            // calculate an estimate for the probability of an additional machine to be chosen:
            // we have n samples (the jobs) of a variable following a binomial distribution with proba p and (k-1) trials,
            // where k is the number of machines
            // estimate of p = sum_jobs p(job)/n, where p(job) = (#eligible machines(job) -1)/(k-1)
            // estimate of p = sum_jobs (sum(#eligible machines(job)) - n)/(n*(k-1))            
            double eligibilityProba = 0; // if there is only one machine, eligibilityProba = 0
            if (k > 1)
            {
                int totalNumberOfEligibleMachines = instance.Jobs.Select(x => x.EligibleMachines.Count).Sum();
                eligibilityProba = (double)(totalNumberOfEligibleMachines - n) / (double)(n * (k - 1));
            }

            //calculate the spread of earliest start dates 
            DateTime maxEarliestStart = instanceData.MaximalEarliestStart;
            DateTime minEarliestStart = instanceData.MinEarliestStart;
            double spreadEarliestStartSeconds = maxEarliestStart.Subtract(minEarliestStart).TotalSeconds;
            int totalProcessingTime = instance.Jobs.Select(x => x.MinTime).Sum();
            double rho = spreadEarliestStartSeconds / totalProcessingTime;

            // calculate the average spread of latest end dates:
            // for every job, calculate by which multiplicative factor the latest end is away 
            // from the earliest possible latest end (=earliest start + minimal processign time) and take average of these values
            double phi = 0;
            double spreadFactorJob;
            for (int i = 0; i < n; i++)
            {
                IJob job = instance.Jobs[i];
                //calculate the spreadFactor for job
                spreadFactorJob = (job.LatestEnd - job.EarliestStart).TotalSeconds / job.MinTime;
                phi += spreadFactorJob;
            }
            phi *= (double)1 / n;

            //create matrix of setupTimes and setupCosts
            int[,] setupTimeMatrix = new int[a, a];
            int[,] symmetricSetupTimeMatrix = new int[a, a];
            int[,] setupCostMatrix = new int[a, a];
            int[,] symmetricCostMatrix = new int[a, a];
            List<int> setupTimesList = new List<int>();
            List<int> setupCostsList = new List<int>();
            List<int> nonDiagSetupTimesList = new List<int>();
            List<int> nonDiagSetupCostsList = new List<int>();
            var sortedAttributes = instance.Attributes.OrderBy(x => x.Key).ToList();
            for (int i = 0; i < a; i++)
            {
                for (int j = 0; j < a; j++)
                {
                    int time = sortedAttributes[i].Value.SetupTimesAttribute[j];
                    setupTimeMatrix[i, j] = time;
                    if (i <= j)
                    {
                        symmetricSetupTimeMatrix[i,j] = time;
                        symmetricSetupTimeMatrix[j, i] = time;
                    }
                    setupTimesList.Add(time);
                    if (i != j)
                    {
                        nonDiagSetupTimesList.Add(time);
                    }

                    int cost = sortedAttributes[i].Value.SetupCostsAttribute[j];
                    setupCostMatrix[i, j] = cost;
                    if (i <= j)
                    {
                        symmetricCostMatrix[i, j] = cost;
                        symmetricCostMatrix[j, i] = cost;
                    }
                    setupCostsList.Add(cost);
                    if (i != j)
                    {
                        nonDiagSetupCostsList.Add(cost);
                    }
                }                    
            }
            //determine setup time type
            SetupType setupTimeType = new SetupType();
            if (setupTimesList.All(x => x == setupTimesList[0]))
            {
                if (setupTimesList[0] == 0)
                {
                    setupTimeType = SetupType.none;
                }
                else
                {
                    setupTimeType = SetupType.constant;
                }
            }
            else if (symmetricSetupTimeMatrix == setupTimeMatrix)
            {
                setupTimeType = SetupType.symmetric;
            }
            //TODO allow instance parameters to contain multiple setup types, ie for the case where matrix is both symmetric and realistic
            else if (Enumerable.Range(0,a-1).Select(x => setupTimeMatrix[x,x]).Max() //largest diagonal element
                     <
                     nonDiagSetupTimesList.Min()) //smallest non-diagonal element
            {
                setupTimeType = SetupType.realistic;
            }
            else
            {
                setupTimeType = SetupType.arbitrary;
            }

            //determine setup time type
            SetupType setupCostType = new SetupType();            
            if (setupCostsList.All(x => x == setupCostsList[0]))
            {
                if (setupCostsList[0] == 0)
                {
                    setupCostType = SetupType.none;
                }
                else
                {
                    setupCostType = SetupType.constant;
                }
            }
            else if (symmetricCostMatrix == setupCostMatrix)
            {
                setupCostType = SetupType.symmetric;
            }
            //TODO allow instance parameters to contain multiple setup types, ie for the case where matrix is both symmetric and realistic
            else if (Enumerable.Range(0, a - 1).Select(x => setupCostMatrix[x, x]).Max() //largest diagonal element
                     <
                     nonDiagSetupCostsList.Min()) //smallest non-diagonal element
            {
                setupCostType = SetupType.realistic;
            }
            else
            {
                setupCostType = SetupType.arbitrary;
            }

            int numberOfJobsWithConstantAttribute = instance.Jobs.Where(j => j.AttributeIdPerMachine.Values.Distinct().Count() == 1).Count();
            bool differentAttributesPerMachine = numberOfJobsWithConstantAttribute == n;
            bool initialStates = instance.InitialStates != null ? true : false;

            RandomInstanceParameters actualParameters = new RandomInstanceParameters(
                n,
                k,
                a,
                maxProcTime,
                diffProcTimes,
                chooseMaxProcTime,
                maxJobSize,
                maxCapLowerBound,
                maxCapUpperBound,
                minShiftCount,
                maxShiftCount,
                availabilityPercentage,
                eligibilityProba,
                rho,
                phi,
                setupCostType,
                setupTimeType,
                null,
                differentAttributesPerMachine,
                initialStates
                );

            return actualParameters;
        }


        /// <summary>
        /// Construct parameters for a random insatnce of the oven scheduling problem 
        /// </summary>
        /// <param name="jobCount"></param>
        /// <param name="machineCount"></param>
        /// <param name="attributeCount"></param>
        /// <param name="maxProcTime"></param>
        /// <param name="diffProcTimes"></param>
        /// <param name="chooseMaxProcTime"></param>
        /// <param name="maxJobSize"></param>
        /// <param name="maxCapLowerBound"></param>
        /// <param name="maxCapUpperBound"></param>
        /// <param name="minShiftCount"></param>
        /// <param name="maxShiftCount"></param>
        /// <param name="availabilityPercentage"></param>
        /// <param name="eligibilityProba"></param>
        /// <param name="rho"></param>
        /// <param name="phi"></param>
        /// <param name="setupCostType"></param>
        /// <param name="setupTimeType"></param>
        /// <param name="solvableByGreedyOnly"></param>
        /// <param name="differentAttributesPerMachine"></param>
        /// <param name="initialStates"></param>
        [JsonConstructor]
        public RandomInstanceParameters(int jobCount, int machineCount, int attributeCount, int maxProcTime, int diffProcTimes, 
            bool chooseMaxProcTime, int maxJobSize, int maxCapLowerBound, int maxCapUpperBound, int minShiftCount, 
            int maxShiftCount, double availabilityPercentage, double eligibilityProba, double rho, double phi, 
            SetupType setupCostType, SetupType setupTimeType, bool? solvableByGreedyOnly, bool differentAttributesPerMachine,
            bool initialStates)
        {
            JobCount = jobCount;
            MachineCount = machineCount;
            AttributeCount = attributeCount;
            MaxProcTime = maxProcTime;
            DiffProcTimes = diffProcTimes;
            ChooseMaxProcTime = chooseMaxProcTime;
            MaxJobSize = maxJobSize;
            MaxCapLowerBound = maxCapLowerBound;
            MaxCapUpperBound = maxCapUpperBound;
            MinShiftCount = minShiftCount;
            MaxShiftCount = maxShiftCount;
            AvailabilityPercentage = availabilityPercentage;
            EligibilityProba = eligibilityProba;
            Rho = rho;
            Phi = phi;
            SetupCostType = setupCostType;
            SetupTimeType = setupTimeType;
            SolvableByGreedyOnly = solvableByGreedyOnly;
            DifferentAttributesPerMachine = differentAttributesPerMachine;
            InitialStates = initialStates;
        }

        /// <summary>
        /// Serialize the parameters to a json file
        /// </summary>
        /// <param name="fileName">Location of the serialized filed</param>
        public void Serialize(string fileName)
        {
            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Objects
            };

            StreamWriter sw = new StreamWriter(fileName);
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, this);
            }
        }

        /// <summary>
        /// Create random instance parameters based on a serialized Object
        /// </summary>
        /// <param name="fileName">File location storing the serialized instance.</param>
        public static RandomInstanceParameters DeserializeInstance(string fileName)
        {
            // deserialize JSON directly from a file
            StreamReader streamReader = File.OpenText(fileName);

            JsonSerializer serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Objects };
            RandomInstanceParameters parameters =
                (RandomInstanceParameters)serializer.Deserialize(streamReader, typeof(RandomInstanceParameters));
            streamReader.Close();

            return parameters;
        }

    }
}
