using System;
using System.Collections.Generic;
using System.Text;

namespace OvenSchedulingAlgorithm.Interface
{
    /// <summary>
    /// An output of the oven scheduling algorithm
    /// </summary>
    public interface IOutput
    {
        /// <summary>
        /// The name of the output
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The date the output was created
        /// </summary>
        DateTime CreationDate { get; }

        /// <summary>
        /// The list of batch assignments that the algorithm generated
        /// </summary>
        IList<IBatchAssignment> BatchAssignments { get; }

        /// <summary>
        /// The list of solution types 
        /// (if more than one entry: the instance has been solved using the SplitAndSolve algorithm 
        /// and solution types are given for every short intervall)
        /// </summary>
        IList<SolutionType> SolutionTypes { get; }

        /// <summary>
        /// Creates the list of batches that the algorithm generated from the list of batch assignments
        /// </summary>
        /// <returns>List of batches.</returns>
        IList<IBatch> GetBatches();

        /// <summary>
        /// Creates the dictionary of batches that the algorithm generated from the list of batches
        /// </summary>
        /// <returns>The dictionary of batches, keys are (machineId, batch position = batchId).</returns>
        IDictionary<(int mach, int pos), IBatch> GetBatchDictionary();

        /// <summary>
        /// Serialize the solution to a json file
        /// </summary>
        /// <param name="fileName">Location of the serialized filed</param>
        void Serialize(string fileName);        
    }
}