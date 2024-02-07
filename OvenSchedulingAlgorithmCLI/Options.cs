using CommandLine;
using OvenSchedulingAlgorithm.InstanceGenerator;

namespace OvenSchedulingAlgorithmCLI
{
    public class Options
    {
        [Option('i', Default = "", HelpText = "Instance file", SetName = "solve")]
        public string InstanceFile { get; set; }

        [Option('g', Default = false, HelpText = "Use Greedy Heuristic", SetName = "solve")]
        public bool UseGreedyHeuristic { get; set; }   

        [Option('s', Default = "", HelpText = "Solution file", SetName = "solve")]
        public string SolutionFile { get; set; }

        [Option("nSer", Default = false, HelpText = "Do not serialize instance and solution", SetName = "solve")]
        public bool DoNotSerializeInstanceSolution { get; set; }

        [Option("wI", Default = "", HelpText = "Warm start instance file", SetName = "warmStart")]
        public string WarmStartInstanceFile { get; set; }

        [Option("wS", Default = "", HelpText = "Warm start solution file", SetName = "warmStart")]
        public string WarmStartSolutionFile { get; set; }

        [Option("wRepr", Default = false, HelpText = "Boolean indicating whether the warm start data is created for a" +
            " minizinc model with a representative job per batch", SetName = "warmStart")]
        public bool ReprJobModel { get; set; }        

        [Option('o', Default = "./", HelpText = "Output file location", SetName = "solve")]
        public string OutputFile { get; set; }

        [Option("valO", Default = false, HelpText = "Validate output", SetName = "solve")]
        public bool ValidateOutput { get; set; }

        [Option("logfile", Default = "logfile", HelpText = "Where to Store the output of the solution validation (filename without extension)", SetName = "solve")]
        public string LogFileName { get; set; }

        [Option("alpha", Default = 4, HelpText = "Weight used for the objective total oven runtime")]
        public int weightRunTime { get; set; }

        [Option("beta", Default = 0, HelpText = "Weight used for the objective total setup times")]
        public int weightSetupTimes { get; set; }

        [Option("gamma", Default = 1, HelpText = "Weight used for the objective total setup costs")]
        public int weightSetupCosts { get; set; }

        [Option("delta", Default = 0, HelpText = "Weight used for the objective number of tardy jobs")]
        public int weightTardiness { get; set; }


        [Option('c', Default = false, HelpText = "Convert to MiniZinc instance")]
        public bool ConvertInstanceToMiniZinc { get; set; }

        [Option("cNew", Default = false, HelpText = "Convert to MiniZinc or CP Optimizer instance in new format " +
            "(extra zeros setup) " + "without solving ")]
        public bool ConvertInstanceToMiniZincCpOptNew { get; set; }

        [Option("dznF", Default = "", HelpText = "Filename without ending of minizinc data file (dzn-format) or CPOptimizer data file (dat-format) " +
            "if instance is converted to MiniZinc or CPOptimizer instance without solving")]
        public string dznFileName { get; set; }

        [Option("cCP", Default = false, HelpText = "Convert to CPOptimizer instance")]
        public bool ConvertInstanceToCPOptimizer { get; set; }

        [Option('t', Default = 60000, HelpText = "Time Limit (in ms)", SetName = "solve")]
        public int TimeLimit { get; set; }

        [Option("iCheck", Default = false, HelpText = "Perform basic satisfiability check on instance", SetName = "solve")]
        public bool CheckInstance { get; set; }

        [Option("iCFile", Default = "", HelpText = "Path to file where result of basic satisfiability " +
            "check of instance should be stored", SetName = "solve")]
        public string InstanceCheckerFile { get; set; }  

        [Option('r', Default = 0, HelpText = "Generate random instance with this number of jobs", SetName = "generate")]
        public int RandomInstanceJobNumber { get; set; }

        [Option('m', Default = 2, HelpText = "Number of machines in random instance", SetName = "generate")]
        public int MachineNumber { get; set; }

        [Option('a', Default = 2, HelpText = "Number of attributes in random instance", SetName = "generate")]
        public int AttributeNumber { get; set; }
                     

        [Option("omt", Default = 60, HelpText = "Maximum processing time for any job (in minutes)", SetName = "generate")]
        public int OverallMaxTime { get; set; }

        [Option("dt", Default = 2, HelpText = "Number of different processing times among which to choose", SetName = "generate")]
        public int DiffTimes { get; set; }

