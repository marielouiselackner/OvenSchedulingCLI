using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OvenSchedulingAlgorithm.Algorithm;
using OvenSchedulingAlgorithm.InstanceChecker;
using OvenSchedulingAlgorithm.Interface;
using OvenSchedulingAlgorithm.Interface.Implementation;
using Attribute = OvenSchedulingAlgorithm.Interface.Implementation.Attribute;

namespace OvenSchedulingAlgorithm.Converter.Implementation
{
    /// <summary>
    /// Converter that creates MiniZinc instance files from a given instance information
    /// and converts back MiniZinc solution files to solutions of the original problem.
    /// (Note that MiniZinc uses times in minutes rather than in seconds)
    /// </summary>
    //internal class MiniZincConverter : IMiniZincConverter
    public class MiniZincConverter : IMiniZincConverter
    {
        // Constants for MiniZinc identifier names
        private const string L = "l=";
        private const string A = "a=";
        private const string SETUP_COSTS = "setup_costs=[";
        private const string SETUP = "setup=[";
        private const string SETUP_COST_LINE = "|";
        private const string SETUP_COSTS_END = "|]";
        private const string SETUP_TIMES = "setup_times=[";
        private const string SETUP_TIMES_LINE = "|";
        private const string SETUP_TIMES_END = "|]";
        private const string M = "m=";
        private const string MIN_CAP = "min_cap=[";
        private const string MIN_CAP_END = "]";
        private const string MAX_CAP = "max_cap=[";
        private const string MAX_CAP_END = "]";
        private const string INIT_STATE = "initState=[";
        private const string INIT_STATE_END = "]";
        private const string S = "s=";
        private const string MACHINE_AVAILABILITY_START = "m_a_s = [";
        private const string MACHINE_AVAILABILITY_LINE = "|";
        private const string MACHINE_AVAILABILITY_START_END = "|]";
        private const string MACHINE_AVAILABILITY_END = "m_a_e = [";
        private const string MACHINE_AVAILABILITY_END_END = "|]";
        private const string B = "b=";
        private const string N = "n=";
        private const string ELIGIBLE_MACHINES = "eligible_machine = [";
        private const string ELIGIBLE_MACHINES_END = "]";
        private const string ELIGIBLE_MACHINES_LINE = "{";
        private const string ELIGIBLE_MACHINES_LINE_END = "},";
        private const string EARLIEST_START = "earliest_start=[";
        private const string EARLIEST_START_END = "]";
        private const string LATEST_END = "latest_end=[";
        private const string LATEST_END_END = "]";
        private const string MIN_TIME = "min_time=[";
        private const string MIN_TIME_END = "]";
        private const string MAX_TIME = "max_time=[";
        private const string MAX_TIME_END = "]";
        private const string SIZE = "size=[";
        private const string SIZE_END = "]";
        private const string JOB_ATTRIBUTE = "attribute=[";
        private const string JOB_ATTRIBUTE_PER_MACHINE_LINE = "|";
        private const string ATTRIBUTE_END = "]";
        private const string MZN_LINE_END = ";";
        private const string UPPER_BOUND_INTEGER_OBJECTIVE = "upper_bound_integer_objective=";
        private const string MULT_FACTOR_TOTAL_RUNTIME = "mult_factor_total_runtime=";
        private const string MULT_FACTOR_FINISHED_TOOLATE = "mult_factor_finished_toolate=";
        private const string MULT_FACTOR_TOTAL_SETUPTIMES = "mult_factor_total_setuptimes=";
        private const string MULT_FACTOR_TOTAL_SETUPCOSTS = "mult_factor_total_setupcosts=";
        private const string UPPER_BOUND_TOTAL_RUNTIME = "running_time_bound=";
        private const string MIN_DURATION = "min_duration=";
        private const string MAX_DURATION = "max_duration=";
        private const string MAX_SETUP_TIME = "max_setup_time=";
        private const string MAX_SETUP_COST = "max_setup_cost=";
        private const string MULT_FACTOR_TDE = "mult_factor_TDE=";
        private const string MULT_FACTOR_TDS  = "mult_factor_TDS=";
        private const string CONSTANT_FOR_TDE = "constant_for_TDE=";
        private const string TDE_BOUND = "TDE_bound=";
        private const string TDS_BOUND = "TDS_bound=";

        // Constants for CP Optimizer identifier names
        private const string cpL = "LengthSchedulingHorizon=";
        private const string cpA = "nAttributes=";
        private const string cpSETUP_COSTS = "SetupCosts=[";
        private const string cpSETUP_COST_LINE = "[";
        private const string cpSETUP_COSTS_END = "]]";
        private const string cpSETUP_TIMES = "SetupTimes=[";
        private const string cpSETUP_TIMES_LINE = "[";
        private const string cpSETUP_LINE_END = "],";
        private const string cpSETUP_TIMES_END = "]]";
        private const string cpM = "nMachines=";
        private const string cpMIN_CAP = "MinCap=[";
        private const string cpMIN_CAP_END = "]";
        private const string cpMAX_CAP = "MaxCap=[";
        private const string cpMAX_CAP_END = "]";
        private const string cpS = "nShifts=";
        private const string cpMACHINE_AVAILABILITY_START = "ShiftStartTimes = [";
        private const string cpMACHINE_AVAILABILITY_LINE = "[";
        private const string cpMACHINE_AVAILABILITY_LINE_END = "],";
        private const string cpMACHINE_AVAILABILITY_START_END = "]]";
        private const string cpMACHINE_AVAILABILITY_END = "ShiftEndTimes = [";
        private const string cpMACHINE_AVAILABILITY_END_END = "]]";
        private const string cpN = "nJobs=";
        private const string cpELIGIBLE_MACHINES = "EligibleMachines = [";
        private const string cpELIGIBLE_MACHINES_END = "]";
        private const string cpELIGIBLE_MACHINES_LINE = "{";
        private const string cpELIGIBLE_MACHINES_LINE_END = "},";
        private const string cpEARLIEST_START = "EarliestStart=[";
        private const string cpEARLIEST_START_END = "]";
        private const string cpLATEST_END = "LatestEnd=[";
        private const string cpLATEST_END_END = "]";
        private const string cpMIN_TIME = "MinTime=[";
        private const string cpMIN_TIME_END = "]";
        private const string cpMAX_TIME = "MaxTime=[";
        private const string cpMAX_TIME_END = "]";
        private const string cpSIZE = "JobSize=[";
        private const string cpSIZE_END = "]";
        private const string cpJOB_ATTRIBUTE = "Attribute=[";
        private const string cpATTRIBUTE_END = "]";
        private const string cp_LINE_END = ";";

        /// <summary>
        /// Takes a dictionary of machines and returns a function that converts machine IDs to corresponding machine IDs in MiniZinc
        /// (for MiniZinc instance, machines need to be numbered from 1 to m,
        /// general Ids are distinct integers in an arbitrary range.
        /// therefore: machine with smallest Id gets number 1, machine with next smallest number 2 etc.)
        /// </summary>
        /// <param name="machines">The dictionary of machines for which conversion function is created</param>
        /// <returns>Return the function that converts Machine Ids to Minizinc Ids </returns>
        public Func<int,int> ConvertMachineIdToMinizinc(IDictionary<int, IMachine> machines)
        {
            //get list of sorted dictionary keys (ie machine Ids)
            List<int> sortedMachines = machines.Keys.ToList();
            sortedMachines.Sort();            

            return x => sortedMachines.IndexOf(x)+1; 
        }

