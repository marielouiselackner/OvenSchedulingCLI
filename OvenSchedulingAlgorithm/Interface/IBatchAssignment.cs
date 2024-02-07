using System;
using System.Collections.Generic;
using System.Text;

namespace OvenSchedulingAlgorithm.Interface
{   /// <summary>
    /// A batch assignment for a job
    /// </summary>
    public interface IBatchAssignment
    {
        /// <summary>
        /// The job the batch assignment is created for
        /// </summary>
        IJob Job { get; }

        /// <summary>
        /// The batch the job is assigned to
        /// </summary>
        IBatch AssignedBatch { get; set; }
    }
}
