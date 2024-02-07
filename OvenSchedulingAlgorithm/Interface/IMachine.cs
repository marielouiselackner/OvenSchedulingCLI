using System;
using System.Collections.Generic;
using System.Text;

namespace OvenSchedulingAlgorithm.Interface
{   /// <summary>
    /// A machine in an instance of the oven scheduling algorithm
    /// </summary>
    public interface IMachine
    {
        /// <summary>
        /// The id of the machine
        /// </summary>
        int Id { get; }

        /// <summary>
        /// The name of the machine
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The minimum capacity of the machine
        /// </summary>
        int MinCap { get; }

        /// <summary>
        /// The maximum capacity of the machine
        /// </summary>
        int MaxCap { get; }

        /// <summary>
        /// List of start times of intervals where machine is available
        /// </summary>
        IList<DateTime> AvailabilityStart { get; }

        /// <summary>
        /// List of end times of intervals where machine is available
        /// </summary>
        IList<DateTime> AvailabilityEnd { get; }


    }
}
