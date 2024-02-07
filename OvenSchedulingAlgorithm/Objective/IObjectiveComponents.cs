using OvenSchedulingAlgorithm.Interface;
using System.Collections.Generic;

namespace OvenSchedulingAlgorithm.Objective
{
    public interface IObjectiveComponents
    {
        /// <summary>
        /// Total runtime of ovens in seconds
        /// </summary>
        double TotalRuntimeSeconds { get; }

        /// <summary>
        /// Weighted total runtime of ovens in minutes 
        /// (normalized and then multiplied with weight of this cost component)
        /// </summary>
        double WeightedTotalRuntime { get; }        

        /// <summary>
        /// Total setup times in seconds
        /// </summary>
        int TotalSetupTimesSeconds { get; }

        /// <summary>
        /// Weighted Total setup times in seconds
        /// (normalized and then multiplied with weight of this cost component)
        /// </summary>
        double WeightedTotalSetupTimes { get; }

        /// <summary>
        /// Total setup costs
        /// </summary>
        int TotalSetupCosts { get; }

        /// <summary>
        /// Weighted Total setup costs
        /// (normalized and then multiplied with weight of this cost component)
        /// </summary>
        double WeightedTotalSetupCosts { get; }

        /// <summary>
        /// Number of jobs that are finished after their latest end time 
        /// </summary>
        int FinishedTooLate { get; }

        /// <summary>
        /// Weighted Number of jobs that are finished after their latest end time 
        /// (normalized and then multiplied with weight of this cost component)
        /// </summary>
        double WeightedFinishedTooLate { get; }

        
        /// <summary>
        /// The number of unscheduled jobs (or jobs that are scheduled outside the scheduling horizon)
        /// </summary>
        int NumberOfUnscheduledJobs { get; }

        /// <summary>
        /// The dictionary of unscheduled jobs (or jobs that are scheduled outside the scheduling horizon).
        /// </summary>
        IDictionary<int, IJob> UnscheduledJobs { get; }

        /// <summary>
        /// The aggregated normalised objective value of the solution (between 0 and 1)
        /// </summary>
        double ObjectiveValue { get; }
    }
}
