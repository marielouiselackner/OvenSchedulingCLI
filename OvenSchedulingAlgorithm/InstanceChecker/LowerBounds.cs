using OvenSchedulingAlgorithm.Interface;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OvenSchedulingAlgorithm.InstanceChecker
{
    public class LowerBounds
    {
        /// <summary>
        /// Lower bound on the number of batches in any feasible solution
        /// </summary>
        public int LowerBoundBatchCount { get; }

        /// <summary>
        /// A lower bound (in seconds) on the cumulative runtime of all ovens  for any feasible solution
        /// (calculated via procedure for lower bounds)
        /// </summary>
        public int LowerBoundTotalRuntimeSeconds { get; }

        /// <summary>
        /// A lower bound on the cumulative runtime of all ovens  for any feasible solution
        /// (calculated via procedure for lower bounds)
        /// if unit of time is minutes (ie rounded up to next minute)
        /// </summary>
        public int LowerBoundTotalRuntimeMinutes { get; }

        /// <summary>
        /// A lower bound (in seconds) on the cumulative setup times
        /// (calculated via procedure for lower bounds)
        /// </summary>
        public int LowerBoundTotalSetupTimesSeconds { get; }

        /// <summary>
        /// A lower bound on the cumulative setup times
        /// (calculated via procedure for lower bounds)
        /// if unit of time is minutes (ie rounded up to next minute)
        /// </summary>
        public int LowerBoundTotalSetupTimesMinutes { get; }

        /// <summary>
        /// A lower bound on the cumulative setup costs
        /// (calculated via procedure for lower bounds)
        /// </summary>
        public int LowerBoundTotalSetupCosts { get; }

        /// <summary>
        /// A lower bound on the number of tardy jobs
        /// (calculated via procedure for lower bounds using a minimum cost flow problem)
        /// </summary>
        public int LowerBoundTardyJobs { get; }

        /// <summary>
        /// A lower bound on the number of tardy jobs
        /// (calculated via procedure for lower bounds scheduling every job individually)
        /// </summary>
        public int SimpleLowerBoundTardyJobs { get; }

        /// <summary>
        /// A lower bound on the aggregated integer-valued
        /// objective (value depends on the chosen weights)
        /// </summary>
        public int LowerBoundIntObjective { get; }

        /// <summary>
        /// A lower bound on the aggregated real-valued
        /// objective (value depends on the chosen weights)
        /// </summary>
        public double LowerBoundFloatObjective { get; }

        /// <summary>
        /// An upper bound on the average number of jobs per batch 
        /// (given by (number of jobs)/(min number of batches)
        /// </summary>
        public double UpperBoundAverageNumberOfJobsPerBatch { get;  }

        /// <summary>
        /// An upper bound on the runtime reduction compared with running every job in a singel batch
        /// (given by (sum of min time of jobs)/(lower bound total runtime)
        /// </summary>
        public double UpperBoundRuntimeReduction { get; }

        /// <summary>
        /// The time required to calculate all other properties of a "LowerBounds.cs" object
        /// </summary>
        public TimeSpan LowerBoundsCalculationTime { get; set; }

        /// <summary>
        /// The time required to calculate batch count/processing time bound 
        /// </summary>
        public TimeSpan BatchAndProcTimeLowerBoundsCalculationTime { get; set; }

        /// <summary>
        /// The time required to calculate tardiness lower bound based on minimum cost flow problem
        /// </summary>
        public TimeSpan TardinessLowerBoundsCalculationTime { get; set; }

        /// <summary>
        /// The time required to calculate setup cost lower bound based on TSP
        /// </summary>
        public TimeSpan SetupCostLowerBoundsCalculationTime { get; set; }

        [JsonConstructor]
        public LowerBounds(int lowerBoundBatchCount, int lowerBoundTotalRuntimeSeconds, int lowerBoundTotalRuntimeMinutes, 
            int lowerBoundTotalSetupTimesSeconds, int lowerBoundTotalSetupTimesMinutes, int lowerBoundTotalSetupCosts, 
            int lowerBoundTardyJobs, int simpleLowerBoundTardyJobs, int lowerBoundIntObjective, double lowerBoundFloatObjective, 
            double upperBoundAverageNumberOfJobsPerBatch, double upperBoundRuntimeReduction, 
            TimeSpan lowerBoundsCalculationTime, TimeSpan batchAndProcTimeLowerBoundsCalculationTime, TimeSpan tardinessLowerBoundsCalculationTime, TimeSpan setupCostLowerBoundsCalculationTime)
        {
            LowerBoundBatchCount = lowerBoundBatchCount;
            LowerBoundTotalRuntimeSeconds = lowerBoundTotalRuntimeSeconds;
            LowerBoundTotalRuntimeMinutes = lowerBoundTotalRuntimeMinutes;
            LowerBoundTotalSetupTimesSeconds = lowerBoundTotalSetupTimesSeconds;
            LowerBoundTotalSetupTimesMinutes = lowerBoundTotalSetupTimesMinutes;
            LowerBoundTotalSetupCosts = lowerBoundTotalSetupCosts;
            LowerBoundTardyJobs = lowerBoundTardyJobs;
            SimpleLowerBoundTardyJobs = simpleLowerBoundTardyJobs;
            LowerBoundIntObjective = lowerBoundIntObjective;
            LowerBoundFloatObjective = lowerBoundFloatObjective;
            UpperBoundAverageNumberOfJobsPerBatch = upperBoundAverageNumberOfJobsPerBatch;
            UpperBoundRuntimeReduction = upperBoundRuntimeReduction;
            LowerBoundsCalculationTime = lowerBoundsCalculationTime;
            BatchAndProcTimeLowerBoundsCalculationTime = batchAndProcTimeLowerBoundsCalculationTime;
            TardinessLowerBoundsCalculationTime = tardinessLowerBoundsCalculationTime;
            SetupCostLowerBoundsCalculationTime = setupCostLowerBoundsCalculationTime;
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

        /// <summary>
        /// Create a lower bounds object based on a serialized Object
        /// </summary>
        /// <param name="fileName">File location storing the serialized lower bounds.</param>
        public static LowerBounds Deserialize(string fileName)
        {
            // deserialize JSON directly from a file
            StreamReader streamReader = File.OpenText(fileName);

            JsonSerializer serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Objects };
            LowerBounds lowerBounds =
                (LowerBounds)serializer.Deserialize(streamReader, typeof(LowerBounds));
            streamReader.Close();

            return lowerBounds;
        }

    }
}
