using OvenSchedulingAlgorithm.Algorithm;
using OvenSchedulingAlgorithm.Converter.Implementation;
using OvenSchedulingAlgorithm.Interface;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OvenSchedulingAlgorithm.InstanceChecker
{   
    public static class Preprocessor
    {

        public static InstanceData DoPreprocessing(IInstance instance, IWeightObjective weights)
        {
            //TODO later - check validity first, if false return invalid-preprocessed instance (need to make constructor)
            //bool passedValidityCheck = ValidityChecker.CheckValidity(instance);
            bool passedValidityCheck = false;

            //TODO test & compare with minizinc 
            //(check also difference with average min time in sec or minutes - values should be more precise and slightly smaller now)
            // this is not the case: we need to use values in minutes otherwise multiplicative constants will be larger by a factor of 60
            // write this up in documentation so that I will remember decision :)
            string name = instance.Name;
            DateTime creaTime = instance.CreationDate;
            int numberOfJobs = instance.Jobs.Count;
            int numberOfMachines = instance.Machines.Count;
            int numberOfAttributes = instance.Attributes.Count;
            TimeSpan lengthSchedulingHorizon = instance.SchedulingHorizonEnd.Subtract(instance.SchedulingHorizonStart);

            //create dictionaries of setup times and costs
            List<int> sortedAttributeIDs = instance.Attributes.Keys.ToList();
            sortedAttributeIDs.Sort();
            IDictionary<(int, int), int> setupTimeDictionary = new Dictionary<(int, int), int>();
            IDictionary<(int, int), int> setupCostDictionary = new Dictionary<(int, int), int>();
            foreach (int attributeID1 in sortedAttributeIDs)
            {
                int i = 0;
                foreach (int attributeID2 in sortedAttributeIDs)
                {
                    setupTimeDictionary.Add((attributeID1, attributeID2), instance.Attributes[attributeID1].SetupTimesAttribute[i]);
                    setupCostDictionary.Add((attributeID1, attributeID2), instance.Attributes[attributeID1].SetupCostsAttribute[i]);
                    i++;
                }
            }

            int upperBoundTotalRuntimeSeconds = instance.Jobs.Select(x => x.MinTime).Sum();
            //upper bound for the total runtime of ovens if unit of time is minutes 
            //(ie all min processing times rounded up to next minute)
            int upperBoundTotalRuntimeMinutes = instance.Jobs.Select(x => (x.MinTime + 59) / 60).Sum();

            int minMinTime = instance.Jobs.Select(x => x.MinTime).Min();
            int minMinTimeMinutes = (minMinTime + 59) / 60; 
            int maxMinTime = instance.Jobs.Select(x => x.MinTime).Max();
            int maxMinTimeMinutes = (maxMinTime + 59) / 60;
            DateTime minEarliestStart = CalculateMinimalEarliestStart(instance);
            DateTime minimalLatestEnd = CalculateMinimalLatestEnd(instance);
            DateTime maximalEarliestStart = CalculateMaximalEarliestStart(instance);
            TimeSpan constantEStar = CalculateConstantDistanceLatestEnd(instance);
            int maxSetupCost = instance.Attributes.Values
                .Select(x => x.SetupCostsAttribute.Max()) //maximum setup cost per attribute
                .Max(); //overall maximum setup cost
            int maxSetupTime = instance.Attributes.Values
                .Select(x => x.SetupTimesAttribute.Max()) //maximum setup time per attribute
                .Max(); //overall maximum setup time
            int maxSetupTimeMinutes = (maxSetupTime + 59) / 60; // max setup time in minutes, rounded up
            int maxNumberOfAvailabilityIntervals = instance.Machines.Values.Max(x => x.AvailabilityStart.Count);
            //average minimal processing time (rounded up, in minutes because minizinc uses minutes as unit)
            int averageMinTime = (upperBoundTotalRuntimeMinutes + numberOfJobs - 1) / ( numberOfJobs); //round up result to next integer
            double upperBoundTDSJob = lengthSchedulingHorizon.TotalMinutes - minMinTimeMinutes
                - minEarliestStart.Subtract(instance.SchedulingHorizonStart).TotalMinutes;
            double upperBoundTDEJob = constantEStar.Add(lengthSchedulingHorizon).TotalMinutes
                - minimalLatestEnd.Subtract(instance.SchedulingHorizonStart).TotalMinutes;


            //bounds and multiplicative constants needed for integer objective value
            //calculate least common multiple of average minimal processing time, max setup time and max setup cost
            long lcm = LCM(averageMinTime, Math.Max(maxSetupCost, 1), Math.Max(maxSetupTimeMinutes, 1));
            long upperBoundForIntegerObjective = lcm * numberOfJobs *
                (weights.WeightRuntime + weights.WeightSetupTimes + weights.WeightSetupCosts + weights.WeightTardiness);
            long multFactorTotalRuntime = weights.WeightRuntime * lcm / averageMinTime;
            long multFactorFinishedTooLate = weights.WeightTardiness * lcm;
            long multFactorTotalSetupTimes = weights.WeightSetupTimes * lcm / Math.Max(maxSetupTimeMinutes, 1);
            long multFactorTotalSetupCosts = weights.WeightSetupCosts * lcm / Math.Max(maxSetupCost, 1);

            var satisfiabilityCheck = BasicSatisfiabilityChecker.CheckSatisfiability(instance
                //, "./satCheck-" + instance.Name.Replace(':', '-') + ".txt"
                //no need to save info to file
                );
            int numberOfJobsAlwaysTardy = satisfiabilityCheck.tardyJobs;

            return new InstanceData(
                name,
                creaTime,
                numberOfJobs,
                numberOfMachines,            
                numberOfAttributes,
                setupTimeDictionary,
                setupCostDictionary,
                lengthSchedulingHorizon,
                upperBoundTotalRuntimeSeconds,
                upperBoundTotalRuntimeMinutes,
                minMinTime,
                minMinTimeMinutes,
                maxMinTime,
                maxMinTimeMinutes,
                minEarliestStart,
                minimalLatestEnd,
                maximalEarliestStart,
                maxSetupCost,
                maxSetupTime,
                maxSetupTimeMinutes,
                maxNumberOfAvailabilityIntervals,
                upperBoundTDSJob,
                constantEStar,
                upperBoundTDEJob,
                upperBoundForIntegerObjective,
                multFactorTotalRuntime,
                multFactorFinishedTooLate,
                multFactorTotalSetupTimes,
                multFactorTotalSetupCosts,
                passedValidityCheck,
                satisfiabilityCheck.satisfiable,
                numberOfJobsAlwaysTardy
                );

        }

        

        /// <summary>
        /// Use Euclid's algorithm to calculate the greatest common divisor (GCD) of two numbers.
        /// </summary>
        /// <param name="a">the first number</param>
        /// <param name="b">the second number</param>
        /// <returns>GCD(a,b)</returns>
        private static long GCD(long a, long b)
        {
            if (a % b == 0)
            {
                return b;
            }
            return GCD(b, a % b);
        }

        /// <summary>
        /// Return the least common multiple (LCM) of two numbers.
        /// </summary>
        /// <param name="a">the first number</param>
        /// <param name="b">the second number</param>
        /// <returns>LCM(a,b)</returns>
        // 
        private static long LCM(long a, long b)
        {
            return a * b / GCD(a, b);
        }

        /// <summary>
        /// Return the least common multiple (LCM) of three numbers.
        /// </summary>
        /// <param name="a">the first number</param>
        /// <param name="b">the second number</param>
        /// <param name="c">the third number</param>
        /// <returns>LCM(a,b,c)</returns>
        // 
        private static long LCM(long a, long b, long c)
        {
            return LCM(a, LCM(b,c));
        }

        /// <summary>
        /// Calculate the minimal (=earliest) earliest start date of the instance     
        /// </summary>
        /// <returns>DateTime-value of the minimal earliest start date</returns>
        private static DateTime CalculateMinimalEarliestStart(IInstance instance)
        {
            //initialise with values of first job
            DateTime minEarliestStart = instance.Jobs[0].EarliestStart;
            foreach (IJob job in instance.Jobs)
            {
                if (job.EarliestStart < minEarliestStart)
                {
                    minEarliestStart = job.EarliestStart;
                }
            }
            return minEarliestStart;
        }

        /// <summary>
        /// Calculate the minimal (=earliest) latest end date of the instance     
        /// </summary>
        /// <returns>DateTime-value of the minimal latest end date</returns>
        private static DateTime CalculateMinimalLatestEnd(IInstance instance)
        {
            //initialise with values of first job
            DateTime minLatestEnd = instance.Jobs[0].LatestEnd;
            foreach (IJob job in instance.Jobs)
            {
                if (job.LatestEnd < minLatestEnd)
                {
                    minLatestEnd = job.LatestEnd;
                }
            }
            return minLatestEnd;
        }

        /// <summary>
        /// Calculate the maximal (=latest) earliest start date of the instance     
        /// </summary>
        /// <returns>DateTime-value of the maximal earliest start date</returns>
        private static DateTime CalculateMaximalEarliestStart(IInstance instance)
        {
            //initialise with values of first job
            DateTime maxEarliestStart = instance.Jobs[0].EarliestStart;
            foreach (IJob job in instance.Jobs)
            {
                if (job.EarliestStart > maxEarliestStart)
                {
                    maxEarliestStart = job.EarliestStart;
                }
            }
            return maxEarliestStart;
        }

        /// <summary>
        /// Calculate the TimeSPan value of the constant eStar needed for the quadratic distance to latest end date. 
        /// </summary>
        /// <returns>TimeSpan value of the the constant eStar</returns>
        private static TimeSpan CalculateConstantDistanceLatestEnd(IInstance instance)
        {
            // e * = d * -c *,
            // d* = max(job in jobs: LatestEndTime(job))
            // and c* = min(job in jobs: EarliestStart(job) + MinimalProcessingTime(job)

            //calculation of d* and c*
            //initialise with values of first job
            //Note: no need to clone SchedulingHorizonStart:
            //DateTime is a value type, so when you assign it you also clone it.

            //constant d*
            DateTime maxLatestEnd = instance.Jobs[0].LatestEnd;
            //constant c*
            DateTime minEarliestEndPossible = instance.Jobs[0].EarliestStart.AddSeconds(instance.Jobs[0].MinTime);

            foreach (IJob job in instance.Jobs)
            {
                if (job.LatestEnd > maxLatestEnd)
                {
                    maxLatestEnd = job.LatestEnd;
                }

                if (job.EarliestStart.AddSeconds(job.MinTime) < minEarliestEndPossible)
                {
                    minEarliestEndPossible = job.EarliestStart.AddSeconds(job.MinTime);
                }
            }
            TimeSpan constantEStar = maxLatestEnd.Subtract(minEarliestEndPossible);

            return constantEStar;
        }        
       
    }
}
