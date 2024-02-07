using System;
using System.Collections.Generic;
using System.Text;

namespace OvenSchedulingAlgorithm.Interface
{
    /// <summary>
    /// Weights of the objective function used for optimization in the oven scheduling problem
    /// </summary>
    public interface IWeightObjective
    {
        /// <summary>
        /// Weight of the objective component cumulative processing time of ovens
        /// </summary>
        int WeightRuntime { get; }

        /// <summary>
        /// Weight of the objective component total setup times
        /// </summary>
        int WeightSetupTimes { get; }

        /// <summary>
        /// Weight of the objective component total setup costs
        /// </summary>
        int WeightSetupCosts { get; }

        /// <summary>
        /// Weight of the objective component number of tardy jobs (= jobs finished after their latest end date)
        /// </summary>
        int WeightTardiness { get; }

    }
}
