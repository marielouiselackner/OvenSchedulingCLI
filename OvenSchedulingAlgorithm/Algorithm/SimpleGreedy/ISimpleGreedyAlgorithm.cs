using OvenSchedulingAlgorithm.Interface;
using System;
using System.Collections.Generic;

namespace OvenSchedulingAlgorithm.Algorithm.SimpleGreedy
{
    /// <summary>
    /// Simple Greedy Algorithm interface that can be used to run a simple greedy heuristic
    ///  that finds an Oven Schedule using the given jobs, machines and attributes information
    /// </summary>
    public interface ISimpleGreedyAlgorithm : IAlgorithm
    {
        /// <summary>
        /// Find a solution of the oven scheduling problem using a simple greedy algorithm
        /// </summary>
        /// <param name="instance">instance of the oven scheduling problem</param>
        IOutput RunSimpleGreedy(IInstance instance);

        /// <summary>
        /// Schedule a single job of a given OSP instance so that it finishes as early as possible on one of its eligible machines
        /// </summary>
        /// <param name="instance">oven scheduling instance</param>
        /// <param name="job">selected job that needs to be scheduled</param>
        /// <returns>Best assignment found for this job (in terms of tardiness)</returns>
        IBatchAssignment ScheduleSingleJobMinimizeTardiness(IInstance instance, IJob job);


    }
}
