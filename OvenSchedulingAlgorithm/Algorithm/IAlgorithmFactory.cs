using OvenSchedulingAlgorithm.Algorithm.SimpleGreedy;

namespace OvenSchedulingAlgorithm.Algorithm
{
    /// <summary>
    /// An algorithm factory for the oven scheduling problem
    /// </summary>
    public interface IAlgorithmFactory
    {
        /// <summary>
        /// Returns the default oven scheduling algorithm
        /// </summary>
        /// <returns>The default oven scheduling algorithm</returns>
        IAlgorithm GetDefaultAlgorithm();


        /// <summary>
        /// Returns the Simple Greedy Algorithm for the oven scheduling problem
        /// </summary>
        /// <returns>The Simple Greedy oven scheduling algorithm</returns>
        ISimpleGreedyAlgorithm GetSimpleGreedyAlgorithm();

    }
}
