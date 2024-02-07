using OvenSchedulingAlgorithm.Algorithm;
using OvenSchedulingAlgorithm.Interface;
using OvenSchedulingAlgorithm.Interface.Implementation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace OvenSchedulingAlgorithm.Objective.Implementation
{
    /// <summary>
    /// Checks the validity of a solution to the oven scheduling problem and counts the number of hard constraints violations (+number of unscheduled jobs) 
    /// </summary>
    public class ViolationCounter
    {
        private readonly IInstance _instance;
        private readonly IList<IBatchAssignment> _batchAssignments;
        private readonly IDictionary<(int mach, int pos), IBatch> _batchDictionary;
        private readonly IDictionary<(int mach, int pos), (int setupTime, int setupCost)> _setupDict;
        private readonly string _logfilename;
        private int _totalViolations;
        private int _jobsScheduledOutsideHorizon;
        private string _logfiledata;
        private IAlgorithmConfig _config;

        /// <summary>
        /// Constructor from IOutput solution
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="solution"></param>
        /// <param name="logfilename"></param>
        /// <param name="config"></param>
        public ViolationCounter(IInstance instance, IOutput solution, string logfilename, IAlgorithmConfig config)
        {
            _instance = instance;
            _batchAssignments = solution.BatchAssignments;
            _batchDictionary = solution.GetBatchDictionary();
            _setupDict = Output.GetSetupTimesAndCostsDictionary(instance, _batchDictionary);
            _logfilename = logfilename;
            _logfiledata = "";
            _config = config;
        }

        /// <summary>
        /// Constructor from IOutput solution
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="solution"></param>
        /// <param name="batchDictionary"></param>
        /// <param name="setupDict"></param>
        /// <param name="logfilename"></param>
        /// <param name="config"></param>
        public ViolationCounter(IInstance instance, IOutput solution,
            IDictionary<(int mach, int pos), IBatch> batchDictionary,
            IDictionary<(int mach, int pos), (int setupTime, int setupCost)> setupDict,
            string logfilename, IAlgorithmConfig config)
        {
            _instance = instance;
            _batchAssignments = solution.BatchAssignments;
            _batchDictionary = batchDictionary;
            _setupDict = setupDict;
            _logfilename = logfilename;
            _logfiledata = "";
            _config = config;
        }


        /// <summary>
        /// Determine the number of hard constraint violations
        /// </summary>
        public (int violations, int jobsScheduledOutsideOrUnscheduled) ValidateSolution(bool writeInfoToConsole)
        {
            _totalViolations = 0;
            _jobsScheduledOutsideHorizon = 0;

            // count jobs that are unscheduled or scheduled outside the scheduling horizon
            CountJobsUnscheduledOrScheduledOutsideHorizon();
            
            // check whether jobs start after their earliest start date 
            CheckEarliestStart(writeInfoToConsole);

            // check whether processing times lie between min and max times
            CheckProcessingTime(writeInfoToConsole);

            // check whether machine capacity is not exceeded
            CheckMachineCapacity(writeInfoToConsole);

            //check whether jobs in the same batch have matching attributes
            CheckMatchingAttributes(writeInfoToConsole);

            //check that setup times are fulfilled between consecutive batches
            CheckSetupTimes(writeInfoToConsole);

            //check that the assigned machines are eligible
            CheckEligibleMachines(writeInfoToConsole);

            //check that the assigned machines are available
            CheckMachineAvailability(writeInfoToConsole);

            _logfiledata += "violations: " + _totalViolations + ";\n";
            _logfiledata += "number of jobs that are unscheduled or scheduled outside the scheduling horizon: " + _jobsScheduledOutsideHorizon + ";\n";

            if (_totalViolations == 0 & _jobsScheduledOutsideHorizon == 0)
            {
                _logfiledata += "feasible: " + 1 + ";\n";
            }
            else if (_totalViolations == 0)
            {
                _logfiledata += "feasible partial solution: " + 1 + ";\n";
            }
            else
            {
                _logfiledata += "feasible: " + 0 + ";\n";
            }

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

            return (_totalViolations, _jobsScheduledOutsideHorizon) ;
        }        

        /// <summary>
        /// Count the number of jobs that are unscheduled or scheduled outside the scheduling horizon
        /// </summary>
        private void CountJobsUnscheduledOrScheduledOutsideHorizon()
        {
            int n = _instance.Jobs.Count;
            int assignments = _batchAssignments.Count;

            int unscheduledJobs = n - assignments;
            int jobsOutsideHorizon = CountJobsAssignedOutsideHorizonCost();

            _jobsScheduledOutsideHorizon += unscheduledJobs + jobsOutsideHorizon;

        }

        /// <summary>
        /// Count the number of jobs that are scheduled outside the scheduling horizon
        /// </summary>
        /// <returns>Number of jobs that are scheduled outside the scheduling horizon in the solution</returns>
        public int CountJobsAssignedOutsideHorizonCost()
        {
            int jobsOutsideHorizon = 0;

            foreach (var batchAssignment in _batchAssignments)
            {
                if (batchAssignment.AssignedBatch.StartTime > _instance.SchedulingHorizonEnd)
                {
                    jobsOutsideHorizon += 1;
                }
            }

            return jobsOutsideHorizon;
        }

        private void CheckEarliestStart(bool writeInfoToConsole)
        {
            foreach (IBatchAssignment batchAssignment in _batchAssignments)
            {
                DateTime jobEarliestStart = batchAssignment.Job.EarliestStart;
                long jobTicks = jobEarliestStart.Ticks;
                DateTime batchStart = batchAssignment.AssignedBatch.StartTime;
                long batchTicks = batchStart.Ticks;
                
                if (jobTicks > batchTicks)
                {
                    if (writeInfoToConsole)
                    {
                        Console.WriteLine("Batch started too early for job {0} (batch start: {2}, earliest job start: {1})", batchAssignment.Job.Id, jobEarliestStart, batchStart);
                    }

                    _totalViolations += 1;
                }
            }
        }

        private void CheckProcessingTime(bool writeInfoToConsole)
        {
            foreach (IBatchAssignment batchAssignment in _batchAssignments.Where(a => a.AssignedBatch.StartTime <= _instance.SchedulingHorizonEnd))
            {
                int jobMinDuration = batchAssignment.Job.MinTime;
                int jobMaxDuration = batchAssignment.Job.MaxTime;
                int batchDuration = (int)batchAssignment.AssignedBatch.EndTime.Subtract(batchAssignment.AssignedBatch.StartTime).TotalSeconds;

                if (jobMinDuration > batchDuration)
                {
                    if (writeInfoToConsole)
                    {
                        Console.WriteLine("Batch duration too short for job {0} (batch duration: {2}, min job duration: {1})", batchAssignment.Job.Id, jobMinDuration, batchDuration);
                    }

                    _totalViolations += 1;
                }

                if (jobMaxDuration < batchDuration)
                {
                    if (writeInfoToConsole)
                    {
                        Console.WriteLine("Batch duration too long for job {0} (batch duration: {2}, max job duration: {1})", batchAssignment.Job.Id, jobMaxDuration, batchDuration);
                    }
                        

                    _totalViolations += 1;
                }
            }
        }

        private void CheckMachineCapacity(bool writeInfoToConsole)
        {
            int n = _instance.Jobs.Count; //number of jobs
            int m = _instance.Machines.Keys.Max();
            int[] batchSize = new int[n*m];
            

            foreach (IBatchAssignment batchAssignment in _batchAssignments.Where(a => a.AssignedBatch.StartTime <= _instance.SchedulingHorizonEnd))
            {
                batchSize[batchAssignment.AssignedBatch.Id-1+n*(batchAssignment.AssignedBatch.AssignedMachine.Id-1)] 
                    += batchAssignment.Job.Size;
            }

            foreach (IBatch batch in _batchDictionary.Values
                .Where(b => b.StartTime <= _instance.SchedulingHorizonEnd))
            {   
                if (batchSize[batch.Id-1+ n *(batch.AssignedMachine.Id - 1)] 
                    > batch.AssignedMachine.MaxCap)
                {
                    if (writeInfoToConsole)
                    {
                        Console.WriteLine("Batch {0} too large for machine {1}", batch.Id, batch.AssignedMachine.Id);
                    }
                    

                    _totalViolations += 1;

                }
            }
        }

        private void CheckMatchingAttributes(bool writeInfoToConsole)
        {
            foreach (IBatchAssignment batchAssignment in _batchAssignments.Where(a => a.AssignedBatch.StartTime <= _instance.SchedulingHorizonEnd))
            {
                int machineId = batchAssignment.AssignedBatch.AssignedMachine.Id;

                if (batchAssignment.Job.AttributeIdPerMachine.ContainsKey(machineId) //check if dictionary contains key first
                    &&
                    batchAssignment.AssignedBatch.Attribute.Id != batchAssignment.Job.AttributeIdPerMachine[machineId])
                {
                    if (writeInfoToConsole)
                    {
                        Console.WriteLine("Attribute of job {0} and batch {1} on machine {2} do not match", batchAssignment.Job.Id, batchAssignment.AssignedBatch.Id, machineId);
                    }
                    

                    _totalViolations += 1;
                }
            }
        }

        private void CheckSetupTimes(bool writeInfoToConsole)
        {

            foreach (int machine in _instance.Machines.Keys)
            {
                int batchOnMachineCount = _batchDictionary.Keys.Where(k => k.mach == machine).Count();

                if (batchOnMachineCount == 0)
                {
                    continue;
                }

                for (int batchPos = 1; batchPos <= batchOnMachineCount; batchPos++)
                {
                    DateTime batchStart = _batchDictionary[(machine, batchPos)].StartTime;
                    int setupTimeBeforeBatch = _setupDict[(machine, batchPos)].setupTime;

                    //check whether there is enough time in the same machine availability interval
                    //for setup before batches

                    //shift for batch
                    IMachine assignedMachine = _instance.Machines[machine];
                    // batch starts in shift s:
                    // this is the maximal shift for which the start time of the batch is >= the start time of the shift
                    int s = assignedMachine.AvailabilityStart.Select((v, i) => new { v, i })
                        .Where(x => x.v <= batchStart)
                        .Select(x => x.i).Max();

                    //check if setup before batch lies in same shift as batch
                    if (batchStart.AddSeconds(-setupTimeBeforeBatch) 
                        < assignedMachine.AvailabilityStart[s])
                    {
                        if (writeInfoToConsole)
                        {
                            Console.WriteLine("Setup before batch {1} on machine {0} is not " +
                                "in same machine availability interval as batch", machine, 
                                _batchDictionary[(machine, batchPos)].Id);
                        }

                        _totalViolations += 1;
                    }


                    //check if batch times do not overlap and setup times are considered
                    //(checking times between current batch and previous batch)
                    if (batchPos > 1)
                    {
                        DateTime previousBatchEnd = _batchDictionary[(machine, batchPos - 1)].EndTime;

                        if (previousBatchEnd.AddSeconds(setupTimeBeforeBatch) > batchStart &&
                        batchStart < _instance.SchedulingHorizonEnd)
                        {
                            if (writeInfoToConsole)
                            {
                                Console.WriteLine("Batches {0} and {1} on machine {2} overlap " +
                                    "(or do not leave enough time for setup)",
                                    _batchDictionary[(machine, batchPos - 1)].Id,
                                    _batchDictionary[(machine, batchPos)].Id,
                                    machine);
                            }

                            _totalViolations += 1;
                        }
                    }                              
                    
                }                
                
            }        

        }

        private void CheckEligibleMachines(bool writeInfoToConsole)
        {
            foreach (IBatchAssignment batchAssignment in _batchAssignments)
            {
                int assignedMachineId = batchAssignment.AssignedBatch.AssignedMachine.Id; 
                if (!batchAssignment.Job.EligibleMachines.Contains(assignedMachineId)) 
                {
                    if (writeInfoToConsole)
                    {
                        Console.WriteLine("Job {0} scheduled in batch {1} on machine {2} " +
                            "but machine not eligible", batchAssignment.Job.Id, batchAssignment.AssignedBatch.Id, batchAssignment.AssignedBatch.AssignedMachine.Id);
                    }                    

                    _totalViolations += 1;
                }
            }
        }

        private void CheckMachineAvailability(bool writeInfoToConsole)
        {
            foreach (IBatchAssignment batchAssignment in _batchAssignments)
            {
                IMachine assignedMachine = batchAssignment.AssignedBatch.AssignedMachine;
                // batch starts in shift i:
                // this is the maximal shift for which the start time of the batch is >= the start time of the shift
                int i = assignedMachine.AvailabilityStart.Select((v, i) => new { v, i })
                    .Where(x => x.v <= batchAssignment.AssignedBatch.StartTime)
                    .Select(x => x.i)
                    .DefaultIfEmpty(-1).Max();

                //TODO now: adapt 
                //if jobs are allowed to be assigned after the scheduling horizon end, this is not quite correct 
                //(message says that batch starts in last shift, but actually batch starts after that shift)

                if (i == -1)//batch not assigned to a machine shift, ie, starts before first shift
                {
                    if (writeInfoToConsole)
                    {
                        Console.WriteLine("Batch {0} on machine {1} starts before " +
                            "first availability interval of machine", batchAssignment.AssignedBatch.Id, 
                            batchAssignment.AssignedBatch.AssignedMachine.Id);
                    }


                    _totalViolations += 1;
                }
                else if (batchAssignment.AssignedBatch.EndTime > assignedMachine.AvailabilityEnd[i])
                {
                    if (writeInfoToConsole)
                    {
                        Console.WriteLine("Batch {0} on machine {1} starts but does not end " +
                            "in {2}-th machine availability interval", batchAssignment.AssignedBatch.Id, 
                            batchAssignment.AssignedBatch.AssignedMachine.Id, i + 1);
                    }
                    

                    _totalViolations += 1;
                }                
            }
        }

    }
}