        /// <summary>
        /// Takes a dictionary of machines and returns a function that converts MiniZinc machine IDs to corresponding original machine IDs
        /// (for MiniZinc instance, machines are numbered from 1 to m, 
        /// general Ids are distinct integers in an arbitrary range)
        /// </summary>
        /// <param name="machines">The dictionary of machines for which conversion function is created</param>
        /// <returns>Return the function that converts Minizinc Ids to Machine Ids </returns>
        public Func<int, int> ConvertMinizincMachineIdToId(IDictionary<int, IMachine> machines)
        {
            //get list of sorted dictionary keys (ie machine Ids)
            List<int> sortedMachines = machines.Keys.ToList();
            sortedMachines.Sort();

            return x => sortedMachines[x-1];
        }

        /// <summary>
        /// Takes a list of jobs and returns a function that converts job IDs to corresponding job IDs in MiniZinc
        /// (for MiniZinc instance, jobs need to be numbered from 1 to n,
        /// general Ids are distinct integers in an arbitrary range.
        /// therefore: job with smallest Id gets number 1, job with next smallest number 2 etc.)
        /// </summary>
        /// <param name="jobs">The list of jobs for which conversion function is created</param>
        /// <returns>Return the function that converts Job Ids to Minizinc Ids </returns>
        private Func<int, int> ConvertJobIdToMinizinc(IList<IJob> jobs)
        {
            //get list of sorted job Ids
            List<int> sortedJobs = new List<int>();
            foreach (Job job in jobs)
            {
                sortedJobs.Add(job.Id);
            }
            sortedJobs.Sort();

            return x => sortedJobs.IndexOf(x) + 1;
        }


        /// <summary>
        /// Takes a dictionary of attributes and returns a function that converts attribute IDs to corresponding attribute IDs in MiniZinc
        /// (for MiniZinc instance, attributes need to be numbered from 1 to a,
        /// general Ids are distinct integers in an arbitrary range.
        /// therefore: attribute with smallest Id gets number 1, attribute with next smallest number 2 etc.)
        /// </summary>
        /// <param name="attributes">The dictionary of attributes for which conversion function is created</param>
        /// <returns>Return the function that converts Attribute Ids to Minizinc Ids </returns>
        public Func<int, int> ConvertAttributeIdToMinizinc(IDictionary<int, IAttribute> attributes)
        {
            //get list of sorted dictionary keys (ie attribute Ids)
            List<int> sortedAttributes = attributes.Keys.ToList();
            sortedAttributes.Sort();

            return x => sortedAttributes.IndexOf(x) + 1;
        }

        /// <summary>
        /// Takes a dictionary of attributes and returns a function that converts MiniZinc attributes IDs to corresponding original attribute IDs
        /// (for MiniZinc instance, attributes are numbered from 1 to a, 
        /// general Ids are distinct integers in an arbitrary range)
        /// </summary>
        /// <param name="attributes">The dictionary of attributes for which conversion function is created</param>
        /// <returns>Return the function that converts Minizinc Ids to Attribute Ids </returns>
        private static Func<int, int> ConvertMinizincAttributeIdToId(IDictionary<int, IAttribute> attributes)
        {
            //get list of sorted dictionary keys (ie machine Ids)
            List<int> sortedAttributes = attributes.Keys.ToList();
            sortedAttributes.Sort();

            return x => sortedAttributes[x - 1];
        }

        /// <summary>
        /// Drops the milliseconds part of a TimeSpan
        /// </summary>
        /// <param name="timespan">timespan from which milliseconds should be dropped</param>
        /// <returns>timespan without milliseconds</returns>
        private static TimeSpan DropMillisecondsTimespan(TimeSpan timespan)
        {
            // milliseconds are ignored (this is achieved by an integer division and multiplication by the number of ticks per second)
            timespan = new TimeSpan(timespan.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond);

            return timespan;
        }

        /// <summary>
        /// Drops the milliseconds part of a DateTime
        /// </summary>
        /// <param name="datetime">datetime from which milliseconds should be dropped</param>
        /// <returns>datetime without milliseconds</returns>
        private static DateTime DropMillisecondsDatetime(DateTime datetime)
        {
            // milliseconds are ignored (this is achieved by an integer division and multiplication by the number of ticks per second)
            datetime = new DateTime(datetime.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond);

            return datetime;
        }

        /// <summary>
        /// Convert time in seconds to time in minutes and round up (if not intgral)
        /// </summary>
        /// <param name="sec">time in seconds</param>
        /// <returns>time in entire minutes</returns>
        private static int RoundUpFromSecondsToMinutes(int sec)
        {
            int eps = 0;
            if (sec % 60 != 0)
            {
                eps += 1;
            }
            // in case sec/60 is not an integer (ie not entire minutes), it is rounded up
            int min = sec / 60 + eps;

            return min;
        }

        /// <summary>
        /// From the list of jobs and the dictionary of all machines, get the dictionary of machines that are eligible for some job
        /// (in minizinc, we do not want to have any machines that are not eligible for any job
        /// as this creates satisfiability problems)
        /// </summary>
        /// <param name="jobs">List of jobs</param>
        /// <param name="machines">Dictionary of machines</param>
        /// <returns>Dictionary of eligible machines</returns>
        private static IDictionary<int, IMachine> getEligibleMachines(IList<IJob> jobs, IDictionary<int, IMachine> machines)
        {
            IDictionary<int, IMachine> eligibleMachines = new Dictionary<int, IMachine>();

            foreach (IJob job in jobs)
            {
                foreach (int i in job.EligibleMachines)
                {
                    if (!eligibleMachines.TryGetValue(i, out IMachine eligibleMachine))
                    {
                        machines.TryGetValue(i, out IMachine machine);
                        eligibleMachines.Add(i, machine);
                    }                 
                }                    
            }

            return eligibleMachines;
        }

