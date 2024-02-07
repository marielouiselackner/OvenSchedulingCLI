using System;
using System.Collections.Generic;
using System.Text;

namespace OvenSchedulingAlgorithm.Interface
{   /// <summary>
    /// An attribute in an instance of the oven scheduling algorithm
    /// </summary>
    public interface IAttribute
    {
        /// <summary>
        /// The id of the attribute
        /// </summary>
        int Id { get; }

        /// <summary>
        /// The name of the attribute
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The setup costs between this attribute and other attributes (list is sorted in increasing order of attribute IDs)
        /// </summary>
        IList<int> SetupCostsAttribute { get; }

        /// <summary>
        /// The setup times (in seconds) between this attribute and other attributes (list is sorted in increasing order of attribute IDs)
        /// </summary>
        IList<int> SetupTimesAttribute { get; }


    }
}
