using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using OvenSchedulingAlgorithm.Interface;
using OvenSchedulingAlgorithm.Interface.Implementation;
using Microsoft.VisualBasic;
using System.IO;
using OvenSchedulingAlgorithm.Objective.Implementation;

namespace OvenSchedulingAlgorithmCLI
{
    //internal class SolutionValidator
    public class SolutionValidator
    {
        private readonly IInstance _instance;
        private readonly int _nJobs;
        private readonly IOutput _solution;
        private readonly IList<IBatch> _batches;
        private readonly IDictionary<(int mach, int pos), IBatch> _batchDictionary;
        private readonly IDictionary<(int mach, int pos), (int setupTime, int setupCost)> _setupDict;
        private readonly string _logfilename;
        private IAlgorithmConfig _algoconfig;
        private int _totalViolations;
        private int _jobsScheduledOutsideHorizon;
        private string _logfiledata;

        public SolutionValidator(IInstance instance, IOutput solution, string logfilename, int alpha, int beta, int gamma, int delta)
        {
            _instance = instance;
            _nJobs = _instance.Jobs.Count; 
            _solution = solution;
            _batches = solution.GetBatches();
            _batchDictionary = solution.GetBatchDictionary();
            _setupDict = Output.GetSetupTimesAndCostsDictionary(instance, _batchDictionary);
            _logfilename = logfilename;
            _algoconfig = new AlgorithmConfig(0, 
                false,
                "",
                new WeightObjective(alpha, beta, gamma, delta));
            _logfiledata = "";
        }

        public SolutionValidator(IInstance instance, IOutput solution, string logfilename, IAlgorithmConfig algoconfig)
        {
            _instance = instance;
            _nJobs = _instance.Jobs.Count;
            _solution = solution;
            _batches = solution.GetBatches();
            _batchDictionary = solution.GetBatchDictionary();
            _setupDict = Output.GetSetupTimesAndCostsDictionary(instance, _batchDictionary);
            _logfilename = logfilename;
            _algoconfig = algoconfig;
            _logfiledata = "";
        }

        /// <summary>
        /// Calculate number of violations and write info to console
        /// </summary>
        /// <returns></returns>
        public int ViolationCheck()
        {
            //check for violations
            ViolationCounter violationCounter = new ViolationCounter(_instance, _solution,
                _batchDictionary, _setupDict, _logfilename, _algoconfig);
            var violResult = violationCounter.ValidateSolution(true);
            if (violResult.jobsScheduledOutsideOrUnscheduled == 0)
            {
                Console.WriteLine("All jobs have been scheduled");
            }
            else
            {
                int assignments = _nJobs - violResult.jobsScheduledOutsideOrUnscheduled;
                Console.WriteLine("Only {0} out of {1} jobs have been scheduled", assignments, _nJobs);
                //only solutions where all jobs are scheduled are accepted
                return int.MaxValue;
            }
            _totalViolations = violResult.violations;
            // print total violations
            Console.WriteLine("The total number of violations is: {0}", _totalViolations);

            return _totalViolations;    
        }

        /// <summary>
        /// Validate solution and determine constraint violations and solution costs
        /// </summary>
        public int ValidateSolution()
        {
            ViolationCheck();

            //calculate objective value
            CalculateObjective objectiveCalculator = new CalculateObjective(_instance, _solution,
                _setupDict, _algoconfig);
            var objResult = objectiveCalculator.CalculateComponentsObjectiveReal();

            //total runtime of ovens (in seconds)
            double totalRuntime = objResult.TotalRuntimeSeconds;
            int totalRuntimeDays = (int)Math.Floor(totalRuntime / 60 / 60 / 24);
            int totalRuntimeHours = (int)Math.Floor((totalRuntime - 24 * 60 * 60 * totalRuntimeDays) / 60 / 60);
            int totalRuntimeMinutes = (int)Math.Floor((totalRuntime - 24 * 60 * 60 * totalRuntimeDays - 60 * 60 * totalRuntimeHours) / 60);
            int totalRuntimeSeconds = (int)(totalRuntime - 24 * 60 * 60 * totalRuntimeDays - 60 * 60 * totalRuntimeHours - 60 * totalRuntimeMinutes);
            Console.WriteLine("Total runtime of ovens is {0} day(s) {1} hour(s) {2} minute(s) {3} second(s).", totalRuntimeDays, totalRuntimeHours, totalRuntimeMinutes, totalRuntimeSeconds);
            
            //finished_too_late
            int finishedTooLate = objResult.FinishedTooLate;
            Console.WriteLine("The number of jobs that finished too late is {0}.", finishedTooLate);

            //total_setup_times and total_setup_costs
            int totalSetupTimes = objResult.TotalSetupTimesSeconds;
            int totalSetupCosts = objResult.TotalSetupCosts;
            Console.WriteLine("The total setup times are {0} minutes and setup costs are {1}", totalSetupTimes / 60, totalSetupCosts);


            //normalized objective
            double normalized_objective = objResult.ObjectiveValue;
            Console.WriteLine("The normalized weighted objective is {0} (value between 0 and 1)", normalized_objective);

            //additional info about solution
            int b = _batches.Count();
            double avg_jobs_per_batch = (double)_nJobs / b;
            int max_processingTime = _instance.Jobs.Select(x => x.MinTime).Sum();
            double oven_runtime_reduction = (double)(max_processingTime / totalRuntime);

            //write data to file
            _logfiledata += "number of batches: " + b + ";\n";
            _logfiledata += "average number of jobs per batch: " + avg_jobs_per_batch + ";\n";
            _logfiledata += "oven runtime reduction: " + oven_runtime_reduction + ";\n";
            int totalRuntimeInMinutes = (int)Math.Floor(totalRuntime / 60);
            _logfiledata += "cost: total running time: " + totalRuntimeInMinutes + ", ";
            _logfiledata += "total setup time: " + totalSetupTimes / 60 + ", ";
            _logfiledata += "total setup costs: " + totalSetupCosts + ", ";
            _logfiledata += "number of too late jobs: " + finishedTooLate + ";\n";
            _logfiledata += "CostVector: [" + totalRuntimeInMinutes + ", " + totalSetupTimes / 60 + ", " + totalSetupCosts + ", " + finishedTooLate + "];\n";
            _logfiledata += "objective: " + normalized_objective + ";\n \n";

            //if logfilename is not empty string, create logfile
            if (!String.IsNullOrEmpty(_logfilename))
            {
                string logfilepath = _logfilename + ".out";
                using (StreamWriter sw = File.AppendText(logfilepath))
                {
                    sw.Write(_logfiledata);
                    sw.Close();
                }
            }            

            return _totalViolations;
        }