        /// <summary>
        /// Take an instance and convert it into a MiniZinc instance file content
        /// </summary>
        /// <param name="instance">The instance that should be converted</param>
        /// <param name="weights">Weights of the components of the objective function that should be used by the minizinc solver</param>
        /// <param name="convertToCPOptimizer">Optional boolean indicating whether insatnce should be converted to CP Optimizer instance instead of minizinc instance</param>
        /// <param name="specialCaseLexicographicOptimization">Optional boolean indicating whether weights should be created for the case of lexicographic minimization 
        /// with total oven runtime lexicographically more important than tardiness, 
        /// tardiness lexicographically more important than setup costs.</param>
        /// <returns>Return the MiniZinc instance file contents as a string</returns>
        public string ConvertToMiniZincInstance(IInstance instance, IWeightObjective weights, 
            bool convertToCPOptimizer = false, 
            bool specialCaseLexicographicOptimization = false) 
        {
            InstanceData instanceData = Preprocessor.DoPreprocessing(instance, weights);

            IList<IJob> jobs = instance.Jobs;
            IDictionary<int,IAttribute> attributes = instance.Attributes;
            IDictionary<int, IMachine> machines = getEligibleMachines(jobs, instance.Machines);

            TimeSpan schedulingHorizonLength = instanceData.LengthSchedulingHorizon;
            //double-value is truncated to get int value, i.e. the schedulingHorizonLength is rounded down
            int l = (int)schedulingHorizonLength.TotalMinutes; 
            int a = instanceData.NumberOfAttributes;
            int m = instanceData.NumberOfMachines;
            int n = instanceData.NumberOfJobs;
            int s = instanceData.MaxNumberOfAvailabilityIntervals;


            // for MiniZinc instance, machines, attributes and jobs need to be numbered from 1 to m/a/n
            // therefore: machine/attributes/jobs with smallest Id gets number 1, machine with next smallest number 2 etc.
            // for this purpose we need sorted dictionaries and the functions MiniZincId and MiniZincAttributeId
            SortedDictionary<int, IMachine> sortedMachines = new SortedDictionary<int, IMachine>(machines);
            SortedDictionary<int, IAttribute> sortedAttributes = new SortedDictionary<int, IAttribute>(attributes);

            Func<int,int> MiniZincId = ConvertMachineIdToMinizinc(machines);
            Func<int, int> MiniZincAttributeId = ConvertAttributeIdToMinizinc(attributes);
            
            // list of jobs sorted by increasing ID for minizinc input
            List<IJob> sortedJobs = jobs.OrderBy(j => j.Id).ToList();

            

            string fileContents = "";
            if (!convertToCPOptimizer)
            // write to MiniZinc file
            {
                fileContents += L + l + MZN_LINE_END + "\n";

                fileContents += A + a + MZN_LINE_END + "\n";
                string setupCosts = SETUP_COSTS;
                string setupTimes = SETUP_TIMES;
                foreach (KeyValuePair<int, IAttribute> attribute in sortedAttributes)
                {
                    setupCosts += SETUP_COST_LINE;
                    foreach (int cost in attribute.Value.SetupCostsAttribute)
                    {
                        setupCosts += cost.ToString(CultureInfo.InvariantCulture) + ",";
                    }
                    setupCosts += "\n";
                    setupTimes += SETUP_TIMES_LINE;
                    foreach (int time in attribute.Value.SetupTimesAttribute)
                    {
                        int eps = 0;
                        if (time % 60 != 0)
                        {
                            eps += 1;
                        }
                        // in case time/60 is not an integer (ie not entire minutes), we round up
                        int timeInMinutes = time / 60 + eps;
                        setupTimes += timeInMinutes.ToString(CultureInfo.InvariantCulture) + ",";
                    }
                    setupTimes += "\n";
                }
                //add 0s at the end (when no setup is required)          
                setupCosts += SETUP_COST_LINE;
                setupTimes += SETUP_TIMES_LINE;
                for (int i = 0; i < a; i++)
                {
                    setupCosts += "0,";
                    setupTimes += "0,";
                }
                setupCosts += "\n";
                setupTimes += "\n";

                fileContents += setupCosts.TrimEnd(',', '\n', '|') + SETUP_COSTS_END + MZN_LINE_END + "\n";
                fileContents += setupTimes.TrimEnd(',', '\n', '|') + SETUP_TIMES_END + MZN_LINE_END + "\n";


                fileContents += M + m + MZN_LINE_END + "\n";
                string minCap = MIN_CAP;
                string maxCap = MAX_CAP;

                foreach (KeyValuePair<int, IMachine> machine in sortedMachines)
                {
                    minCap += machine.Value.MinCap.ToString(CultureInfo.InvariantCulture) + ",";
                    maxCap += machine.Value.MaxCap.ToString(CultureInfo.InvariantCulture) + ",";
                }
                fileContents += minCap.TrimEnd(',') + MIN_CAP_END + MZN_LINE_END + "\n";
                fileContents += maxCap.TrimEnd(',') + MAX_CAP_END + MZN_LINE_END + "\n";
                fileContents += "\n";

                //initial state of machines
                if (instance.InitialStates != null)
                {
                    string initStateMachines = INIT_STATE 
                        + String.Join(",", instance.InitialStates.Values.ToList()) 
                        + INIT_STATE_END;
                    fileContents += initStateMachines + MZN_LINE_END +"\n";
                }

                fileContents += S + s + MZN_LINE_END + "\n";
                string availabilityStart = MACHINE_AVAILABILITY_START;
                string availabilityEnd = MACHINE_AVAILABILITY_END;
                foreach (IMachine machine in sortedMachines.Values)
                {
                    //if there are less than s shifts on this machine, null-shifts from 0 to 0 are added at the beginning
                    int i = s - machine.AvailabilityStart.Count;
                    string null_shifts = "";
                    for (int j = 0; j < i; j++)
                    {
                        null_shifts += "0,";
                    }

                    availabilityStart += MACHINE_AVAILABILITY_LINE + null_shifts;

                    foreach (DateTime date in machine.AvailabilityStart)
                    {
                        if (date <= instance.SchedulingHorizonStart) 
                        {
                            availabilityStart += 0 + ",";
                        }
                        else
                        {
                            TimeSpan startToShiftStart = date.Subtract(instance.SchedulingHorizonStart);
                            // milliseconds are ignored
                            startToShiftStart = DropMillisecondsTimespan(startToShiftStart);
                            // if shift start time is not in entire minutes, it is rounded up
                            availabilityStart += (int)Math.Ceiling(startToShiftStart.TotalMinutes) + ",";
                        }
                        
                    }
                    availabilityStart += "\n";

                    availabilityEnd += MACHINE_AVAILABILITY_LINE + null_shifts;
                    foreach (DateTime date in machine.AvailabilityEnd)
                    {
                        if (date >= instance.SchedulingHorizonEnd)
                        {
                            availabilityEnd += l + ",";
                        }
                        else
                        {
                            TimeSpan startToShiftEnd = date.Subtract(instance.SchedulingHorizonStart);
                            // milliseconds are ignored
                            startToShiftEnd = DropMillisecondsTimespan(startToShiftEnd);
                            // if shift end time is not in entire minutes, it is rounded up
                            availabilityEnd += (int)Math.Ceiling(startToShiftEnd.TotalMinutes) + ",";
                        }                        
                        
                    }
                    availabilityEnd += "\n";
                }

                fileContents += availabilityStart.TrimEnd(',', '\n', '|') + MACHINE_AVAILABILITY_START_END + MZN_LINE_END + "\n";
                fileContents += availabilityEnd.TrimEnd(',', '\n', '|') + MACHINE_AVAILABILITY_END_END + MZN_LINE_END + "\n";


                fileContents += N + n + MZN_LINE_END + "\n";
                string eligibleMachines = ELIGIBLE_MACHINES;
                foreach (IJob job in sortedJobs)
                {
                    string eligibleMachinesLine = ELIGIBLE_MACHINES_LINE;
                    foreach (int machineId in job.EligibleMachines)
                    {
                        eligibleMachinesLine += MiniZincId(machineId).ToString(CultureInfo.InvariantCulture) + ",";
                    }
                    eligibleMachines += eligibleMachinesLine.TrimEnd(',') + ELIGIBLE_MACHINES_LINE_END + "\n";
                }
                fileContents += eligibleMachines.TrimEnd(',', '\n') + ELIGIBLE_MACHINES_END + MZN_LINE_END + "\n";
                string earliestStart = EARLIEST_START;
                string latestEnd = LATEST_END;
                string minTime = MIN_TIME;
                string maxTime = MAX_TIME;
                string size = SIZE;
                string jobAttribute = JOB_ATTRIBUTE;        
                foreach (IJob job in sortedJobs)
                {
                    TimeSpan startToEarliestStart = job.EarliestStart.Subtract(instance.SchedulingHorizonStart);
                    // milliseconds are ignored
                    startToEarliestStart = DropMillisecondsTimespan(startToEarliestStart);
                    // if earliest start time is not in entire minutes, it is rounded up
                    int earliestStartMinutes = (int)Math.Ceiling(startToEarliestStart.TotalMinutes);
                    earliestStart += earliestStartMinutes.ToString(CultureInfo.InvariantCulture) + ",";
                    TimeSpan startToLatestEnd = job.LatestEnd.Subtract(instance.SchedulingHorizonStart);
                    // milliseconds are ignored
                    startToLatestEnd = DropMillisecondsTimespan(startToLatestEnd);
                    // if latest end time is not in entire minutes, it is rounded down
                    int latestEndMinutes = (int)Math.Floor(startToLatestEnd.TotalMinutes);
                    latestEnd += latestEndMinutes.ToString(CultureInfo.InvariantCulture) + ",";

                    // in case job.MinTime is not in entire minutes, it is rounded up
                    int MinTimeInMinutes = RoundUpFromSecondsToMinutes(job.MinTime);
                    minTime += MinTimeInMinutes.ToString(CultureInfo.InvariantCulture) + ",";

                    // in case job.MaxTime is not in entire minutes, it is rounded up
                    int MaxTimeInMinutes = RoundUpFromSecondsToMinutes(job.MaxTime);
                    maxTime += MaxTimeInMinutes.ToString(CultureInfo.InvariantCulture) + ",";

                    size += job.Size.ToString(CultureInfo.InvariantCulture) + ",";
                    //pick the first eligible machine for this jobs
                    int machineId = job.EligibleMachines.First();
                    jobAttribute += MiniZincAttributeId(job.AttributeIdPerMachine[machineId]).ToString(CultureInfo.InvariantCulture) + ",";

                }
                fileContents += earliestStart.TrimEnd(',') + EARLIEST_START_END + MZN_LINE_END + "\n";
                fileContents += latestEnd.TrimEnd(',') + LATEST_END_END + MZN_LINE_END + "\n";
                fileContents += minTime.TrimEnd(',') + MIN_TIME_END + MZN_LINE_END + "\n";
                fileContents += maxTime.TrimEnd(',') + MAX_TIME_END + MZN_LINE_END + "\n";
                fileContents += size.TrimEnd(',') + SIZE_END + MZN_LINE_END + "\n";

                fileContents += jobAttribute.TrimEnd(',', '\n');
                fileContents += ATTRIBUTE_END + MZN_LINE_END + "\n";

                fileContents += "\n";
            }
            else
            // write to CPOptimizer file
            {
                fileContents += cpL + l + cp_LINE_END + "\n";

                fileContents += cpA + a + cp_LINE_END + "\n";
                string setupCosts = cpSETUP_COSTS;
                string setupTimes = cpSETUP_TIMES;
                //add 0s at the beginning (when no setup is required)
                setupCosts += cpSETUP_COST_LINE;
                setupTimes += cpSETUP_TIMES_LINE;
                for (int i = 0; i < a; i++)
                {
                    setupCosts += "0,";
                    setupTimes += "0,";
                }
                setupCosts = setupCosts.TrimEnd(',');
                setupCosts += cpSETUP_LINE_END + "\n";
                setupTimes = setupTimes.TrimEnd(',');
                setupTimes += cpSETUP_LINE_END + "\n";
                foreach (KeyValuePair<int, IAttribute> attribute in sortedAttributes)
                {
                    setupCosts += cpSETUP_COST_LINE;
                    foreach (int cost in attribute.Value.SetupCostsAttribute)
                    {
                        setupCosts += cost.ToString(CultureInfo.InvariantCulture) + ",";
                    }
                    setupCosts = setupCosts.TrimEnd(',');
                    setupCosts += cpSETUP_LINE_END + "\n";
                    setupTimes += cpSETUP_TIMES_LINE;
                    foreach (int time in attribute.Value.SetupTimesAttribute)
                    {
                        int eps = 0;
                        if (time % 60 != 0)
                        {
                            eps += 1;
                        }
                        // in case time/60 is not an integer (ie not entire minutes), we round up
                        int timeInMinutes = time / 60 + eps;
                        setupTimes += timeInMinutes.ToString(CultureInfo.InvariantCulture) + ",";
                    }
                    setupTimes = setupTimes.TrimEnd(',');
                    setupTimes += cpSETUP_LINE_END + "\n";
                }
                

                fileContents += setupCosts.TrimEnd(',', '\n', '|', ']') + cpSETUP_COSTS_END + cp_LINE_END + "\n";
                fileContents += setupTimes.TrimEnd(',', '\n', '|', ']') + cpSETUP_TIMES_END + cp_LINE_END + "\n";


                fileContents += cpM + m + cp_LINE_END + "\n";
                string minCap = cpMIN_CAP;
                string maxCap = cpMAX_CAP;

                foreach (KeyValuePair<int, IMachine> machine in sortedMachines)
                {
                    minCap += machine.Value.MinCap.ToString(CultureInfo.InvariantCulture) + ",";
                    maxCap += machine.Value.MaxCap.ToString(CultureInfo.InvariantCulture) + ",";
                }
                fileContents += minCap.TrimEnd(',') + cpMIN_CAP_END + cp_LINE_END + "\n";
                fileContents += maxCap.TrimEnd(',') + cpMAX_CAP_END + cp_LINE_END + "\n";
                fileContents += "\n";

                //initial state of machines
                if (instance.InitialStates != null)
                {
                    string initStateMachines = INIT_STATE
                        + String.Join(",", instance.InitialStates.Values.ToList())
                        + INIT_STATE_END + cp_LINE_END; ;
                    fileContents += initStateMachines + "\n";
                }

                fileContents += cpS + s + cp_LINE_END + "\n";
                string availabilityStart = cpMACHINE_AVAILABILITY_START;
                string availabilityEnd = cpMACHINE_AVAILABILITY_END;
                foreach (IMachine machine in sortedMachines.Values)
                {
                    //if there are less than s shifts on this machine, null-shifts from 0 to 0 are added at the beginning
                    int i = s - machine.AvailabilityStart.Count;
                    string null_shifts = "";
                    for (int j = 0; j < i; j++)
                    {
                        null_shifts += "0,";
                    }

                    availabilityStart += cpMACHINE_AVAILABILITY_LINE + null_shifts;

                    foreach (DateTime date in machine.AvailabilityStart)
                    {
                        if (date <= instance.SchedulingHorizonStart)
                        {
                            availabilityStart += 0 + ",";
                        }
                        else
                        {
                            TimeSpan startToShiftStart = date.Subtract(instance.SchedulingHorizonStart);
                            // milliseconds are ignored
                            startToShiftStart = DropMillisecondsTimespan(startToShiftStart);
                            // if shift start time is not in entire minutes, it is rounded up
                            availabilityStart += (int)Math.Ceiling(startToShiftStart.TotalMinutes) + ",";
                        }
                    }


                    availabilityStart = availabilityStart.TrimEnd(',');
                    availabilityStart += cpMACHINE_AVAILABILITY_LINE_END + "\n";
                    
                    availabilityEnd += cpMACHINE_AVAILABILITY_LINE + null_shifts;
                    foreach (DateTime date in machine.AvailabilityEnd)
                    {
                        if (date >= instance.SchedulingHorizonEnd)
                        {
                            availabilityEnd += l + ",";
                        }
                        else
                        {
                            TimeSpan startToShiftEnd = date.Subtract(instance.SchedulingHorizonStart);
                            // milliseconds are ignored
                            startToShiftEnd = DropMillisecondsTimespan(startToShiftEnd);
                            // if shift end time is not in entire minutes, it is rounded up
                            availabilityEnd += (int)Math.Ceiling(startToShiftEnd.TotalMinutes) + ",";
                        }
                    }
                    availabilityEnd = availabilityEnd.TrimEnd(',');
                    availabilityEnd += cpMACHINE_AVAILABILITY_LINE_END + "\n";
                }

                fileContents += availabilityStart.TrimEnd(',', '\n', '|', ']') + cpMACHINE_AVAILABILITY_START_END + cp_LINE_END + "\n";
                fileContents += availabilityEnd.TrimEnd(',', '\n', '|', ']') + cpMACHINE_AVAILABILITY_END_END + cp_LINE_END + "\n";


                fileContents += cpN + n + cp_LINE_END + "\n";
                string eligibleMachines = cpELIGIBLE_MACHINES;
                foreach (IJob job in sortedJobs)
                {
                    string eligibleMachinesLine = cpELIGIBLE_MACHINES_LINE;
                    foreach (int machineId in job.EligibleMachines)
                    {
                        eligibleMachinesLine += MiniZincId(machineId).ToString(CultureInfo.InvariantCulture) + ",";
                    }
                    eligibleMachines += eligibleMachinesLine.TrimEnd(',') + cpELIGIBLE_MACHINES_LINE_END + "\n";
                }
                fileContents += eligibleMachines.TrimEnd(',', '\n') + cpELIGIBLE_MACHINES_END + cp_LINE_END + "\n";
                string earliestStart = cpEARLIEST_START;
                string latestEnd = cpLATEST_END;
                string minTime = cpMIN_TIME;
                string maxTime = cpMAX_TIME;
                string size = cpSIZE;
                string jobAttribute = cpJOB_ATTRIBUTE;
                foreach (IJob job in sortedJobs)
                {
                    TimeSpan startToEarliestStart = job.EarliestStart.Subtract(instance.SchedulingHorizonStart);
                    // milliseconds are ignored
                    startToEarliestStart = DropMillisecondsTimespan(startToEarliestStart);
                    // if earliest start time is not in entire minutes, it is rounded up
                    int earliestStartMinutes = (int)Math.Ceiling(startToEarliestStart.TotalMinutes);
                    earliestStart += earliestStartMinutes.ToString(CultureInfo.InvariantCulture) + ",";
                    TimeSpan startToLatestEnd = job.LatestEnd.Subtract(instance.SchedulingHorizonStart);
                    // milliseconds are ignored
                    startToLatestEnd = DropMillisecondsTimespan(startToLatestEnd);
                    // if latest end time is not in entire minutes, it is rounded down
                    int latestEndMinutes = (int)Math.Floor(startToLatestEnd.TotalMinutes);
                    latestEnd += latestEndMinutes.ToString(CultureInfo.InvariantCulture) + ",";

                    // in case job.MinTime is not in entire minutes, it is rounded up
                    int MinTimeInMinutes = RoundUpFromSecondsToMinutes(job.MinTime);
                    minTime += MinTimeInMinutes.ToString(CultureInfo.InvariantCulture) + ",";

                    // in case job.MaxTime is not in entire minutes, it is rounded up
                    int MaxTimeInMinutes = RoundUpFromSecondsToMinutes(job.MaxTime);
                    maxTime += MaxTimeInMinutes.ToString(CultureInfo.InvariantCulture) + ",";

                    size += job.Size.ToString(CultureInfo.InvariantCulture) + ",";

                    //attribute of job
                    //Note: CP Optimizer data can currently not be created for Model.modelMCP with job-attributes per machine
                    //pick the first eligible machine for this jobs
                    int machineId = job.EligibleMachines.First();
                    jobAttribute += MiniZincAttributeId(job.AttributeIdPerMachine[machineId]).ToString(CultureInfo.InvariantCulture) + ",";
                }
                fileContents += earliestStart.TrimEnd(',') + cpEARLIEST_START_END + cp_LINE_END + "\n";
                fileContents += latestEnd.TrimEnd(',') + cpLATEST_END_END + cp_LINE_END + "\n";
                fileContents += minTime.TrimEnd(',') + cpMIN_TIME_END + cp_LINE_END + "\n";
                fileContents += maxTime.TrimEnd(',') + cpMAX_TIME_END + cp_LINE_END + "\n";
                fileContents += size.TrimEnd(',') + cpSIZE_END + cp_LINE_END + "\n";
                fileContents += jobAttribute.TrimEnd(',') + cpATTRIBUTE_END + cp_LINE_END + "\n";
                fileContents += "\n";
            }

            //constants needed for the calculation of the objective function
            if (specialCaseLexicographicOptimization)
            //create weight factors for the special case of lexicographic minimization
            //with total oven runtime lexicographically more important than tardiness
            //tardiness lexicographically more important than setup costs
            {
                //calculation of weights and upper bound
                long weightSetupCosts = 1;
                long weightTardiness = instanceData.NumberOfJobs * instanceData.MaxSetupCost + 1;
                long weightRuntime = weightTardiness * (n + 1);
                //upperBound = weightSetupCosts * n * instanceData.MaxSetupCost + weightTardiness * n  + weightRuntime * upperBound(runtime) 
                long upperBoundIntegerObjective = 
                    weightTardiness - 1 
                    + weightTardiness * instanceData.NumberOfJobs
                    + weightRuntime * instanceData.UpperBoundTotalRuntimeMinutes;

                //add weights and upper bound to string
                fileContents += UPPER_BOUND_INTEGER_OBJECTIVE + upperBoundIntegerObjective + MZN_LINE_END + "\n";
                fileContents += MULT_FACTOR_TOTAL_RUNTIME + weightRuntime + MZN_LINE_END + "\n";
                fileContents += MULT_FACTOR_FINISHED_TOOLATE + weightTardiness + MZN_LINE_END + "\n";
                fileContents += MULT_FACTOR_TOTAL_SETUPTIMES + "0" + MZN_LINE_END + "\n";
                fileContents += MULT_FACTOR_TOTAL_SETUPCOSTS + weightSetupCosts + MZN_LINE_END + "\n";
            }
            else //normal calculation of weight-factors
            {
                fileContents += UPPER_BOUND_INTEGER_OBJECTIVE + instanceData.UpperBoundForIntegerObjective + MZN_LINE_END + "\n";
                fileContents += MULT_FACTOR_TOTAL_RUNTIME + instanceData.MultFactorTotalRuntime + MZN_LINE_END + "\n";
                fileContents += MULT_FACTOR_FINISHED_TOOLATE + instanceData.MultFactorFinishedTooLate + MZN_LINE_END + "\n";
                fileContents += MULT_FACTOR_TOTAL_SETUPTIMES + instanceData.MultFactorTotalSetupTimes + MZN_LINE_END + "\n";
                fileContents += MULT_FACTOR_TOTAL_SETUPCOSTS + instanceData.MultFactorTotalSetupCosts + MZN_LINE_END + "\n";

            }
            fileContents += UPPER_BOUND_TOTAL_RUNTIME + instanceData.UpperBoundTotalRuntimeMinutes + MZN_LINE_END + "\n";
            fileContents += MIN_DURATION + instanceData.MinMinTimeMinutes + MZN_LINE_END + "\n";
            fileContents += MAX_DURATION + instanceData.MaxMinTimeMinutes + MZN_LINE_END + "\n";
            fileContents += MAX_SETUP_TIME + instanceData.MaxSetupTimeMinutes + MZN_LINE_END + "\n";
            fileContents += MAX_SETUP_COST + instanceData.MaxSetupCost + MZN_LINE_END + "\n";
            
            return fileContents;
        }

