using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OvenSchedulingAlgorithm.Interface.Implementation
{
    /// <summary>
    /// A job in an instance of the oven scheduling algorithm
    /// </summary>
    public class Job : IJob
    {
        /// <summary>
        /// The id of the job
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// The name of the job
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The earliest start time of the job
        /// </summary>
        public DateTime EarliestStart { get; }

        /// <summary>
        /// The latest end time (=deadline) of the job
        /// </summary>
        public DateTime LatestEnd { get; }

        /// <summary>
        /// The minimum time the job must spend in an oven (in seconds)
        /// </summary>
        public int MinTime { get; }

        /// <summary>
        /// The maximum time the job may spend in an oven (in seconds)
        /// </summary>
        public int MaxTime { get; }

        /// <summary>
        /// The size of the job 
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// The dictionary of attribute IDs of the job for every eligible machine; the keys are the machine IDs
        /// </summary>
        public IDictionary<int, int> AttributeIdPerMachine { get; }

        /// <summary>
        /// List of IDs of eligible machines (jobs can only be processed on certain machines)
        /// </summary>
        public IList<int> EligibleMachines { get; }

        /// <summary>
        /// Create a job in an instance of the oven scheduling algorithm
        /// </summary>
        /// <param name="id">The id of the job</param>
        /// <param name="name">The name of the job</param>
        /// <param name="earliestStart">The earliest start time of the job</param>
        /// <param name="latestEnd">The latest end time of the job</param>
        /// <param name="minTime"> The minimum time the job must spend in an oven (in seconds)</param>
        /// <param name="maxTime"> The maximum time the job may spend in an oven (in seconds)</param>
        /// <param name="size">The size of the job </param>
        /// <param name="attributeIdPerMachine">The attribute IDs of the job for evry eligible machine </param>
        /// <param name="eligibleMachines">The list of eligible machines for this job </param>
        [JsonConstructor]
        public Job(
            int id, 
            string name, 
            DateTime earliestStart, 
            DateTime latestEnd, 
            int minTime, 
            int maxTime, 
            int size,
            IDictionary<int, int> attributeIdPerMachine, 
            IList<int> eligibleMachines)
        {
            Id = id;
            Name = name;
            EarliestStart = earliestStart;
            LatestEnd = latestEnd;
            MinTime = minTime;
            MaxTime = maxTime;
            Size = size;
            AttributeIdPerMachine = attributeIdPerMachine;
            EligibleMachines = eligibleMachines;
        }

        ///
        /// copy constructor
        ///
        public Job(IJob other)
        {
            Id = other.Id;
            Name = other.Name;
            EarliestStart = other.EarliestStart;
            LatestEnd = other.LatestEnd;
            MinTime = other.MinTime;
            MaxTime = other.MaxTime;
            Size = other.Size;
            AttributeIdPerMachine = other.AttributeIdPerMachine;
            EligibleMachines = new List<int>(other.EligibleMachines.Count);
            foreach (int eligibleMachine in other.EligibleMachines)
            {
                EligibleMachines.Add(eligibleMachine);
            }
        }
    }
}