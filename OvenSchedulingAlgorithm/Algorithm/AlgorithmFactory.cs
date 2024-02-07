using OvenSchedulingAlgorithm.Algorithm.SimpleGreedy;
using OvenSchedulingAlgorithm.Algorithm.SimpleGreedy.Implementation;

namespace OvenSchedulingAlgorithm.Algorithm
{
    /// <summary>
    /// An algorithm factory for the oven scheduling problem
    /// </summary>
    public class AlgorithmFactory: IAlgorithmFactory
    {
        /// <summary>
        /// Returns the default oven scheduling algorithm
        /// </summary>
        /// <returns>The default oven scheduling algorithm</returns>
        public IAlgorithm GetDefaultAlgorithm()
        {
            return GetSimpleGreedyAlgorithm();
        }


        /// <summary>
        /// Returns the Simple Greedy Algorithm for the oven scheduling problem
        /// </summary>
        /// <returns>The Simple Greedy oven scheduling algorithm</returns>
        public ISimpleGreedyAlgorithm GetSimpleGreedyAlgorithm()
        {
            return new SimpleGreedyAlgorithm();
        }        

    }
}