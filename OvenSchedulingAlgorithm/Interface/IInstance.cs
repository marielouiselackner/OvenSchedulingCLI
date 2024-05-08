using System;
using System.Collections.Generic;

namespace OvenSchedulingAlgorithm.Interface
{/// <summary>
    /// An instance for the oven scheduling algorithm
    /// </summary>
    public interface IInstance 
    {
        /// <summary>
        /// The name of the instance
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The time when the instance was created
        /// </summary>
        DateTime CreationDate { get; }

        /// <summary>
        /// The dictionary of machines
        /// </summary>
        IDictionary<int, IMachine> Machines { get; }

        /// <summary>
        /// The dictionary of initial state IDs for every machine; the keys are the machine IDs
        /// </summary>
        IDictionary<int, int> InitialStates { get; }

        /// <summary>
        /// The list of jobs
        /// </summary>
        IList<IJob> Jobs { get; }

        /// <summary>
        /// The dictionary of attributes, keys are attribute IDs
        /// </summary>
        IDictionary<int,IAttribute> Attributes { get; }

        /// <summary>
        /// The start of the scheduling horizon as a reference date
        /// </summary>
        DateTime SchedulingHorizonStart { get; }

        /// <summary>
        /// The end of the scheduling horizon 
        /// </summary>
        DateTime SchedulingHorizonEnd { get; }

        /// <summary>
        /// Serialize the instance to a json file
        /// </summary>
        /// <param name="fileName">Location of the serialized filed</param>
        void Serialize(string fileName);
    }
}
