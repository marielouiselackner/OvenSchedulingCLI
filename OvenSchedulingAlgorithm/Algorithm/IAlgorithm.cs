using OvenSchedulingAlgorithm.Interface;

namespace OvenSchedulingAlgorithm.Algorithm
{
    /// <summary>
    /// An oven scheduling algorithm
    /// </summary>
    public interface IAlgorithm
    {
        /// <summary>
        /// Trigger the algorithm with the given instance
        /// </summary>
        /// <param name="instance">The given instance</param>
        /// <param name="algorithmConfig">Parameters of the algorithm</param>
        /// <returns>The output of the algorithm</returns>
        IOutput Solve(IInstance instance, IAlgorithmConfig algorithmConfig);
    }
}