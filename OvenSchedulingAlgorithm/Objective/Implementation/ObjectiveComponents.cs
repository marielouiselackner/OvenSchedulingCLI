using OvenSchedulingAlgorithm.Interface;
using System.Collections.Generic;

namespace OvenSchedulingAlgorithm.Objective.Implementation
{
    internal class ObjectiveComponents : IObjectiveComponents
    {
        /// <summary>
        /// Total runtime of ovens in seconds
        /// </summary>
        public double TotalRuntimeSeconds { get; }

        /// <summary>
        /// Weighted total runtime of ovens in minutes 
        /// (converted to minutes, normalized and then multiplied with weight of this cost component)
        /// </summary>
        public double WeightedTotalRuntime { get; }        

        /// <summary>
        /// Total setup times in seconds
        /// </summary>
        public int TotalSetupTimesSeconds { get; }

        /// <summary>
        /// Weighted Total setup times in seconds
        /// (normalized and then multiplied with weight of this cost component)
        /// </summary>
        public double WeightedTotalSetupTimes { get; }

        /// <summary>
        /// Total setup costs
        /// </summary>
        public int TotalSetupCosts { get; }

        /// <summary>
        /// Weighted Total setup costs
        /// (normalized and then multiplied with weight of this cost component)
        /// </summary>
        public double WeightedTotalSetupCosts { get; }

        /// <summary>
        /// Number of jobs that are finished after their latest end time 
        /// </summary>
        public int FinishedTooLate { get; }

        /// <summary>
        /// Weighted Number of jobs that are finished after their latest end time 
        /// (normalized and then multiplied with weight of this cost component)
        /// </summary>
        public double WeightedFinishedTooLate { get; }

        /// <summary>
        /// The number of unscheduled jobs (or jobs that are scheduled outside the scheduling horizon)
        /// </summary>
        public int NumberOfUnscheduledJobs { get; }

        /// <summary>
        /// The dictionary of unscheduled jobs (or jobs that are scheduled outside the scheduling horizon).
        /// </summary>
        public IDictionary<int, IJob> UnscheduledJobs { get; }

        /// <summary>
        /// The aggregated normalised objective value of the solution (between 0 and 1)
        /// </summary>
        public double ObjectiveValue { get; }        

        public ObjectiveComponents(double totalRuntimeSeconds, double weightedTotalRuntime,
            int totalSetupTimesSeconds, double weightedTotalSetupTimes, int totalSetupCosts, double weightedTotalSetupCosts,
            int finishedTooLate, double weightedFinishedTooLate,
            int numberOfUnscheduledJobs, IDictionary<int, IJob> unscheduledJobs, double objectiveValue)
        {
            TotalRuntimeSeconds = totalRuntimeSeconds;
            WeightedTotalRuntime = weightedTotalRuntime;
            TotalSetupTimesSeconds = totalSetupTimesSeconds;
            WeightedTotalSetupTimes = weightedTotalSetupTimes;
            TotalSetupCosts = totalSetupCosts;
            WeightedTotalSetupCosts = weightedTotalSetupCosts;
            FinishedTooLate = finishedTooLate;
            WeightedFinishedTooLate = weightedFinishedTooLate;
            NumberOfUnscheduledJobs = numberOfUnscheduledJobs;
            UnscheduledJobs = unscheduledJobs;
            ObjectiveValue = objectiveValue;
        }

    }
}