        /// <summary>
        /// Take a (partial) initial solution and convert it into a MiniZinc additional instance file content
        /// which van be used for warm start in MiniZinc
        /// Note: The produced data is correct but minizinc does not seem to be capable of handling partial input
        /// </summary>
        /// /// <param name="instance">The instance for which the partial solution was created</param>
        /// <param name="partialSolution">The partial solution that should be converted</param>
   	    /// <param name="convertToCPOptimizer">Optional boolean indicating whether instance should be converted to CP Optimizer instance instead of minizinc instance</param>
        /// <param name="reprJobPerBatchModel">Optional parameter indictaing whether the warm start data is created for a 
        /// minizinc model with a representative job per batch</param>
        /// <returns>Return the MiniZinc warm start data file contents as a string</returns>
        public string ConvertToMiniZincWarmStartData(IInstance instance, IOutput partialSolution, 
            bool reprJobPerBatchModel = false, bool convertToCPOptimizer = false)
        {
            IList<int> allJobs = instance.Jobs.Select(x => x.Id).ToList();
            IDictionary<int, IMachine> machines = instance.Machines;
            int machineCount = machines.Count;
            int jobCount = allJobs.Count;

            TimeSpan schedulingHorizonLength = instance.SchedulingHorizonEnd.Subtract(instance.SchedulingHorizonStart);
            int l = (int)schedulingHorizonLength.TotalMinutes;

            // create list of job IDs that are part of the partial solution 
            IList<int> scheduledJobs = new List<int>();
            IList<int> unscheduledJobs = new List<int>();
            foreach (var assignment in partialSolution.BatchAssignments)
            {
                scheduledJobs.Add(assignment.Job.Id);
                //check if job is actually part of instance (via ID)
                if (!allJobs.Contains(assignment.Job.Id))
                {
                    Console.WriteLine("Jobs in instance and jobs in solution do not match");
                    string errorMessage = "Jobs in instance " + instance.Name +
                        " and jobs in solution " + partialSolution.Name + " do not match: job with ID "
                        + assignment.Job.Id.ToString() + " is not part of instance \n";
                    using (StreamWriter sw = File.AppendText("./errorlog.txt"))
                    {
                        sw.Write(errorMessage);
                        sw.Close();
                    }
                    

                    return null;
                }

            }

            foreach (int jobId in allJobs)
            {
                if (!scheduledJobs.Contains(jobId))
                {
                    unscheduledJobs.Add(jobId);
                }
            }
            //append list of unscheduled jobs to unscheduledJobFile
            if (unscheduledJobs.Any())
            {
                string unscheduledJobFile = "The jobs with following IDs were not assigned in initial solution: "
                    + string.Join(", ", unscheduledJobs) + " for instance " + instance.Name + "\n";

                string unscheduledJobFilePath = "./unscheduledJobs.txt";
                using (StreamWriter sw = File.AppendText(unscheduledJobFilePath))
                {
                    sw.Write(unscheduledJobFile);
                    sw.Close();
                }
            }
            if (!(unscheduledJobs.Count + scheduledJobs.Count == jobCount))
            {
                Console.WriteLine("Jobs in instance and jobs in solution do not match");
                File.WriteAllText("./errorlog.txt", "Jobs in instance " +  instance.Name + 
                    " and jobs in solution " + partialSolution.Name + " do not match");

                return null;
            }

            // dictionaries of batches and machines for jobs:
            // key is job id 
            // value is minizinc-batchId (integer between 1 and number of jobs)
            // resp. minizinc-machineIds (integer between 1 and number of machines)
            IDictionary<int, string> batchForJob = new Dictionary<int, string>();
            IDictionary<int, string> machineForJob = new Dictionary<int, string>();
            //add entries to dictionaries for unscheduled jobs
            foreach (int jobId in unscheduledJobs)
            {
                batchForJob.Add(jobId, "<>");
                machineForJob.Add(jobId, "<>");
            }

            //list of batch start times (in minutes since start of scheduling horizon start)
            IList<int> startTimes = new List<int>();
            //initilase list with values for empty batches 
            //startTime = end of scheduling horizon
            for (int i = 0; i < machineCount * jobCount; i++)
            {
                startTimes.Add(l);
            }

		    List<int> sortedMachineIds = new List<int>();
		    foreach (IMachine machine in machines.Values)
		    {
		        sortedMachineIds.Add(machine.Id);
		    }
		    sortedMachineIds.Sort();

            for (int i = 0; i < sortedMachineIds.Count; i++)
            {
                //write entries for batchForJob, machineForJob and startTimes for all jobs/batches scheduled on the i-th machine

                ICollection<IBatchAssignment> batchesOnCurrentMachine
                    = partialSolution.BatchAssignments.Where(x => x.AssignedBatch.AssignedMachine.Id
                    == sortedMachineIds[i]).ToList();

                int batchCountOnCurrentMachine = 0;
                int currentBatchId = 0;

                foreach (IBatchAssignment batchAssignment in batchesOnCurrentMachine.OrderBy(x => x.AssignedBatch.StartTime))
                {
                    //if this batch is different from previous batch
                    if (!(batchAssignment.AssignedBatch.Id == currentBatchId))
                    {
                        //increase batchCountOnCurrentMachine
                        batchCountOnCurrentMachine += 1;

                        //update currentBatchId
                        currentBatchId = batchAssignment.AssignedBatch.Id;

                        //write start time 
                        TimeSpan startToBatchStart = batchAssignment.AssignedBatch.StartTime.Subtract(instance.SchedulingHorizonStart);
                        // milliseconds are ignored
                        startToBatchStart = DropMillisecondsTimespan(startToBatchStart);
                        // if start time is not in entire minutes, it is rounded up
                        int startToBatchStartMinutes = (int)Math.Ceiling(startToBatchStart.TotalMinutes);
                        startTimes[batchCountOnCurrentMachine - 1 + i * jobCount] = startToBatchStartMinutes;
                    }

                    //add jobId and batchId/machineId to resp. dictionnaries
                    batchForJob.Add(batchAssignment.Job.Id, batchCountOnCurrentMachine.ToString());
                    machineForJob.Add(batchAssignment.Job.Id, (i + 1).ToString());
                }
            }

            //sort batchForJob and machineForJob by their keys
            SortedDictionary<int, string> sortedBatchForJob = new SortedDictionary<int, string>(batchForJob);
            SortedDictionary<int, string> sortedMachineForJob = new SortedDictionary<int, string>(machineForJob);

            string warmStartFileContents = "";
            if (!reprJobPerBatchModel && !convertToCPOptimizer)
            {              

                warmStartFileContents += "warm_start = warm_start_array( [ \n" + "";

                warmStartFileContents += "warm_start( batch_for_job, array1d(1.." + jobCount.ToString()
                    + ",[" + string.Join(",", sortedBatchForJob.Values) + "])),\n";
                warmStartFileContents += "warm_start( machine_for_job, array1d(1.." + jobCount.ToString()
                    + ",[" + string.Join(",", sortedMachineForJob.Values) + "])),\n";
                warmStartFileContents += "warm_start( start_times, [" + string.Join(",", startTimes) + "])\n ] )";
            }
            else if (reprJobPerBatchModel && !convertToCPOptimizer)
            {
                //dictionary of job start times on machines (in minutes since start of scheduling horizon start)
                //keys are (jobId, machineId)
                IDictionary<(int, int), int> startTimesMach = new Dictionary<(int, int), int>();
                //dictionary of job start times, keys are (jobId)
                IDictionary<int, int> jobStartTimes = new Dictionary<int, int>();
                //dictionary of job durations on machines (in minutes since start of scheduling horizon start)
                //keys are (jobId, machineId)
                IDictionary<(int, int), int> durationsMach = new Dictionary<(int, int), int>();
                IDictionary<int, int> durations = new Dictionary<int, int>();
                //dictionary of pointers to representative jobs
                IDictionary<int, int> inBatchWithJob = new Dictionary<int, int>();


                IDictionary<(int machId, int batchId), (IBatch batch, IList<int> jobs)> jobsInBatchDict =
                    new Dictionary<(int, int), (IBatch, IList<int>)>();

                //create lists of jobs per batch
                foreach (var assignment in partialSolution.BatchAssignments)
                {
                    int machId = assignment.AssignedBatch.AssignedMachine.Id;
                    int batchId = assignment.AssignedBatch.Id;
                    int jobId = assignment.Job.Id;
                    IBatch batch = assignment.AssignedBatch;
                    if (!jobsInBatchDict.ContainsKey((machId, batchId)))
                    {
                        jobsInBatchDict.Add((machId, batchId), (assignment.AssignedBatch, new List<int>()));
                    }
                    jobsInBatchDict[(machId, batchId)].jobs.Add(jobId);

                }                

                foreach (var entry in jobsInBatchDict)
                {
                    int machineId = entry.Key.machId;
                    IBatch batch = entry.Value.batch;

                    //representative job for this batch
                    int reprJobBatch = entry.Value.jobs.Min();
                    for (int j = 0; j < entry.Value.jobs.Count(); j++)
                    {
                        int jobId = entry.Value.jobs[j];
                        if (jobId == reprJobBatch)
                        {
                            //write start time
                            TimeSpan startToBatchStart = batch.StartTime.Subtract(instance.SchedulingHorizonStart);
                            // milliseconds are ignored
                            startToBatchStart = DropMillisecondsTimespan(startToBatchStart);
                            // if start time is not in entire minutes, it is rounded up
                            int startToBatchStartMinutes = (int)Math.Ceiling(startToBatchStart.TotalMinutes);
                            startTimesMach.Add((jobId, machineId), startToBatchStartMinutes);
                            jobStartTimes.Add(jobId, startToBatchStartMinutes);

                            //write duration
                            TimeSpan batchDuration = batch.EndTime.Subtract(batch.StartTime);
                            // milliseconds are ignored
                            batchDuration = DropMillisecondsTimespan(batchDuration);
                            // if start time is not in entire minutes, it is rounded up
                            int batchDurationMinutes = (int)Math.Ceiling(batchDuration.TotalMinutes);
                            durationsMach.Add((jobId, machineId), batchDurationMinutes);
                            durations.Add(jobId, batchDurationMinutes);
                        }
                        else
                        {
                            //write inBatchWithJob
                            inBatchWithJob.Add(jobId, reprJobBatch);
                        }
                    }
                }

                string inBatchWithJobString = "";
                //string jobStartOnMachString = "";
                //string jobDurOnMachString = "";
                string jobDurString = "";
                for (int i = 1; i <= instance.Jobs.Count; i++)
                {
                    //add entry to inBatchWithJobString
                    //assuming that jobs have IDs 1, ..., n
                    if (inBatchWithJob.ContainsKey(i))
                    {
                        inBatchWithJobString += inBatchWithJob[i].ToString() + ','; 
                    }
                    else
                    {
                        inBatchWithJobString += "0,";
                    }
                    //add entry to jobDurString
                    if (durations.ContainsKey(i))
                    {
                         jobDurString += durations[i].ToString() + ',';
                    }
                    else
                    {
                         jobDurString += "0,";
                    }

                    //cannot provide optional variable to minizinc as warmstart data
                    //for (int j = 1; j <= instance.Machines.Count; j++)
                    //{
                    //    //add entry to jobStartOnMachString
                    //    //add entry to jobDurOnMachString
                    //    //assuming that machines have IDs 1, ..., m
                    //    if (startTimesMach.ContainsKey((i,j)))
                    //    {
                    //        jobStartOnMachString += startTimes[(i,j)].ToString() + ',';
                    //        jobDurOnMachString += durations[(i, j)].ToString() + ',';
                    //    }
                    //    else
                    //    {
                    //        jobStartOnMachString += "<>,";
                    //        jobDurOnMachString += "0,";
                    //    }
                    //}
                }

            warmStartFileContents += "warm_start = warm_start_array( [ \n" + "";
            //warmStartFileContents += "warm_start( startOnMach, [" + jobStartOnMachString.TrimEnd(',') + "]),\n";
            warmStartFileContents += "warm_start( dur, [" + jobDurString.TrimEnd(',') + "]),\n";
            warmStartFileContents += "warm_start( inBatchWithJob, [" + inBatchWithJobString.TrimEnd(',') + "])\n ] )";

            }

            
            if (convertToCPOptimizer)
            {
                warmStartFileContents = "";
                // OPL
                IList<int> allMachines = instance.Machines.Select(x => x.Key).ToList();

                // batch start
                warmStartFileContents += "WsBatchStart = [";
                for (int m = 0; m < machineCount; m++)
                {
                    warmStartFileContents += "[";
                    for (int b = 0; b < jobCount; b++)
                    {
                        int startTime = startTimes[m * jobCount + b];
                        if (startTime == l)
                        {
                            startTime = -1;
                        }
                        warmStartFileContents += startTime;
                        if (b != jobCount - 1)
                        {
                            warmStartFileContents += ",";
                        }
                    }
                    warmStartFileContents += "]";
                    if (m != machineCount - 1)
                    {
                        warmStartFileContents += ",";
                    }
                }
                warmStartFileContents += "];\n";

                // JobStart
                warmStartFileContents += "WsJobStart = [";
                foreach (int j in allJobs)
                {
                    int batch = int.Parse(batchForJob[j], DateTimeFormatInfo.InvariantInfo);
                    int machine = int.Parse(machineForJob[j], DateTimeFormatInfo.InvariantInfo);
                    // if all jobs that have the same batch on the same machine as this job are greater or equal to this job, it is the representative job 
                    if (allJobs.Where(x => batch == int.Parse(batchForJob[x], DateTimeFormatInfo.InvariantInfo) && machine == int.Parse(machineForJob[x], DateTimeFormatInfo.InvariantInfo)).All(x => x >= j))
                    {
                        int startTime = startTimes[(machine - 1) * jobCount + (batch - 1)];
                        if (startTime == l)
                        {
                            startTime = -1;
                        }

                        warmStartFileContents += startTime;
                    }
                    else
                    {
                        warmStartFileContents += "-1";
                    }
                    if (j != allJobs.Last())
                    {
                        warmStartFileContents += ",";
                    }
                }
                warmStartFileContents += "];\n";

                // JobOnMachStart
                warmStartFileContents += "WsJobOnMachStart = [";
                foreach (int j in allJobs)
                {
                    warmStartFileContents += "[";
                    foreach (int m in allMachines)
                    {
                        int machine = int.Parse(machineForJob[j], DateTimeFormatInfo.InvariantInfo);
                        int batch = int.Parse(batchForJob[j], DateTimeFormatInfo.InvariantInfo);
                        // if all jobs that have the same batch on the same machine as this job are greater or equal to this job, it is the representative job 
                        if (m == machine && allJobs.Where(x =>
                                batch == int.Parse(batchForJob[x], DateTimeFormatInfo.InvariantInfo) && machine ==
                                int.Parse(machineForJob[x], DateTimeFormatInfo.InvariantInfo)).All(x => x >= j))
                        {
                            int startTime = startTimes[(machine - 1) * jobCount + (batch - 1)];
                            if (startTime == l)
                            {
                                startTime = -1;
                            }

                            warmStartFileContents += startTime;
                        }
                        else
                        {
                            warmStartFileContents += "-1";
                        }

                        if (m != allMachines.Last())
                        {
                            warmStartFileContents += ",";
                        }
                    }
                    if (j != allJobs.Last())
                    {
                        warmStartFileContents += ",";
                    }
                    warmStartFileContents += "]";
                }
                warmStartFileContents += "];\n";

                // InBatchWithJob
                warmStartFileContents += "WsInBatchWithJob = [";
                foreach (int j in allJobs)
                {
                    int batch = int.Parse(batchForJob[j], DateTimeFormatInfo.InvariantInfo);
                    int machine = int.Parse(machineForJob[j], DateTimeFormatInfo.InvariantInfo);

                    int representativeJob = allJobs.Where(x =>
                        batch == int.Parse(batchForJob[x], DateTimeFormatInfo.InvariantInfo) &&
                        machine == int.Parse(machineForJob[x], DateTimeFormatInfo.InvariantInfo)).Min();
                    if (j == representativeJob)
                    {
                        warmStartFileContents += "0";
                    }
                    else
                    {
                        warmStartFileContents += representativeJob;
                    }
                    if (j != allJobs.Last())
                    {
                        warmStartFileContents += ",";
                    }
                }
                warmStartFileContents += "];\n";
            }
            return warmStartFileContents;
        }

