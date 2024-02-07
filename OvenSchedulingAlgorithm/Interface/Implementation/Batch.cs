using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace OvenSchedulingAlgorithm.Interface.Implementation
{   /// <summary>
    /// A batch in a solution of the oven scheduling algorithm
    /// </summary>
    public class Batch: IBatch
    {
        /// <summary>
        /// The id of the batch 
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The machine the batch is assigned to
        /// </summary>
        public IMachine AssignedMachine { get; set; }

        /// <summary>
        /// The start time of the batch 
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The end time of the batch 
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// The attribute of the batch 
        /// </summary>
        public IAttribute Attribute { get; set; }

        /// <summary>
        /// Checks whether the batch is equal to another batch
        /// <param name="otherBatch">Other batch</param>
        /// </summary>        
        /// <returns>True if the batches are the same.</returns>
        public bool IsEqual(IBatch otherBatch)
        {
            bool isEqual = new bool();

            if (Id== otherBatch.Id &&
                AssignedMachine.Id == otherBatch.AssignedMachine.Id &&
                StartTime == otherBatch.StartTime &&
                EndTime == otherBatch.EndTime &&
                Attribute.Id == otherBatch.Attribute.Id)
            {
                isEqual = true;
            }

            return isEqual;
        }

        /// <summary>
        /// Create a batch in a solution of the oven scheduling algorithm
        /// </summary>
        /// <param name="id">The id of the batch</param>
        /// <param name="assignedMachine">The machine the batch is assigned to</param>
        /// <param name="startTime">The start time of the batch</param>
        /// <param name="endTime">The end time of the batch</param>
        /// <param name="attribute">The attribute of the batch</param>
        [JsonConstructor]
        public Batch(
            int id,
            IMachine assignedMachine,
            DateTime startTime,
            DateTime endTime,
            IAttribute attribute
            )

        {
            Id = id;
            AssignedMachine = assignedMachine;
            StartTime = startTime;
            EndTime = endTime;
            Attribute = attribute;
        }

        /// <summary>
        /// Create a deep copy of the Batch
        /// </summary>
        /// <returns>A deep copy of the Batch</returns>
        public IBatch DeepCopy()
        {
            return new Batch(
            Id,
            new Machine(AssignedMachine),
            StartTime,
            EndTime,
            new Attribute(Attribute));
        }

    }
}
