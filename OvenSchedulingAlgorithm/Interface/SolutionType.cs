namespace OvenSchedulingAlgorithm.Interface
{
    /// <summary>
    /// Possible solution types
    /// </summary>
    ///
    public enum SolutionType
    {
        /// <summary>
        /// solution was proven to be optimal by minizinc
        /// </summary>
        OptimalSolutionFound,
        /// <summary>
        /// valid solution found but optimality not proven
        /// </summary>
        ValidSolutionFound,
        /// <summary>
        /// Solution found, but validity has not been checked 
        /// </summary>
        UnvalidatedSolution,
        /// <summary>
        /// Instance is unsatifiable
        /// </summary>
        Unsatisfiable,
        /// <summary>
        /// No solution found within timelimit
        /// </summary>
        NoSolutionFound,
        /// <summary>
        /// Partial solution found assigning only some jobs to batches
        /// </summary>
        ValidPartialSolutionFound
    }
}