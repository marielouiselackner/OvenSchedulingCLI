using System;
using System.Collections.Generic;
using System.Text;

namespace OvenSchedulingAlgorithm.Interface
{   /// <summary>
    /// A job in an instance of the oven scheduling algorithm
    /// </summary>
    public interface IJob
    {
        /// <summary>
        /// The id of the job
        /// </summary>
        int Id { get; }

        /// <summary>
        /// The name of the job
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The earliest start time of the job
        /// </summary>
        DateTime EarliestStart { get; }

        /// <summary>
        /// The latest end time (=deadline) of the job
        /// </summary>
        DateTime LatestEnd { get; }

        /// <summary>
        /// The minimum time the job must spend in an oven (in seconds)
        /// </summary>
        int MinTime { get; }

        /// <summary>
        /// The maximum time the job may spend in an oven (in seconds)
        /// </summary>
        int MaxTime { get; }

        /// <summary>
        /// The size of the job 
        /// </summary>
        int Size { get; }

        /// <summary>
        /// The dictionary of attribute IDs of the job for every eligible machine; the keys are the machine IDs
        /// </summary>
        IDictionary<int, int> AttributeIdPerMachine { get; }

        /// <summary>
        /// List of IDs of eligible machines (jobs can only be processed on certain machines)
        /// </summary>
        IList<int> EligibleMachines { get; }
    }
}
