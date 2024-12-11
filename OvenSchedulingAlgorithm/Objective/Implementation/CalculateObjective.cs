using OvenSchedulingAlgorithm.Algorithm;
using OvenSchedulingAlgorithm.InstanceChecker;
using OvenSchedulingAlgorithm.Interface;
using OvenSchedulingAlgorithm.Interface.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IInstance = OvenSchedulingAlgorithm.Interface.IInstance;

namespace OvenSchedulingAlgorithm.Objective.Implementation
{
    /// <summary>
    /// Calculate objective of a given solution and for given weights to an instance of the Oven Scheduling Problem.
    /// </summary>
    /// 
    public class CalculateObjective : ICalculateObjective
    {
        private readonly IInstance _instance;
        private readonly InstanceData _instanceData;
        private readonly IList<IBatchAssignment> _batchAssignments;
        private readonly IList<IBatch> _batches;
        private readonly IDictionary<(int mach, int pos), (int setupTime, int setupCost)> _setupDict;
        private IWeightObjective _weights;
        private int _alpha_Runtime;
        private int _beta_SetupTimes;
        private int _gamma_SetupCosts;
        private int _delta_Tardiness;

        /// <summary>
        /// Constructor from IOutput solution
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="solution"></param>
        /// <param name="weights"></param>
        public CalculateObjective(IInstance instance, IOutput solution, IWeightObjective weights)
        {
            _instance = instance;
            _instanceData = Preprocessor.DoPreprocessing(_instance, weights);
            _batchAssignments = solution.BatchAssignments;
            _batches = solution.GetBatches();
            _setupDict = Output.GetSetupTimesAndCostsDictionary(instance, solution.GetBatchDictionary());
            _weights = weights;
            _alpha_Runtime = weights.WeightRuntime;
            _beta_SetupTimes = weights.WeightSetupTimes;
            _gamma_SetupCosts = weights.WeightSetupCosts;
            _delta_Tardiness = weights.WeightTardiness;
        }

        /// <summary>
        /// Constructor from IOutput solution
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="solution"></param>
        /// <param name="weights"></param>
        public CalculateObjective(IInstance instance, IOutput solution, IAlgorithmConfig config)
        {
            _instance = instance;
            _instanceData = Preprocessor.DoPreprocessing(_instance, config.WeightsObjective);
            _batchAssignments = solution.BatchAssignments;
            _batches = solution.GetBatches();
            _setupDict = Output.GetSetupTimesAndCostsDictionary(instance, solution.GetBatchDictionary());
            _weights = config.WeightsObjective;
            _alpha_Runtime = _weights.WeightRuntime;
            _beta_SetupTimes = _weights.WeightSetupTimes;
            _gamma_SetupCosts = _weights.WeightSetupCosts;
            _delta_Tardiness = _weights.WeightTardiness;
        }

        /// <summary>
        /// Constructor from IOutput solution
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="solution"></param>
        /// <param name="setupDict"></param>
        /// <param name="config"></param>        
        public CalculateObjective(IInstance instance, IOutput solution,
            IDictionary<(int mach, int pos), (int setupTime, int setupCost)> setupDict,
            IAlgorithmConfig config)
        {
            _instance = instance;
            _instanceData = Preprocessor.DoPreprocessing(_instance, config.WeightsObjective);
            _batchAssignments = solution.BatchAssignments;
            _batches = solution.GetBatches();
            _setupDict = setupDict;
            _weights = config.WeightsObjective;
            _alpha_Runtime = _weights.WeightRuntime;
            _beta_SetupTimes = _weights.WeightSetupTimes;
            _gamma_SetupCosts = _weights.WeightSetupCosts;
            _delta_Tardiness = _weights.WeightTardiness;
        }

        /// <summary>
        /// Constructor from IOutput solution
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="solution"></param>
        /// <param name="weights"></param>
        public CalculateObjective(IInstance instance, IOutput solution, IWeightObjective weights, InstanceData instanceData)
        {
            _instance = instance;
            _instanceData = instanceData;
            _batchAssignments = solution.BatchAssignments;
            _batches = solution.GetBatches();
            _setupDict = Output.GetSetupTimesAndCostsDictionary(instance, solution.GetBatchDictionary());
            _weights = weights;
            _alpha_Runtime = weights.WeightRuntime;
            _beta_SetupTimes = weights.WeightSetupTimes;
            _gamma_SetupCosts = weights.WeightSetupCosts;
            _delta_Tardiness = weights.WeightTardiness;
        }


