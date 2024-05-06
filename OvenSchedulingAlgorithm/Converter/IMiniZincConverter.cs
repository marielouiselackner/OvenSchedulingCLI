using System;
using System.Collections.Generic;
using OvenSchedulingAlgorithm.Algorithm;
using OvenSchedulingAlgorithm.Interface;

namespace OvenSchedulingAlgorithm.Converter
{
    /// <summary>
    /// Converter that creates MiniZinc instance files from a given instance information
    /// and converts back MiniZinc solution files to solutions of the original problem
    /// </summary>
    public interface IMiniZincConverter
    {
        /// <summary>
        /// Take an instance an convert it into a MiniZinc instance file content
        /// </summary>
        /// <param name="instance">The instance that should be converted</param>
        /// <param name="weights">Weights of the components of the objective function that should be used by the minizinc solver</param>
        /// <param name="convertToCPOptimizer">Optional boolean indicating whether insatnce should be converted to CP Optimizer instance instead of minizinc instance</param>
        /// <param name="extraZerosSetup">Optional boolean indicating whether extra zeroes should be added to the matrix of setup times and costs</param>
        /// <param name="specialCaseLexicographicOptimization">Optional boolean indicating whether weights should be created for the case of lexicographic minimization 
        /// with total oven runtime lexicographically more important than tardiness, 
        /// tardiness lexicographically more important than setup costs.</param>
        /// <returns>Return the MiniZinc instance file contents as a string</returns>
        string ConvertToMiniZincInstance(IInstance instance, IWeightObjective weights, 
            bool convertToCPOptimizer = false, 
            bool specialCaseLexicographicOptimization = false);

        /// <summary>
        /// Take a (partial) initial solution and convert it into a MiniZinc additional instance file content
        /// which van be used for warm start in MiniZinc
        /// </summary>
        /// /// <param name="instance">The instance for which the partial solution was created</param>
        /// <param name="partialSolution">The partial solution that should be converted</param>
        /// <param name="reprJobPerBatch">Optional parameter indictaing whether the warm start data is created for a 
        /// minizinc model with a representative job per batch</param>
        /// <returns>Return the MiniZinc warm start data file contents as a string</returns>
        string ConvertToMiniZincWarmStartData(IInstance instance, IOutput partialSolution, bool reprJobPerBatch = false, bool convertToCPOptimizer = false);

        /// <summary>
        /// Create content of a MiniZinc weights file from weights of the objective function
        /// </summary>
        /// <param name="weights">The weights that should be used</param>
        /// <returns>Return the MiniZinc weights file contents as a string</returns>
        string ConvertToMiniZincWeights(IWeightObjective weights);
        
        /// <summary>
        /// Convert a given MiniZinc solution file into an OvenScheduling Object
        /// </summary>
        /// <param name="instance">Instance information associated to the solution</param>
        /// <param name="solutionFileContents">The contents of the solution file that should be parsed as a string</param>
        /// <returns>Creates output consisting of a list of converted batches 
        /// and list of converted batch assignments for each job (if no solution could be found, the output will be empty).</returns>
        IOutput ConvertMiniZincSolutionFile(IInstance instance, string solutionFileContents);

        /// <summary>
        /// Takes a dictionary of machines and returns a function that converts machine IDs to corresponding machine IDs in MiniZinc
        /// (for MiniZinc instance, machines need to be numbered from 1 to m,
        /// general Ids are distinct integers in an arbitrary range.
        /// therefore: machine with smallest Id gets number 1, machine with next smallest number 2 etc.)
        /// </summary>
        /// <param name="machines">The dictionary of machines for which conversion function is created</param>
        /// <returns>Return the function that converts Machine Ids to Minizinc Ids </returns>
        Func<int, int> ConvertMachineIdToMinizinc(IDictionary<int, IMachine> machines);

        /// <summary>
        /// Takes a dictionary of machines and returns a function that converts MiniZinc machine IDs to corresponding original machine IDs
        /// (for MiniZinc instance, machines are numbered from 1 to m, 
        /// general Ids are distinct integers in an arbitrary range)
        /// </summary>
        /// <param name="machines">The dictionary of machines for which conversion function is created</param>
        /// <returns>Return the function that converts Minizinc Ids to Machine Ids </returns>
        Func<int, int> ConvertMinizincMachineIdToId(IDictionary<int, IMachine> machines);
		
		/// <summary>
        /// Takes a dictionary of attributes and returns a function that converts attribute IDs to corresponding attribute IDs in MiniZinc
        /// (for MiniZinc instance, attributes need to be numbered from 1 to a,
        /// general Ids are distinct integers in an arbitrary range.
        /// therefore: attribute with smallest Id gets number 1, attribute with next smallest number 2 etc.)
        /// </summary>
        /// <param name="attributes">The dictionary of attributes for which conversion function is created</param>
        /// <returns>Return the function that converts Attribute Ids to Minizinc Ids </returns>
        Func<int, int> ConvertAttributeIdToMinizinc(IDictionary<int, IAttribute> attributes);
    }
}
