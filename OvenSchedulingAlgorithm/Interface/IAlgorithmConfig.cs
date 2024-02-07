namespace OvenSchedulingAlgorithm.Interface
{    /// <summary>
     /// Parameters used to configure the oven scheduling algorithm
     /// </summary>
    public interface IAlgorithmConfig
    {

        /// <summary>
        /// Run time limit in milliseconds
        /// </summary>
        int RunTimeLimit { get; }

        /// <summary>
        /// Boolean flag that determines whether the input and output objects should be serialized
        /// </summary>
        bool SerializeInputOutput { get; }

        /// <summary>
        /// The destination path where serialized input and output files should be stored.
        /// </summary>
        string SerializeOutputDestination { get; }

        /// <summary>
        /// Weights used in the normalised objective function
        /// </summary>
        IWeightObjective WeightsObjective { get; }

    }
}
