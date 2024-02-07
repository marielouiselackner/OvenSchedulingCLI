using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace OvenSchedulingAlgorithm.Interface.Implementation
{
    /// <summary>
    /// A machine in an instance of the oven scheduling algorithm
    /// </summary>
    public class Machine : IMachine
    {
        /// <summary>
        /// The id of the machine
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// The name of the machine
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The minimum capacity of the machine
        /// </summary>
        public int MinCap { get; }

        /// <summary>
        /// The maximum capacity of the machine
        /// </summary>
        public int MaxCap { get; }

        /// <summary>
        /// List of start times of intervals where machine is available
        /// </summary>
        public IList<DateTime> AvailabilityStart { get; }

        /// <summary>
        /// List of end times of intervals where machine is available
        /// </summary>
        public IList<DateTime> AvailabilityEnd { get; }


        /// <summary>
        /// Create a machine in an instance of the oven scheduling algorithm
        /// </summary>
        /// <param name="id">The id of the machine</param>
        /// <param name="name">The name of the machine</param>
        /// <param name="minCap">The minimum capacity of the machine</param>
        /// <param name="maxCap">The maximum capacity of the machine</param>
        /// <param name="availabilityStart">List of start times of intervals where machine is available</param>
        /// <param name="availabilityEnd">List of end times of intervals where machine is available</param>
        [JsonConstructor]
        public Machine(int id, string name, int minCap, int maxCap, IList<DateTime> availabilityStart, IList<DateTime> availabilityEnd)
        {
            Id = id;
            Name = name;
            MinCap = minCap;
            MaxCap = maxCap;
            AvailabilityStart = availabilityStart;
            AvailabilityEnd = availabilityEnd;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public Machine(IMachine other)
        {
            Id = other.Id;
            Name = other.Name;
            MinCap = other.MinCap;
            MaxCap = other.MaxCap;
            AvailabilityStart = new List<DateTime>(other.AvailabilityStart);
            AvailabilityEnd = new List<DateTime>(other.AvailabilityEnd);
        }
    }
}