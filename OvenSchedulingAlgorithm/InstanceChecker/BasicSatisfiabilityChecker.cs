using OvenSchedulingAlgorithm.Algorithm.SimpleGreedy;
using OvenSchedulingAlgorithm.Algorithm.SimpleGreedy.Implementation;
using OvenSchedulingAlgorithm.Interface;
using OvenSchedulingAlgorithm.Interface.Implementation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OvenSchedulingAlgorithm.InstanceChecker
{
    public class BasicSatisfiabilityChecker
    {
        /// <summary>
        /// Perform a basic satisfiability test on the given instance:
        /// check whether all individual jobs can be scheduled 
        /// and whether they can finish before their latest end date
        /// and write info about jobs that cannot be scheduled to file
        /// </summary>
        /// <param name="instance">the given instance</param>
        /// <param name="logFilePath">optional parameter: the given path of the logfile</param>
        /// <returns>true if instance succesfully passed the satisfiability test (instance could still be unsatisfiable as a whole),
        /// false if instance did not pass (unsatisfiability is guaranteed)</returns>
        public static (bool satisfiable, int tardyJobs) CheckSatisfiability(IInstance instance, string logFilePath = "")
        {
            Console.WriteLine("Checking satisfiability of instance.");

            bool passedSatisfiabilityTest = true;
            int tardyJobs = 0;

            ISimpleGreedyAlgorithm greedyAlgo = new SimpleGreedyAlgorithm();
            IAlgorithmConfig algorithmConfig = new AlgorithmConfig(0, false, "", new WeightObjective(1));
            string greedyInfo = "";

            foreach (IJob job in instance.Jobs)
            {                

                //create instance consisting of this job only
                IList<IJob> jobList = new List<IJob>
                {
                    job
                };
                IInstance singleJobInstance = new Instance("small instance", DateTime.Now, instance.Machines, 
                    instance.InitialStates, jobList, 
                    instance.Attributes, job.EarliestStart, instance.SchedulingHorizonEnd);

                IOutput greedySolution = greedyAlgo.RunSimpleGreedySingleJob(singleJobInstance);

                if (greedySolution.BatchAssignments.Count == 0)
                {
                    passedSatisfiabilityTest = false;
                    Console.WriteLine("Instance " + instance.Name + "is unsatisfiable. " +
                        "Job with Id " + job.Id + " cannot be scheduled \n");

                    //add info to string: current job cannot be scheduled
                    greedyInfo += "Job with Id " + job.Id + " cannot be scheduled \n";
                }
                else if (greedySolution.BatchAssignments[0].AssignedBatch.EndTime
                    > job.LatestEnd)
                {
                    tardyJobs += 1;
                    Console.WriteLine("Job with Id " + job.Id + " always finishes late \n");

                    //add info to string: current job cannot be scheduled
                    greedyInfo += "Job with Id " + job.Id + " always finishes late \n";
                }
            }

            if (passedSatisfiabilityTest)
            {
                Console.WriteLine("Basic satisfiability test passed. Scheduling of single jobs was successful for all jobs.");
                greedyInfo += "Scheduling of single jobs was successful for all jobs.";
            }

            //write info to file if logFilePath is not null or empty
            if (!string.IsNullOrEmpty(logFilePath))
            {
                File.WriteAllText(logFilePath, greedyInfo);
            }
            

            return (passedSatisfiabilityTest, tardyJobs);
        }
    }
}
