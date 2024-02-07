using System;
using System.Collections.Generic;
using System.Text;

namespace OvenSchedulingAlgorithm.Interface
{   /// <summary>
    /// A batch in a solution of the oven scheduling algorithm
    /// </summary>
    public interface IBatch
    {
        /// <summary>
        /// The id of the batch
        /// </summary>
        int Id { get; set; }

        /// <summary>
        /// The machine the batch is assigned to
        /// </summary>
        IMachine AssignedMachine { get; set; }

        /// <summary>
        /// The start time of the batch 
        /// </summary>
        DateTime StartTime { get; set; }

        /// <summary>
        /// The end time of the batch 
        /// </summary>
        DateTime EndTime { get; set; }

        /// <summary>
        /// The attribute of the batch 
        /// </summary>
        IAttribute Attribute { get; set; }

        /// <summary>
        /// Checks whether the batch is equal to another batch
        /// <param name="otherBatch">Other batch</param>
        /// </summary>        
        /// <returns>True if the batches are the same.</returns>
        public bool IsEqual(IBatch otherBatch);

        /// <summary>
        /// Create a deep copy of the Batch
        /// </summary>
        /// <returns>A deep copy of the Batch</returns>
        public IBatch DeepCopy();

    }
}