        /// <summary>
        /// From an instance and a solution to the oven scheduling problem as well as the weights used for optimization, 
        /// calculate the components of the objective and the real-valued objective value of the solution (value between 0 and 1).
        /// Note: times involved in objective components are in minutes (in order to be compatible with mzn calculations)
        /// </summary>
        /// <returns>The IObjectiveValue object associated with the solution and weights</returns>
        public IObjectiveComponents CalculateComponentsObjectiveReal()
        {
            //Note: times involved are in seconds (are converted to min later on)
            double totalRuntime = CalculateTotalProcessingTime();
            int totalSetupTimes = CalculateSetupTimesAndCosts().totalSetupTimes;
            int totalSetupCosts = CalculateSetupTimesAndCosts().totalSetupCosts;
            int finishedTooLate = CalculateLateJobs();
            //Note: times involved are in minutes
            double distanceEarliestStart = CalculateDistanceEarliestStart();
            double distanceLatestEnd = CalculateDistanceLatestEnd();         

            var unscheduledJobs = FindUnassignedJobs();     

            return CalculateObjectiveFromComponents(totalRuntime, totalSetupTimes, totalSetupCosts,  
                finishedTooLate,  distanceEarliestStart,  distanceLatestEnd, unscheduledJobs);
        }

        public IObjectiveComponents CalculateObjectiveFromComponents(double totalRuntimeSeconds, int totalSetupTimesSeconds, 
            int totalSetupCosts, int finishedTooLate, double distanceEarliestStart, double distanceLatestEnd, 
            IDictionary<int,IJob> unscheduledJobs)
        {
            int n = _instanceData.NumberOfJobs;
            //Note: av_processingTime is in minutes (instead of sec) in order to be compatible with minizinc where integer values are needed (at least for some solvers)
            int av_processingTime = (_instanceData.UpperBoundTotalRuntimeSeconds + 60 * n - 1) / (60 * n); //round up average minTime (in minutes) of jobs
            int max_setup_cost = Math.Max(_instanceData.MaxSetupCost, 1); //if max setup cost =0, we take 1 instead 
            int max_setup_time = Math.Max(_instanceData.MaxSetupTimeMinutes, 1); //max setup time in minutes (rouded up); if max setup time in minutes =0, we take 1 instead

            //normalized cost components are real numbers between 0 and n (for feasible solutions)
            //TODO? do normalization within the methods computing the cost components (for every job/ every batch)?
            double normalizedTotalRuntime = totalRuntimeSeconds / (60 * av_processingTime);
            double normalizedTotalSetupTimes = (double)totalSetupTimesSeconds / (60 * max_setup_time);
            double normalizedTotalSetupCosts = (double)totalSetupCosts / max_setup_cost;
            double normalizedFinishedTooLate = finishedTooLate;


            //objective is a real number between 0 and 1 (for feasible solutions)
            double objective =
                (_alpha_Runtime * normalizedTotalRuntime
                + _beta_SetupTimes * normalizedTotalSetupTimes
                + _gamma_SetupCosts * normalizedTotalSetupCosts
                + _delta_Tardiness * normalizedFinishedTooLate)
                / (n * (_alpha_Runtime + _beta_SetupTimes + _gamma_SetupCosts + _delta_Tardiness));

            long integerObjective = (int) (objective * _instanceData.UpperBoundForIntegerObjective);

            IObjectiveComponents objectiveValue = new ObjectiveComponents(
                totalRuntimeSeconds,
                _alpha_Runtime * normalizedTotalRuntime,
                totalSetupTimesSeconds,
                _beta_SetupTimes * normalizedTotalSetupTimes,
                totalSetupCosts,
                _gamma_SetupCosts * normalizedTotalSetupCosts,
                finishedTooLate,
                _delta_Tardiness * normalizedFinishedTooLate,
                unscheduledJobs.Count,
                unscheduledJobs,
                objective,
                integerObjective
                );

            return objectiveValue;
        }



        /// <summary>
        /// Calculate total runtime of ovens in seconds
        /// </summary>
        /// <returns>total runtime of ovens in seconds</returns>
        private double CalculateTotalProcessingTime()
        {
            double totalRuntime = 0;
            foreach (IBatch batch in _batches)
            {
                totalRuntime += batch.EndTime.Subtract(batch.StartTime).TotalSeconds;
            }

            return totalRuntime;
        }

        /// <summary>
        /// Calculate number of jobs that are finished too late
        /// </summary>
        /// <returns>Number of jobs that are finished too late</returns>
        private int CalculateLateJobs()
        {
            int finishedTooLate = 0;
            foreach (IBatchAssignment batchAssignment in _batchAssignments)
            {
                if (batchAssignment.AssignedBatch.EndTime > batchAssignment.Job.LatestEnd)
                {
                    finishedTooLate += 1;
                    //lateness += (int)batchAssignment.AssignedBatch.EndTime.Subtract(batchAssignment.Job.LatestEnd).TotalSeconds;
                }
            }

            return finishedTooLate;
        }

        /// <summary>
        /// Calculate total setup times (in seconds) and total setup costs
        /// </summary>
        /// <returns>(total setup time, total setup cost)</returns>
        private (int totalSetupTimes, int totalSetupCosts) CalculateSetupTimesAndCosts()
        {
            //total_setup_times and total_setup_costs
            int totalSetupTimes = 0;
            int totalSetupCosts = 0;

            var batchPos = _setupDict.Keys.ToList();

            for (int i = 0; i < batchPos.Count; i++)
            {
                totalSetupTimes += _setupDict[batchPos[i]].setupTime;
                totalSetupCosts += _setupDict[batchPos[i]].setupCost;
            }

            return (totalSetupTimes, totalSetupCosts);
        }
       