        /// <summary>
        /// Validate solution and determine constraint violations and solution costs for the special case of lexicographic minimization
        /// </summary>
        public int ValidateSolutionLexMinSpecialCase(int upperBoundIntObj)
        {
            ViolationCheck();

            //calculate objective value
            CalculateObjective objectiveCalculator = new CalculateObjective(_instance, _solution,
                _setupDict, _algoconfig);
            var objResult = objectiveCalculator.CalculateComponentsObjectiveReal();

            //total runtime of ovens (in seconds)
            double totalRuntime = objResult.TotalRuntimeSeconds;
            int totalRuntimeDays = (int)Math.Floor(totalRuntime / 60 / 60 / 24);
            int totalRuntimeHours = (int)Math.Floor((totalRuntime - 24 * 60 * 60 * totalRuntimeDays) / 60 / 60);
            int totalRuntimeMinutes = (int)Math.Floor((totalRuntime - 24 * 60 * 60 * totalRuntimeDays - 60 * 60 * totalRuntimeHours) / 60);
            int totalRuntimeSeconds = (int)(totalRuntime - 24 * 60 * 60 * totalRuntimeDays - 60 * 60 * totalRuntimeHours - 60 * totalRuntimeMinutes);
            Console.WriteLine("Total runtime of ovens is {0} day(s) {1} hour(s){2} minute(s) {3} second(s).", totalRuntimeDays, totalRuntimeHours, totalRuntimeMinutes, totalRuntimeSeconds);

            //finished_too_late
            int finishedTooLate = objResult.FinishedTooLate;
            Console.WriteLine("The number of jobs that finished too late is {0}.", finishedTooLate);

            //total_setup_times and total_setup_costs
            int totalSetupTimes = objResult.TotalSetupTimesSeconds;
            int totalSetupCosts = objResult.TotalSetupCosts;
            Console.WriteLine("The total setup times are {0} minutes and setup costs are {1}", totalSetupTimes / 60, totalSetupCosts);

            //integer value objective
            double obj_int = objResult.TotalRuntimeSeconds / 60 * _algoconfig.WeightsObjective.WeightRuntime
                + objResult.TotalSetupCosts * _algoconfig.WeightsObjective.WeightSetupCosts
                + objResult.FinishedTooLate * _algoconfig.WeightsObjective.WeightTardiness;
            Console.WriteLine("The integer valued objective is: {0}", obj_int);      

            //normalized objective
            double normalized_objective = obj_int / upperBoundIntObj;
            Console.WriteLine("The normalized weighted objective is {0} (value between 0 and 1)", normalized_objective);

            //additional info about solution
            int b = _batches.Count();
            double avg_jobs_per_batch = (double)_nJobs / b;
            int max_processingTime = _instance.Jobs.Select(x => x.MinTime).Sum();
            double oven_runtime_reduction = (double)(max_processingTime / totalRuntime);

            //write data to file
            _logfiledata += "number of batches: " + b + ";\n";
            _logfiledata += "average number of jobs per batch: " + avg_jobs_per_batch + ";\n";
            _logfiledata += "oven runtime reduction: " + oven_runtime_reduction + ";\n";
            int totalRuntimeInMinutes = (int)Math.Floor(totalRuntime / 60);
            _logfiledata += "cost: total running time: " + totalRuntimeInMinutes + ", ";
            _logfiledata += "total setup time: " + totalSetupTimes / 60 + ", ";
            _logfiledata += "total setup costs: " + totalSetupCosts + ", ";
            _logfiledata += "number of too late jobs: " + finishedTooLate + ";\n";
            _logfiledata += "CostVector: [" + totalRuntimeInMinutes + ", " + totalSetupTimes / 60 + ", " + totalSetupCosts + ", " + finishedTooLate + "];\n";
            _logfiledata += "integer_objective: " + obj_int + ";\n \n";
            _logfiledata += "objective: " + normalized_objective + ";\n \n";

            //if logfilename is not empty string, create logfile
            if (!String.IsNullOrEmpty(_logfilename))
            {
                string logfilepath = _logfilename + ".out";
                using (StreamWriter sw = File.AppendText(logfilepath))
                {
                    sw.Write(_logfiledata);
                    sw.Close();
                }
            }

            return _totalViolations;
        }

    }
}