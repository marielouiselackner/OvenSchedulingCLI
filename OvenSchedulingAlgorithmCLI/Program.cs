using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CommandLine;
using MCP.MetaheuristicsFramework.Types.Impl;
using OvenSchedulingAlgorithm.Algorithm;
using OvenSchedulingAlgorithm.InstanceGenerator;
using OvenSchedulingAlgorithm.Interface;
using OvenSchedulingAlgorithm.Interface.Implementation;
using OvenSchedulingAlgorithm.Converter;
using OvenSchedulingAlgorithm.Converter.Implementation;
using OvenSchedulingAlgorithmCLI.Util;
using OvenSchedulingAlgorithm.InstanceChecker;
using OvenSchedulingAlgorithm.Objective.Implementation;
using System.Threading;

namespace OvenSchedulingAlgorithmCLI
{
    //public class Program
    internal class Program
    {

        /// <summary>
        /// This method will handle the CTRL+C event and stop the algorithm
        /// </summary>
        public static void CancelKeyHandler(object sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;
            Console.WriteLine("Stopping algorithm...");
        }

        static void Main(string[] args)
        {        
            if (args.Length == 0) 
            {
                Console.WriteLine("No parameter given. At least, path to instance file or number of jobs for creation of random instance needs to be provided.");
                Thread.Sleep(1000);
                args = ["--help"];
            }
            // Parse and process command line arguments
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunProgram)
                .WithNotParsed(HandleParseError);                   
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
        }

