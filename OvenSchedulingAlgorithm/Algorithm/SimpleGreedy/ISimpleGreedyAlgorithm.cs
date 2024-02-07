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
        /// For an instance consisting of a single job, find a solution of the oven scheduling problem using the simple greedy algorithm
        /// </summary>
        /// <param name="instance">instance of the oven scheduling problem</param>
        /// <returns>solution of the oven scheduling problem</returns>
        IOutput RunSimpleGreedySingleJob(IInstance instance);
    }
}
