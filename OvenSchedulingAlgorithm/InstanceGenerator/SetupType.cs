using System;
using System.Collections.Generic;
using System.Text;

namespace OvenSchedulingAlgorithm.InstanceGenerator
{
    /// <summary>
    /// Possible types for setup times and costs
    /// </summary>
    public enum SetupType
    {
        /// <summary>
        /// no setup times or costs 
        /// </summary>
        none,
        /// <summary>
        /// constant setup times or costs 
        /// </summary>
        constant,
        /// <summary>
        /// purely random setup times or costs 
        /// </summary>
        arbitrary,
        /// <summary>
        /// setup times or costs that are closer to reality: 
        /// setup between batches of the same attribute is smaller than between batches of different attributes
        /// </summary>
        realistic,
        /// <summary>
        /// symmetric setup times or costs
        /// </summary>
        symmetric
    }
}