        private static void RunProgram(Options opts)
        {
            string logfile = string.IsNullOrEmpty(opts.InstanceFile) ? 
                "localsearchLogfile" 
                : opts.InstanceFile.Replace("json", "").Replace("New", "" ) + "solution" + DateTime.Now.ToString() + ".localsearchlog";
            //logger is currently only used for local search algorithms (sim annealing) from metaheuristics framework
            //Logging.SetupLogger(3, true, logfile);
            Logging.SetupLogger(1, true, logfile);

            IInstance instance = new Instance("", new DateTime(), new Dictionary<int, IMachine>(), new Dictionary<int, int>(), 
                new List<IJob>(), new Dictionary<int, IAttribute>(), new DateTime(), new DateTime());

            IWeightObjective weights = new WeightObjective(opts.weightRunTime, opts.weightSetupTimes, opts.weightSetupCosts,
                    opts.weightTardiness);

            if (!string.IsNullOrEmpty(opts.WarmStartInstanceFile) && !string.IsNullOrEmpty(opts.WarmStartSolutionFile))
            {
                //create minizinc warm start data file 
                IMiniZincConverter miniZincConverter = new MiniZincConverter();

                // parse instance object from file 
                IInstance warmStartInstance = Instance.DeserializeInstance(opts.WarmStartInstanceFile);
                IOutput warmStartInitialSolution = Output.DeserializeSolution(opts.WarmStartSolutionFile);

                string warmStartMznInput = miniZincConverter.ConvertToMiniZincWarmStartData
                    (warmStartInstance, warmStartInitialSolution, opts.ReprJobModel);

                string instanceFileNameForWarmStartFile = opts.WarmStartInstanceFile;
                //remove extension and directory path
                int index = instanceFileNameForWarmStartFile.LastIndexOf("/");                
                if (index > 0)
                {
                    instanceFileNameForWarmStartFile = instanceFileNameForWarmStartFile.Substring(index+1);
                }
                index = instanceFileNameForWarmStartFile.LastIndexOf("\\");
                if (index > 0)
                {
                    instanceFileNameForWarmStartFile = instanceFileNameForWarmStartFile.Substring(index + 1);
                }
                index = instanceFileNameForWarmStartFile.LastIndexOf(".json");
                if (index > 0)
                {
                    instanceFileNameForWarmStartFile = instanceFileNameForWarmStartFile.Substring(0, index);
                }

                string warmStartFilename = "./warm_start_" + instanceFileNameForWarmStartFile + ".dzn";
                File.WriteAllText(warmStartFilename, warmStartMznInput);

                return;
            }

            //create random instance
            if ((opts.RandomInstanceJobNumber != 0 | string.IsNullOrEmpty(opts.InstanceFile))
                && (string.IsNullOrEmpty(opts.WarmStartInstanceFile) | string.IsNullOrEmpty(opts.WarmStartSolutionFile)))
            {
                RandomInstanceGenerator instanceGenerator = new RandomInstanceGenerator();

                RandomInstanceParameters parameters = new RandomInstanceParameters(opts.RandomInstanceJobNumber, opts.MachineNumber, opts.AttributeNumber,
                opts.OverallMaxTime, opts.DiffTimes, opts.MaxTime, opts.MaxSize, opts.MaxCapLowerBound, opts.MaxCapUpperBound,
                opts.MinShiftCount, opts.MaxShiftCount, opts.AvailabilityPercentage, opts.EligibilityProba,
                opts.EarliestStartDateFactor, opts.LatestEndDateFactor, opts.setupCosts, opts.setupTimes, opts.SolvableByGreedyOnly,
                opts.DifferentAttributesPerMachine, opts.InitialStates);

                instance = instanceGenerator.GenerateInstance(parameters);
                
            }
            


            //algorithm configuration
            //where to store the solution
            string outputFileLocation = Path.GetDirectoryName(opts.InstanceFile) + "/";

            if (!string.IsNullOrEmpty(opts.OutputFile))
            {
                outputFileLocation = opts.OutputFile;
            }

            IAlgorithmConfig config = new AlgorithmConfig(opts.TimeLimit, !opts.DoNotSerializeInstanceSolution, outputFileLocation, weights);

            //TODO: either convert or run greedy
            if (opts.ConvertInstanceToMiniZinc || opts.ConvertInstanceToCPOptimizer 
                || opts.ConvertInstanceToMiniZincCpOptNew)
            {
                // convert to mzn instance
                IMiniZincConverter miniZincConverter = new MiniZincConverter();

                // convert Instance to Minizinc/Cp Optimizer format
                string instanceFileContents = miniZincConverter.ConvertToMiniZincInstance(instance, weights,  
                    opts.ConvertInstanceToCPOptimizer, opts.ConvertInstanceToMiniZincCpOptNew, opts.SpecialCaseLexicographicWeights);
         
                // write instance file
                string InstanceFileName = "";
                if (opts.ConvertInstanceToCPOptimizer)
                //in CPOptimizer format
                {
                    if (string.IsNullOrEmpty(opts.dznFileName))
                    {
                        string nowTime = DateTime.Now.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture);
                        InstanceFileName += "oven_scheduling.cpoptimizer_instance_" + nowTime + ".dat";
                    }
                    else
                    {
                        InstanceFileName += opts.dznFileName + ".dat";
                    }
                }
                else 
                //in Minizinc format
                {
                    if (string.IsNullOrEmpty(opts.dznFileName))
                    {
                        string nowTime = DateTime.Now.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture);
                        InstanceFileName += "oven_scheduling.minizinc_instance_" + nowTime + ".dzn";
                    }
                    else
                    {
                        InstanceFileName += opts.dznFileName + ".dzn";
                    }
                }                
                
                File.WriteAllText(InstanceFileName, instanceFileContents);

