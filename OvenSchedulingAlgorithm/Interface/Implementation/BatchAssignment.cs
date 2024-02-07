using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace OvenSchedulingAlgorithm.Interface.Implementation
{
    /// <summary>
    /// A batch assignment for a job
    /// </summary>
    public class BatchAssignment : IBatchAssignment
    {
        /// <summary>
        /// The job the batch assignment is created for
        /// </summary>
        public IJob Job { get; }

        /// <summary>
        /// The batch the job is assigned to
        /// </summary>
        public IBatch AssignedBatch { get; set; }

        /// <summary>
        /// Create a batch assignment for a job
        /// </summary>
        /// <param name="job">The job the batch assignment is created for</param>
        /// <param name="assignedBatch">The batch the job is assigned to</param>
        [JsonConstructor]
        public BatchAssignment(IJob job, IBatch assignedBatch)
        {
            Job = job;
            AssignedBatch = assignedBatch;
        }

    }
}