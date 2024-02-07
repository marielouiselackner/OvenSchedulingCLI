using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace OvenSchedulingAlgorithm.Interface.Implementation
{
    /// <summary>
    /// An attribute in an instance of the oven scheduling algorithm
    /// </summary>
    public class Attribute : IAttribute
    {
        /// <summary>
        /// The id of the attribute
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// The name of the attribute
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The setup costs between this attribute and other attributes (list is sorted in increasing order of attribute IDs)
        /// </summary>
        public IList<int> SetupCostsAttribute { get; }

        /// <summary>
        /// The setup times (in seconds) between this attribute and other attributes (list is sorted in increasing order of attribute IDs)
        /// </summary>
        public IList<int> SetupTimesAttribute { get; }

        /// <summary>
        /// Create an attribute in an instance of the oven scheduling algorithm
        /// </summary>
        /// <param name="id">The id of the attribute</param>
        /// <param name="name">The name of the attribute</param>
        /// <param name="setupCostsAttribute">The setup costs between this attribute and other attributes (list is sorted in increasing order of attribute IDs)</param>
        /// <param name="setupTimesAttribute">The setup times between this attribute and other attributes (list is sorted in increasing order of attribute IDs)</param>

        [JsonConstructor]
        public Attribute(
            int id,
            string name,
            IList<int> setupCostsAttribute,
            IList<int> setupTimesAttribute)
        {
            Id = id;
            Name = name;
            SetupCostsAttribute = setupCostsAttribute;
            SetupTimesAttribute = setupTimesAttribute;
        }

        ///
        /// copy constructor
        ///
        public Attribute(IAttribute other)
        {
            Id = other.Id;
            Name = other.Name;
            SetupCostsAttribute = new List<int>(other.SetupCostsAttribute.Count);
            foreach (int cost in other.SetupCostsAttribute)
            {
                SetupCostsAttribute.Add(cost);
            }
            SetupTimesAttribute = new List<int>(other.SetupTimesAttribute.Count);
            foreach (int time in other.SetupTimesAttribute)
            {
                SetupTimesAttribute.Add(time);
            }
        }
    }
}
