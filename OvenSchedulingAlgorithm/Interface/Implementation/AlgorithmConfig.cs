namespace OvenSchedulingAlgorithm.Interface.Implementation
{
    /// <summary>
    /// Parameters used to configure the oven scheduling algorithm
    /// </summary>
    public class AlgorithmConfig : IAlgorithmConfig
    {
        /// <summary>
        /// Run time limit in milliseconds
        /// </summary>
        public int RunTimeLimit { get; }

          /// <summary>
        /// Boolean flag that determines whether the input and output objects should be serialized
        /// </summary>
        public bool SerializeInputOutput { get; }

        /// <summary>
        /// The destination path where serialized input and output files should be stored.
        /// </summary>
        public string SerializeOutputDestination { get; }

        /// <summary>
        /// Weights used in the normalised objective function
        /// </summary>
        public IWeightObjective WeightsObjective { get; }

 
        /// <summary>
        /// Create algorithm parameters for the oven scheduling algorithm
        /// </summary>
        /// <param name="runTimeLimit">Run time limit in milliseconds</param>
        /// <param name="serializeInputOutput">Boolean flag that determines whether the input and output objects should be serialized</param>
        /// <param name="serializeOutputDestination">The destination path where serialized input and output files should be stored</param>
        /// <param name="weightsObjective">Weights used in the normalised objective function</param>
        public AlgorithmConfig(
            int runTimeLimit,
            bool serializeInputOutput,
            string serializeOutputDestination,
            IWeightObjective weightsObjective
            )
        {
            RunTimeLimit = runTimeLimit;
            SerializeInputOutput = serializeInputOutput;
            SerializeOutputDestination = serializeOutputDestination;
            WeightsObjective = weightsObjective;            
        }

        ///
        /// copy constructor
        ///
        public AlgorithmConfig(IAlgorithmConfig other)
        {
            RunTimeLimit = other.RunTimeLimit;
            SerializeInputOutput = other.SerializeInputOutput;
            SerializeOutputDestination = other.SerializeOutputDestination;
            WeightsObjective = other.WeightsObjective;
        }
    }
}