        [Option("mt", Default = false, HelpText = "Whether a maximum processing time should be generated for every job or not. " +
            "If not, max_time will be equal to the overall maximum processing time.", SetName = "generate")]
        public bool MaxTime { get; set; }

        [Option("ms", Default = 5, HelpText = "Maximum size of a job", SetName = "generate")]
        public int MaxSize { get; set; }

        [Option("max_cap_l", Default = 5, HelpText = "Lower bound for the maximum capacity of machines", SetName = "generate")]
        public int MaxCapLowerBound { get; set; }

        [Option("max_cap_u", Default = 10, HelpText = "Upper bound for the maximum capacity of machines", SetName = "generate")]
        public int MaxCapUpperBound { get; set; }

        [Option("minSC", Default = 1, HelpText = "Minimum number of shifts that are generated per machine", SetName = "generate")]
        public int MinShiftCount { get; set; }

        [Option("maxSC", Default = 2, HelpText = "Maximum number of shifts that are generated per machine", SetName = "generate")]
        public int MaxShiftCount { get; set; }

        [Option("avP", Default = 0.5, HelpText = "Lower bound for the fraction of time that every machine should be available. Between 0 and 1.", SetName = "generate")]
        public double AvailabilityPercentage { get; set; }

        [Option("elP", Default = 0.5, HelpText = "Probability of an additional machine to be selected as eligible machine for a job (one machine will always be selected)." +
            " Between 0 and 1.", SetName = "generate")]
        public double EligibilityProba { get; set; }

        [Option("eSDf", Default = 0.5, HelpText = "Factor used to create earliest start date. Should be between 0 and 1. " +
            "The larger the factor is, the more the earliest start dates are spread out over the scheduling horizon.", SetName = "generate")]
        public double EarliestStartDateFactor { get; set; }

        [Option("lEDf", Default = 2, HelpText = "Factor used to create latest end date. Should be larger or equal to 1. " +
            "If equal to 1, all jobs must be processed immediately. The larger the factor gets, the more time there is for every job.", SetName = "generate")]
        public double LatestEndDateFactor { get; set; }

        [Option("sC", Default = SetupType.none, HelpText = "Type of setup costs. Can be one of: none, constant, arbitrary, realistic, symmetric.", SetName = "generate")]
        public SetupType setupCosts { get; set; }

        [Option("sT", Default = SetupType.none, HelpText = "Type of setup times.  Can be one of: none, constant, arbitrary, realistic, symmetric.", SetName = "generate")]
        public SetupType setupTimes { get; set; }

        [Option("sgreedy", Default = false, HelpText = "Boolean indicating whether instances that cannot be solved by " +
            "greedy heuristic should be thrown away. (Ie, only instances " +
            "were all jobs can be assigned by greedy heuristic are accepted as random instances)", SetName = "generate")]
        public bool SolvableByGreedyOnly { get; set; }

        [Option("diffAtt", Default = false, HelpText = "Boolean indicating whether jobs may have different attributes " +
            "on different eligible machines. If false, attributes of a job are the same on all eligible machines.", SetName = "generate")]
        public bool DifferentAttributesPerMachine { get; set; }

        [Option("initStates", Default = false, HelpText = "Boolean indicating whether initial states should be generated for all machines.", SetName = "generate")]
        public bool InitialStates { get; set; }

        //TODO implement
        [Option('x', Default = false, HelpText = "Parse MiniZinc solution and convert to .json solution")]
        public bool ParseMiniZincSolution { get; set; }

        //TODO disable for time-being
        [Option("lB", Default = false, HelpText = "Calculate lower bounds for instance (do no solve)", SetName = "solve")]
        public bool CalculateLowerBounds { get; set; }

        [Option("specialLexW", Default = false, HelpText = "In MiniZinc converter (both for mzn and CPOptimizer), create weights for UC2, ie, the special case of lexicographic minimization " +
            "with total oven runtime lexicographically more important than tardiness," +
            "tardiness lexicographically more important than setup costs.", SetName = "solve")]
        public bool SpecialCaseLexicographicWeights { get; set; }

        [Option("valSpecialLexW", Default = false, HelpText = "Do validation of solution with weights for UC2, ie, the special case of lexicographic minimization " +
            "with total oven runtime lexicographically more important than tardiness," +
            "tardiness lexicographically more important than setup costs.", SetName = "solve")]
        public bool ValidateSpecialCaseLexicographicWeights { get; set; }
    }
}