                //convert only, do not proceed to solving
                return;
            }              


            if (opts.CalculateLowerBounds)
            {
                DateTime startLowerBounds = DateTime.Now;

                InstanceData instanceData = Preprocessor.DoPreprocessing(
                    instance, 
                    weights);                

                LowerBounds lb = LowerBoundsCalculator.CalculateLowerBounds(instance, instanceData, config);
                

                DateTime endLowerBounds = DateTime.Now;
                TimeSpan runtimeLowerBounds = endLowerBounds - startLowerBounds;
                //write runtime info to file 
                string logFileName = "runtimeCalculationLowerBounds" + instance.Name + "-" + instance.CreationDate.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture) + ".txt";
                string runtime = "time required to calculate lower bounds: " + runtimeLowerBounds.ToString("c") + " (hh:mm:ss.xxxxxxx);";

                File.WriteAllText(logFileName.Replace(':', '-'), runtime);

                //add runtime info
                lb.LowerBoundsCalculationTime = runtimeLowerBounds;
                lb.Serialize("Lower bounds for instance "
                        + instance.Name.Replace(':', '-')
                        + ".json");

                //calculate lower bounds only, do not proceed to solving
                return;
            }

            if (!string.IsNullOrEmpty(opts.InstanceCheckerFile) || opts.CheckInstance)
            {
                //perform basic satisfiability check on instance
                //bool passedSatisfiabilityTest = 
                BasicSatisfiabilityChecker.CheckSatisfiability(instance, opts.InstanceCheckerFile);

                //check satisfiability only, do not proceed to solving
                return;
            }

            if (!string.IsNullOrEmpty(opts.InstanceFile) && string.IsNullOrEmpty(opts.SolutionFile) && opts.UseGreedyHeuristic)
                {
                    //
                    // solve the instance 
                    //

                    SolveInstanceGreedy(instance, opts, config);
                }
            else if (!string.IsNullOrEmpty(opts.SolutionFile))
                {
                //
                // validate solution against instance
                //

                // parse solution
                IOutput solution = Output.DeserializeSolution(opts.SolutionFile);

                Console.WriteLine("----------------------------");

                Console.WriteLine("Validating Oven Scheduling solution \n{0}\n for instance \n{1}\n",opts.SolutionFile, opts.InstanceFile);

                SolutionValidator validator = new SolutionValidator(instance, solution, opts.LogFileName, config);
                validator.ValidateSolution();

                Console.WriteLine("----------------------------");
                }              
            
        }

        private static void SolveInstanceGreedy(IInstance instance, Options opts, IAlgorithmConfig config)
        {
            

            // solve the instance
            //
            IAlgorithmFactory factory = new AlgorithmFactory();
            IAlgorithm algorithm = factory.GetSimpleGreedyAlgorithm(); 

            IOutput solution = algorithm.Solve(instance,config); 
            
            if (opts.ValidateOutput)
            {
                if (opts.ValidateSpecialCaseLexicographicWeights)
                {
                    InstanceData instanceData = Preprocessor.DoPreprocessing(instance, new WeightObjective(1,1,1,1));
                    //calculation of weights and upper bound
                    int weightSetupCosts = 1;
                    int weightTardiness = instanceData.NumberOfJobs * instanceData.MaxSetupCost + 1;
                    int weightRuntime = weightTardiness * (instanceData.NumberOfJobs + 1);
                    //upperBound = weightSetupCosts * n * instanceData.MaxSetupCost + weightTardiness * n  + weightRuntime * upperBound(runtime) 
                    int upper_bound_obj = weightTardiness - 1
                        + weightTardiness * instanceData.NumberOfJobs
                        + weightRuntime * instanceData.UpperBoundTotalRuntimeMinutes;

                    IWeightObjective weights = new WeightObjective(weightRuntime, 0, weightSetupCosts,
                    weightTardiness);
                    config = new AlgorithmConfig(config.RunTimeLimit, config.SerializeInputOutput, config.SerializeOutputDestination, weights);
                    SolutionValidator validator = new SolutionValidator(instance, solution, opts.LogFileName, config);
                    validator.ValidateSolutionLexMinSpecialCase(upper_bound_obj);

                }
                else
                {
                    SolutionValidator validator = new SolutionValidator(instance, solution, opts.LogFileName, config);
                    validator.ValidateSolution();
                }
                
            }
            
        }
    }
}