        /// <summary>
        /// Create content of a MiniZinc weights file from weights of the objective function
        /// </summary>
        /// <param name="weights">The weights that should be used</param>
        /// <returns>Return the MiniZinc weights file contents as a string</returns>
        public string ConvertToMiniZincWeights(IWeightObjective weights)
        {
            string mznWeightsFileContents = "";

            mznWeightsFileContents += "int: alpha = " + weights.WeightRuntime 
                + "; % Weight of the objective component cumulative processing time of ovens\n";
            mznWeightsFileContents += "int: beta = " + weights.WeightSetupTimes 
                + "; % Weight of the objective component total setup times\n";
            mznWeightsFileContents += "int: gamma = " + weights.WeightSetupCosts 
                + "; % Weight of the objective component total setup costs\n";
            mznWeightsFileContents += "int: delta = " + weights.WeightTardiness 
                + "; % Weight of the objective component number of too late jobs\n";

            return mznWeightsFileContents;
        }

        /// <summary>
        /// Convert a given MiniZinc solution file into an OvenScheduling Object
        /// </summary>
        /// <param name="instance">Instance information associated to the solution</param>
        /// <param name="solutionFileContents">The contents of the solution file that should be parsed as a string</param>
        /// <returns>Creates output consisting of a list of converted batch assignments 
        /// (if no solution could be found, the output will be empty).</returns>
        public IOutput ConvertMiniZincSolutionFile(IInstance instance, string solutionFileContents)
        {
            DateTime creaTime = DateTime.Now;
            IList<int> machineForJob = new List<int>(); 
            IList<int> assignedBatchIds = new List<int>();
            IList<int> startTimes = new List<int>(); //start times in minutes
            IList<int> durations = new List<int>(); //durations in minutes
            IList<int> attributeIds = new List<int>();
            IList<SolutionType> solutionTypes = new List<SolutionType>();
            SolutionType solutionType = SolutionType.NoSolutionFound;

            //get parameters from instance
            int m = getEligibleMachines(instance.Jobs, instance.Machines).Count;
            int n = instance.Jobs.Count;

            //get functions that converts IDs to and from MiniZincId 
            Func<int, int> MachineIdFromMiniZincId = ConvertMinizincMachineIdToId(instance.Machines);
            Func<int, int> MiniZincIdFromJobId = ConvertJobIdToMinizinc(instance.Jobs);            
            Func<int, int> AttributeIdFromMiniZincId = ConvertMinizincAttributeIdToId(instance.Attributes);
            
            // parse MiniZinc solution
            string[] lines = solutionFileContents.Split(new[] { "\r\n", "\r", "\n", "\\n"}, StringSplitOptions.None);
            Array.Reverse(lines);
            foreach (string line in lines)
            {
                if (line.StartsWith("Attribute of batch:", StringComparison.InvariantCulture))
                {
                    int startIndex = line.IndexOf('[') + 1;
                    int endIndex = line.IndexOf(']') - 1;
                    string attributeIdString = line.Substring(startIndex, endIndex - startIndex + 1);

                    foreach (string id in attributeIdString.Split(','))
                    {
                        attributeIds.Add(int.Parse(id, CultureInfo.InvariantCulture));
                    }

                    //some solution has been found
                    solutionType = SolutionType.ValidSolutionFound;
                }
                if (line.StartsWith("Durations of batches are:", StringComparison.InvariantCulture))
                {
                    int startIndex = line.IndexOf('[') + 1;
                    int endIndex = line.IndexOf(']') - 1;
                    string durationsString = line.Substring(startIndex, endIndex - startIndex + 1);

                    foreach (string time in durationsString.Split(','))
                    {
                        durations.Add(int.Parse(time, CultureInfo.InvariantCulture));
                    }
                }
                if (line.StartsWith("Start times of batches are:", StringComparison.InvariantCulture))
                {
                    int startIndex = line.IndexOf('[') + 1;
                    int endIndex = line.IndexOf(']') - 1;
                    string startTimesString = line.Substring(startIndex, endIndex - startIndex + 1);

                    foreach (string time in startTimesString.Split(','))
                    {
                        startTimes.Add(int.Parse(time, CultureInfo.InvariantCulture));
                    }
                }
                if (line.StartsWith("Jobs are scheduled for following machines:", StringComparison.InvariantCulture))
                {
                    int startIndex = line.IndexOf('[') + 1;
                    int endIndex = line.IndexOf(']') - 1;
                    string machineForJobString = line.Substring(startIndex, endIndex - startIndex + 1);

                    foreach (string assignment in machineForJobString.Split(','))
                    {
                        machineForJob.Add(int.Parse(assignment, CultureInfo.InvariantCulture));
                    }
                }
                if (line.StartsWith("Jobs are scheduled for following batches:", StringComparison.InvariantCulture))
                {
                    int startIndex = line.IndexOf('[') + 1;
                    int endIndex = line.IndexOf(']') - 1;
                    string batchAssignmentString = line.Substring(startIndex, endIndex - startIndex + 1);

                    foreach (string assignment in batchAssignmentString.Split(','))
                    {
                        assignedBatchIds.Add(int.Parse(assignment, CultureInfo.InvariantCulture));
                    }
                    break;
                }
                
                
                
            }

            foreach (string line in lines)
            {
                if (line.StartsWith("==========", StringComparison.InvariantCulture))
                {
                    solutionType = SolutionType.OptimalSolutionFound;
                    break;
                }
                //if (line.Contains("UNSATISFIABLE", System.StringComparison.InvariantCulture))
                if (line.Contains("UNSATISFIABLE"))
                {
                    solutionType = SolutionType.Unsatisfiable;
                    break;
                }
                
            }

            solutionTypes.Add(solutionType);

                if (assignedBatchIds.Count == 0)
            {
                // if no assignments are found the problem is either unsatisfiable or no solution could be found within the time limit
                // an empty output is returned in this case
                return new Output();
            }

            //int batchCount = assignedBatchIds.Max();
            IList<IBatch> batches = new List<IBatch>();
            IList<IBatchAssignment> assignedBatches = new List<IBatchAssignment>();
            int batchCount = 1;
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    //consider i-th batch on machine j
                    if (durations[(j-1) * n + i - 1] != 0) //this is not an empty batch with duration=0
                    {
                        int machineId = MachineIdFromMiniZincId(j);
                        instance.Machines.TryGetValue(machineId, out IMachine machine);
                        DateTime startTime = instance.SchedulingHorizonStart.AddMinutes(startTimes[(j - 1) * n + i - 1]);
                        DateTime endTime = startTime.AddMinutes(durations[(j - 1) * n + i - 1]);
                        int attributeId = AttributeIdFromMiniZincId(attributeIds[(j - 1) * n + i - 1]);
                        instance.Attributes.TryGetValue(attributeId, out IAttribute attribute);
                        IBatch batch = new Batch(batchCount, machine, startTime, endTime, attribute);
                        batches.Add(batch);
                        foreach (IJob job in instance.Jobs)
                        {
                            if (assignedBatchIds[MiniZincIdFromJobId(job.Id) - 1] == i && machineForJob[MiniZincIdFromJobId(job.Id) - 1] == j)
                            {
                                assignedBatches.Add(new BatchAssignment(job, batch));
                            }
                        }
                        batchCount++;
                    }
                }
            }

            IOutput solution = new Output("Oven Scheduling Problem solution", DateTime.Now, assignedBatches, solutionTypes);

            return solution;
        }

        
    }
}
