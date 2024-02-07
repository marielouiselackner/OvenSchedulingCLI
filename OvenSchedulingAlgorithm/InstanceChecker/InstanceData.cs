using OvenSchedulingAlgorithm.Interface;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OvenSchedulingAlgorithm.InstanceChecker
{
    public class InstanceData
    {
        /// <summary>
        /// The name of the instance
        /// </summary>
        public string InstanceName { get; }

        /// <summary>
        /// The time when the instance was created
        /// </summary>
        public DateTime InstanceCreationDate { get; }

        /// <summary>
        /// The total number of jobs in the instance
        /// </summary>
        public int NumberOfJobs { get; }

        /// <summary>
        /// The total number of machines in the instance
        /// </summary>
        public int NumberOfMachines { get; }

        /// <summary>
        /// The total number of attributes in the instance
        /// </summary>
        public int NumberOfAttributes { get; }

        /// <summary>
        /// The dictionary of setup times in seconds. 
        /// Entry dict(ID1, ID2) = time means setuptime between batch of attribute with ID1 and batch of attribute with ID2 is equal to time.
        /// </summary>
        public IDictionary<(int, int), int> SetupTimeDictionary { get; }

        /// <summary>
        /// The dictionary of setup costs. 
        /// Entry dict(ID1, ID2) = cost means setupcost between batch of attribute with ID1 and batch of attribute with ID2 is equal to cost.
        /// </summary>
        public IDictionary<(int, int), int> SetupCostDictionary { get; }


        /// <summary>
        /// The length of the scheduling horizon
        /// </summary>
        public TimeSpan LengthSchedulingHorizon { get; }

        /// <summary>
        /// An upper bound (in seconds) for the cumulative runtime of all ovens 
        /// (sum of all minimal processing times of jobs)
        /// </summary>
        public int UpperBoundTotalRuntimeSeconds { get; }

        /// <summary>
        /// An upper bound (in minutes) for the cumulative runtime of all ovens 
        /// if unit of time is minutes (ie all min processing times rounded up to next minute)
        /// </summary>
        public int UpperBoundTotalRuntimeMinutes { get; }
        
        /// <summary>
        /// The minimum of all minimal processing times (in seconds)  
        /// </summary>
        public int MinMinTime { get; }

        /// <summary>
        /// The minimum of all minimal processing times (in minutes)  
        /// </summary>
        public int MinMinTimeMinutes { get; }

        /// <summary>
        /// The maximum of all minimal processing times (in seconds)  
        /// </summary>
        public int MaxMinTime { get; }

        /// <summary>
        /// The maximum of all minimal processing times (in minutes)  
        /// </summary>
        public int MaxMinTimeMinutes { get; }

        /// <summary>
        /// The overall earliest earliest start time of a job 
        /// </summary>
        public DateTime MinEarliestStart { get; }

        /// <summary>
        /// The overall earliest latest end time of a job 
        /// </summary>
        public DateTime MinimalLatestEnd { get; }

        /// <summary>
        /// The overall latest earliest start time of a job 
        /// </summary>
        public DateTime MaximalEarliestStart { get; }

        /// <summary>
        /// The maximum of all setup costs  
        /// </summary>
        public int MaxSetupCost { get; }

        /// <summary>
        /// The maximum of all setup times (in seconds)  
        /// </summary>
        public int MaxSetupTime { get; }

        /// <summary>
        /// The maximum of all setup times (in minutes)  
        /// </summary>
        public int MaxSetupTimeMinutes { get; }

        /// <summary>
        /// The maximum number of availability intervals per machine
        /// </summary>
        public int MaxNumberOfAvailabilityIntervals { get; }

        /// <summary>
        /// Upper bound for the TotalQuadraticDistanceToEarliestStartDate of a job (times in minutes)
        /// </summary>
        public double UpperBoundTDSJob { get; }

        /// <summary>
        /// TimeSPan value of the constant eStar needed for the calculation of quadratic distance to latest end date (TDE)
        /// </summary>
        public TimeSpan ConstantEStarForTDE { get; }

        /// <summary>
        /// Upper bound for the TotalQuadraticDistanceToLatestEndDate  of a job (times in minutes)
        /// </summary>
        public double UpperBoundTDEJob { get; }


        /// <summary>
        /// Upper bound for the objective function when using an integer-valued objective
        /// </summary>
        public long UpperBoundForIntegerObjective { get; }

        /// <summary>
        /// Multiplicative factor for the cost component "Total Runtime" neede for the calculation of an integer-valued objective
        /// </summary>
        public long MultFactorTotalRuntime { get; }

        /// <summary>
        /// Multiplicative factor for the cost component "FinishedTooLate" neede for the calculation of an integer-valued objective
        /// </summary>
        public long MultFactorFinishedTooLate { get; }

        /// <summary>
        /// Multiplicative factor for the cost component "TotalSetupTimes" neede for the calculation of an integer-valued objective
        /// </summary>
        public long MultFactorTotalSetupTimes { get; }

        /// <summary>
        /// Multiplicative factor for the cost component "TotalSetupCosts" neede for the calculation of an integer-valued objective
        /// </summary>
        public long MultFactorTotalSetupCosts { get; }


        /// <summary>
        /// Boolean indicating whether the instance passed the validity check
        /// </summary>
        public bool PassedValidityCheck { get; }

        /// <summary>
        /// Boolean indicating whether the instance passed a basic satisfiability check
        /// </summary>
        public bool PassedSatisfiabilityCheck { get; }

        /// <summary>
        /// The number of jobs that will always finish late,
        /// even if they are processed immediatly on the first available machine. 
        /// This is a lower bound on the number of tardy jobs.
        /// </summary>
        public int LowerBoundTardyJobs { get; }

        [JsonConstructor]
        public InstanceData(string instanceName, DateTime instanceCreationDate, int numberOfJobs, int numberOfMachines, int numberOfAttributes,
            IDictionary<(int, int), int> setupTimeDictionary, IDictionary<(int, int), int> setupCostDictionary,
            TimeSpan lengthSchedulingHorizon, int upperBoundTotalRuntimeSeconds, int upperBoundTotalRuntimeMinutes,
            int minMinTime, int minMinTimeMinutes, 
            int maxMinTime, int maxMinTimeMinutes, DateTime minEarliestStart, DateTime minimalLatestEnd, DateTime maximalEarliestStart,
            int maxSetupCost, int maxSetupTime, int maxSetupTimeMinutes, int maxNumberOfAvailabilityIntervals,
            double upperBoundTDSJob, TimeSpan constantEStarForTDE,  double upperBoundTDEJob, long upperBoundForIntegerObjective, 
            long multFactorTotalRuntime, long multFactorFinishedTooLate, 
            long multFactorTotalSetupTimes, long multFactorTotalSetupCosts, 
            bool passedValidityCheck, bool passedSatisfiabilityCheck, int lowerBoundTardyJobs)
        {
            InstanceName = instanceName;
            InstanceCreationDate = instanceCreationDate;
            NumberOfJobs = numberOfJobs;
            NumberOfMachines = numberOfMachines;
            NumberOfAttributes = numberOfAttributes;
            SetupTimeDictionary = setupTimeDictionary;
            SetupCostDictionary = setupCostDictionary;
            LengthSchedulingHorizon = lengthSchedulingHorizon;
            UpperBoundTotalRuntimeSeconds = upperBoundTotalRuntimeSeconds;
            UpperBoundTotalRuntimeMinutes = upperBoundTotalRuntimeMinutes;
            MinMinTime = minMinTime;
            MinMinTimeMinutes = minMinTimeMinutes;
            MaxMinTime = maxMinTime;
            MaxMinTimeMinutes = maxMinTimeMinutes;
            MinEarliestStart = minEarliestStart;
            MinimalLatestEnd = minimalLatestEnd;
            MaximalEarliestStart = maximalEarliestStart;
            MaxSetupCost = maxSetupCost;
            MaxSetupTime = maxSetupTime;
            MaxSetupTimeMinutes = maxSetupTimeMinutes;
            MaxNumberOfAvailabilityIntervals = maxNumberOfAvailabilityIntervals;
            UpperBoundTDEJob = upperBoundTDEJob;
            ConstantEStarForTDE = constantEStarForTDE;
            UpperBoundTDSJob = upperBoundTDSJob;
            UpperBoundForIntegerObjective = upperBoundForIntegerObjective;
            MultFactorTotalRuntime = multFactorTotalRuntime;
            MultFactorFinishedTooLate = multFactorFinishedTooLate;
            MultFactorTotalSetupTimes = multFactorTotalSetupTimes;
            MultFactorTotalSetupCosts = multFactorTotalSetupCosts;
            PassedValidityCheck = passedValidityCheck;
            PassedSatisfiabilityCheck = passedSatisfiabilityCheck;
            LowerBoundTardyJobs = lowerBoundTardyJobs;
        }

        /// <summary>
        /// Serialize the instance to a json file
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

    }
}
