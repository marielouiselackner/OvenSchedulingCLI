using System.Collections.Generic;
using OvenSchedulingAlgorithm.Interface;
using System;

namespace OvenSchedulingAlgorithm.Objective
{
    /// <summary>
    /// Breaks down the objective of a solution of an oven scheduling problem into smaller intervals and into machines
    /// and writes these values to a string for a .csv file
    /// </summary>
    public interface IObjectiveWriter
    
    {
        /// <summary>
        /// Writes the objective of a solution of an oven scheduling problem to a string for .csv file
        /// Objective is broken down to machines and smaller intervals
        /// </summary>
        /// <param name="instance">the instance of the oven scheduling problem</param>
        /// <param name="output">the solution of the oven scheduling problem</param>
        /// <param name="interval">the length of the interval into which the objective should be broken done to</param>
        /// <returns>string that can be written to .csv file</returns>
        public string WriteObjectiveToCsv(IInstance instance, IOutput output, TimeSpan interval);

        /// <summary>
        /// Calculates the runtime per oven and per interval for a given solution of the oven scheduling problem
        /// Note that the runtime of a batch is counted to the interval in which the batch starts and 
        /// is not split if the batch spans several intervals
        /// </summary>
        /// <param name="instance">the instance of the oven scheduling problem</param>
        /// <param name="output">the solution of the oven scheduling problem</param>
        /// <param name="interval">the length of the interval</param>
        /// <returns>An array of runtimes per interval and per machine, an array of runtimes per interval and an array of machine names</returns>
        public (string[] intervalDescription, TimeSpan[,] runtimePerMachinePerInterval, TimeSpan[] runtimePerInterval, string[] machineName)
            CalculateRuntime(IInstance instance, IOutput output, TimeSpan interval);


    }
}
