using System;
using System.Collections.Generic;
using System.Text;

namespace OvenSchedulingAlgorithm.Interface.Implementation
{   
    /// <summary>
    /// Weights of the objective function used for optimization in the oven scheduling problem
    /// </summary>
    public class WeightObjective : IWeightObjective
    {
        /// <summary>
        /// Weight of the objective component cumulative processing time of ovens
        /// </summary>
        public int WeightRuntime { get; }

        /// <summary>
        /// Weight of the objective component total setup times
        /// </summary>
        public int WeightSetupTimes{ get; }

        /// <summary>
        /// Weight of the objective component total setup costs
        /// </summary>
        public int WeightSetupCosts { get; }

        /// <summary>
        /// Weight of the objective component number of tardy jobs (= jobs finished after their latest end date)
        /// </summary>
        public int WeightTardiness { get; }

        /// <summary>
        /// Create weights of the objective function used for optimization in the oven scheduling problem
        /// </summary>
        /// <param name="weightRuntime">Weight of the objective component cumulative processing time of ovens</param>
        /// <param name="weightSetupTimes">Weight of the objective component total setup times</param>
        /// <param name="weightSetupCosts">Weight of the objective component total setup costs</param>
        /// <param name="weightTardiness">Weight of the objective component number of tardy jobs</param>

        public WeightObjective(
            int weightRuntime,
            int weightSetupTimes,
            int weightSetupCosts,
            int weightTardiness
            )
        {
            WeightRuntime = weightRuntime;
            WeightSetupTimes = weightSetupTimes;
            WeightSetupCosts = weightSetupCosts;
            WeightTardiness = weightTardiness;
        }

        /// <summary>
        /// Create equal weights of the objective function used for optimization in the oven scheduling problem,
        /// i.e. all weights are equal to weight
        /// </summary>
        /// <param name="weight">Weight of all objective components </param> 
        public WeightObjective(int weight)
        {
            WeightRuntime = weight;
            WeightSetupTimes = weight;
            WeightSetupCosts = weight;
            WeightTardiness = weight;
        }

        ///
        /// copy constructor
        ///
        public WeightObjective(IWeightObjective other)
        {
            WeightRuntime = other.WeightRuntime;
            WeightSetupTimes = other.WeightSetupTimes;
            WeightSetupCosts = other.WeightSetupCosts;
            WeightTardiness = other.WeightTardiness;
        }

    }
}