        /// <summary>
        /// Calculate the total quadratic distance to earliest start date (times involved are in minutes)
        /// </summary>
        /// <returns>Value of the objective component total quadratic distance to earliest start date (for times in minutes)</returns>
        private double CalculateDistanceEarliestStart()
        {
            // total quadratic distance to earliest start date = TDS 
            // = sum_{job in jobs} QuadraticDistanceToEarliestStartDate(job)
            // = sum_{job in jobs} ([CompletionTime(job) - (EarliestStartTime(job) + MinimalProcessingTime(job))]^2)

            //double quadraticDistanceToEarliestStartSeconds = 0;
            double quadraticDistanceToEarliestStartMinutes = 0;

            DateTime EarliestPossibleEndJob;
            TimeSpan distanceToEarliestStartJob;
            //double quadraticDistanceToEarliestStartJobInSeconds;
            double quadraticDistanceToEarliestStartJobInMinutes;


            foreach (IBatchAssignment batchAssignment in _batchAssignments)
            {
                //TODO: do normalization already here?
                //TODO calculate EarliestPossibleEndJob in preprocessing
                EarliestPossibleEndJob = batchAssignment.Job.EarliestStart
                    .AddSeconds(batchAssignment.Job.MinTime); 
                distanceToEarliestStartJob = batchAssignment.AssignedBatch.EndTime.Subtract(EarliestPossibleEndJob);
                //quadraticDistanceToEarliestStartJobInSeconds = distanceToEarliestStartJob.TotalSeconds * distanceToEarliestStartJob.TotalSeconds;
                quadraticDistanceToEarliestStartJobInMinutes = distanceToEarliestStartJob.TotalMinutes * distanceToEarliestStartJob.TotalMinutes;

                //quadraticDistanceToEarliestStartSeconds += quadraticDistanceToEarliestStartJobInSeconds;
                quadraticDistanceToEarliestStartMinutes += quadraticDistanceToEarliestStartJobInMinutes;
            }

            return quadraticDistanceToEarliestStartMinutes;
        }       



        /// <summary>
        /// Calculate the total quadratic distance to latest end date (times involved are in minutes)     
        /// </summary>
        /// <returns>Value of the objective component total quadratic distance to latest end date (times in minutes)</returns>
        private double CalculateDistanceLatestEnd()
        {
            // total quadratic distance to latest end date = TDE 
            // = sum_{job in jobs} QuadraticDistanceToLatestEndDate(job)
            // = sum_{job in jobs} (CompletionTime(job) - LatestEndTime(job) + e*)2,
            // where e* = d* - c*,
            // d* = max(job in jobs: LatestEndTime(job))
            // and c* = min(job in jobs: EarliestStart(job) + MinimalProcessingTime(job)

            double quadraticDistanceToLatestEnd = 0;

            TimeSpan distanceToLatestEndJob;
            double quadraticDistanceToLatestEndJobInMinutes;

            foreach (IBatchAssignment batchAssignment in _batchAssignments)
            {
                TimeSpan _constantEStar = _instanceData.ConstantEStarForTDE;

                //distanceToLatestEndJob = CompletionTime(job) - LatestEndTime(job) + e*
                distanceToLatestEndJob = batchAssignment.AssignedBatch.EndTime
                    .Subtract(batchAssignment.Job.LatestEnd)
                    .Add(_constantEStar);
                quadraticDistanceToLatestEndJobInMinutes = distanceToLatestEndJob.TotalMinutes * distanceToLatestEndJob.TotalMinutes;
                quadraticDistanceToLatestEnd += quadraticDistanceToLatestEndJobInMinutes;
            }

            return quadraticDistanceToLatestEnd;
        }


        /// <summary>
        /// Find all jobs of the instance that have not been assigned 
        /// or that were assigned beyond the scheduling horizon in the current list of batch assignments
        /// </summary>
        /// <returns>The dictionary of unassigned jobs (keys are job ids)</returns>
  
        public IDictionary<int, IJob> FindUnassignedJobs()
        {
            IDictionary<int, IJob> unassignedJobs = new Dictionary<int, IJob>();
            //initiliase dict
            foreach (IJob job in _instance.Jobs)
            {
                unassignedJobs[job.Id] = job;
            }
            //remove job from dictionary if there is a batch assignment for job in Output 
            //and batch start time is not after SchedulingHorizonEnd
            foreach (var assignment in _batchAssignments)
            {
                if (assignment.AssignedBatch.StartTime < _instance.SchedulingHorizonEnd)
                {
                    unassignedJobs.Remove(assignment.Job.Id);
                }
            }
            return unassignedJobs;
        }

    }
}
