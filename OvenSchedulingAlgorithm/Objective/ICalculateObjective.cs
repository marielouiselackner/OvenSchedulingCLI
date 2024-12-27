using OvenSchedulingAlgorithm.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace OvenSchedulingAlgorithm.Objective
{
    /// <summary>
    /// Calculate objective of a given solution and for given weights to an instance of the Oven Scheduling Problem.
    /// </summary>
    /// 
    public interface ICalculateObjective
    {

        /// <summary>
        /// From an instance and a solution to the oven scheduling problem as well as the weights used for optimization, 
        /// calculate the components of the objective and the real-valued objective value of the solution (value between 0 and 1)
        /// </summary>
        /// <returns>The IObjectiveValue object associated with the solution and weights</returns>
        IObjectiveComponents CalculateComponentsObjective();

        /// <summary>
        /// Find all jobs of the instance that have not been assigned 
        /// or that were assigned beyond the scheduling horizon in the current list of batch assignments
        /// </summary>
        /// <returns>The dictionary of unassigned jobs (keys are job ids)</returns>
        public IDictionary<int, IJob> FindUnassignedJobs();

        public IObjectiveComponents CalculateObjectiveFromComponents(double totalRuntimeMinutes, int totalSetupTimesMinutes,
            int totalSetupCosts, int finishedTooLate, double distanceEarliestStart, double distanceLatestEnd,
            IDictionary<int, IJob> unscheduledJobs);
    }
}
