using System;
using System.Collections.Generic;
using System.Globalization;
using OvenSchedulingAlgorithm.Interface;
using OvenSchedulingAlgorithm.Converter;
using OvenSchedulingAlgorithm.Converter.Implementation;
using OvenSchedulingAlgorithm.Objective.Implementation.Resources;

namespace OvenSchedulingAlgorithm.Objective.Implementation
{
    /// <summary>
    /// Breaks down the objective of a solution of an oven scheduling problem into smaller intervals and into machines
    /// and writes these values to a string for a .csv file
    /// </summary>
    public class ObjectiveWriter : IObjectiveWriter
    {


        /// <summary>
        /// Writes the objective of a solution of an oven scheduling problem to a string for .csv file
        /// Objective is broken down to machines and smaller intervals
        /// </summary>
        /// <param name="instance">the instance of the oven scheduling problem</param>
        /// <param name="output">the solution of the oven scheduling problem</param>
        /// <param name="interval">the length of the interval into which the objective should be broken done to</param>
        /// <returns>string that can be written to .csv file</returns>
        public string WriteObjectiveToCsv(IInstance instance, IOutput output, TimeSpan interval)
        {
            int m = instance.Machines.Count;

            //calculate runtimes
            var runtime = CalculateRuntime(instance, output, interval);

            string[] intervalDescription = runtime.intervalDescription;
            TimeSpan[,] runtimePerMachinePerInterval = runtime.runtimePerMachinePerInterval;
            TimeSpan[] runtimePerInterval = runtime.runtimePerInterval;
            string[] machineName = runtime.machineName;
            int nb = runtimePerInterval.Length - 1;


            // write to csv file
            string csvFileContents = ";";

            //headers: interval description
            for (int i = 0; i < intervalDescription.Length; i++)
            {
                csvFileContents += intervalDescription[i] + ";" ;
            }
            csvFileContents += "\n";

            //runtime per machine
            for (int j = 0; j < m; j++)
            {
                csvFileContents += machineName[j] + ";";
                for (int i = 0; i < nb + 1; i++)
                {
                    csvFileContents += runtimePerMachinePerInterval[j, i].ToString("%d") + " " +
                                       ObjectiveWriterResources.Days + " " +
                                       runtimePerMachinePerInterval[j, i].ToString(@"hh\:mm") + ";";
                }
                csvFileContents += "\n";
            }

            // total runtime 
            csvFileContents += ObjectiveWriterResources.Total + ";";
            for (int i = 0; i < nb+1; i++)
            {
                csvFileContents += runtimePerInterval[i].ToString("%d") + " " + ObjectiveWriterResources.Days + " " +
                                   runtimePerInterval[i].ToString(@"hh\:mm") + ";";
            }



            return csvFileContents;
        }

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
            CalculateRuntime(IInstance instance, IOutput output, TimeSpan interval)
        {
            IList<IJob> jobs = instance.Jobs;
            int m = instance.Machines.Count;
            IDictionary<int, IAttribute> attributes = instance.Attributes;
            IList<IBatch> batches = output.GetBatches();

            //need to convert machine Ids to 1..m
            IMiniZincConverter miniZincConverter = new MiniZincConverter();
            Func<int, int> MiniZincMachineId = miniZincConverter.ConvertMachineIdToMinizinc(instance.Machines);
            Func<int, int> OriginalMachineId = miniZincConverter.ConvertMinizincMachineIdToId(instance.Machines);


            TimeSpan schedulingHorizonLength = instance.SchedulingHorizonEnd.Subtract(instance.SchedulingHorizonStart);
            long OriginalSchedulingHorizonInTicks = schedulingHorizonLength.Ticks;
            long ShortSchedulingHorizonInTicks = interval.Ticks;

            // number of intervals for which objective value should be calculated
            int nb = (int)Math.Ceiling((double)OriginalSchedulingHorizonInTicks / (double)ShortSchedulingHorizonInTicks);

            string[] intervalDescription = new string[nb + 1];
            TimeSpan[,] runtimePerMachinePerInterval = new TimeSpan[m, nb + 1];
            TimeSpan[] runtimePerInterval = new TimeSpan[nb + 1];
            string[] machineName = new string[m];

            TimeSpan runtimeBatch;

            foreach (IBatch batch in batches)
            {
                runtimeBatch = batch.EndTime.Subtract(batch.StartTime);

                int assignedMachineId = MiniZincMachineId(batch.AssignedMachine.Id);
                string name = batch.AssignedMachine.Name;
                // batch starts in interval i:
                long horizonStartToBatchStartInTicks = batch.StartTime.Subtract(instance.SchedulingHorizonStart).Ticks;
                int i = (int)Math.Floor((double)horizonStartToBatchStartInTicks / (double)ShortSchedulingHorizonInTicks);

                //write name of assigned machine (this is overwritten with every batch
                machineName[assignedMachineId - 1] = name;

                //add runtime of batch to assigned machine and corresponding shift
                runtimePerMachinePerInterval[assignedMachineId - 1, i] += runtimeBatch;
                //add runtime of batch to total runtime assigned machine
                runtimePerMachinePerInterval[assignedMachineId - 1, nb] += runtimeBatch;
                //add runtime of batch to corresponding shift
                runtimePerInterval[i] += runtimeBatch;
                //add runtime of batch to total runtime
                runtimePerInterval[nb] += runtimeBatch;
            }

            DateTime startInterval = instance.SchedulingHorizonStart;
            DateTime endInterval = startInterval.Add(interval);
            for (int i = 0; i < nb; i++)
            {
                intervalDescription[i] = ObjectiveWriterResources.Interval + " " +
                                         (i + 1).ToString(CultureInfo.InvariantCulture) + ": " +
                                         startInterval.ToString("dd MM yyyy HH:mm:ss", CultureInfo.InvariantCulture) +
                                         " " + ObjectiveWriterResources.to + " " +
                                         endInterval.ToString("dd MM yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                startInterval += interval;
                endInterval += interval;
            }
            intervalDescription[nb] = ObjectiveWriterResources.Total;


            return (intervalDescription, runtimePerMachinePerInterval, runtimePerInterval, machineName);
        }



    }
}
