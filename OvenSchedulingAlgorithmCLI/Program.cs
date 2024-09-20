using CommandLine;
using OvenSchedulingAlgorithm.Algorithm;
using OvenSchedulingAlgorithm.Converter;
using OvenSchedulingAlgorithm.Converter.Implementation;
using OvenSchedulingAlgorithm.InstanceChecker;
using OvenSchedulingAlgorithm.InstanceGenerator;
using OvenSchedulingAlgorithm.Interface;
using OvenSchedulingAlgorithm.Interface.Implementation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

            IInstance instance = new Instance("", new DateTime(), new Dictionary<int, IMachine>(), new Dictionary<int, int>(), 
                new List<IJob>(), new Dictionary<int, IAttribute>(), new DateTime(), new DateTime());

            IWeightObjective weights = new WeightObjective(opts.weightRunTime, opts.weightSetupTimes, opts.weightSetupCosts,
                    opts.weightTardiness);

            if (!string.IsNullOrEmpty(opts.WarmStartInstanceFile) && !string.IsNullOrEmpty(opts.WarmStartSolutionFile))
            {
                Console.WriteLine("----------------------\nCreate warmstart file.");

                //create warm start data file 
                IMiniZincConverter miniZincConverter = new MiniZincConverter();

                // parse instance object from file 
                IInstance warmStartInstance = Instance.DeserializeInstance(opts.WarmStartInstanceFile);
                IOutput warmStartInitialSolution = Output.DeserializeSolution(opts.WarmStartSolutionFile);

                string warmStartInput = miniZincConverter.ConvertToMiniZincWarmStartData
                    (warmStartInstance, warmStartInitialSolution, opts.ReprJobModel, opts.ConvertInstanceToCPOptimizer);

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

                string warmStartFilename = "./warm_start_" + instanceFileNameForWarmStartFile;
                if (opts.ConvertInstanceToCPOptimizer)
                {
                    warmStartFilename += ".dat";
                }
                else
                {
                    warmStartFilename += ".dzn";
                }
                
                File.WriteAllText(warmStartFilename, warmStartInput);
                return;
            }

            if (!string.IsNullOrEmpty(opts.InstanceFile))
            {
                // parse instance object from file 
                instance = Instance.DeserializeInstance(opts.InstanceFile);
            }
            else if (opts.ConvertMiniZincInstanceToJson)
            {
                //read minizinc instance file content and create Instance object
                string instanceFileContents = File.ReadAllText(opts.dznFileName + ".dzn");
                IMiniZincConverter miniZincConverter = new MiniZincConverter();
                IInstance convertedInstance = miniZincConverter.ConvertMiniZincInstanceToInstanceObject(instanceFileContents);

                //serialize
                convertedInstance.Serialize(opts.dznFileName + ".json");
            }
            //create random instance
            else if ((opts.RandomInstanceJobNumber != 0 | string.IsNullOrEmpty(opts.InstanceFile))
                && (string.IsNullOrEmpty(opts.WarmStartInstanceFile) | string.IsNullOrEmpty(opts.WarmStartSolutionFile)))
            {
                Console.WriteLine("----------------------\nCreate random instance with {0} jobs, {1} machines and {2} attributes.", opts.RandomInstanceJobNumber, opts.MachineNumber, opts.AttributeNumber);
                RandomInstanceGenerator instanceGenerator = new RandomInstanceGenerator();

                RandomInstanceParameters parameters = new RandomInstanceParameters(opts.RandomInstanceJobNumber, opts.MachineNumber, opts.AttributeNumber,
                opts.OverallMaxTime, opts.DiffTimes, opts.MaxTime, opts.MaxSize, opts.MaxCapLowerBound, opts.MaxCapUpperBound,
                opts.MinShiftCount, opts.MaxShiftCount, opts.AvailabilityPercentage, opts.EligibilityProba,
                opts.EarliestStartDateFactor, opts.LatestEndDateFactor, opts.setupCosts, opts.setupTimes, opts.SolvableByGreedyOnly,
                false, true);

                instance = instanceGenerator.GenerateInstance(parameters, opts.FileNameGreedySolRandom);
                
            }

            if (!string.IsNullOrEmpty(opts.MiniZincSolutionFile) && !string.IsNullOrEmpty(opts.InstanceFile))
            {
                Console.WriteLine("----------------------\nConvert and serialise minzinc solution for given instance.");

                // read solution file text
                string solutionFileContents = File.ReadAllText(opts.MiniZincSolutionFile);
                IMiniZincConverter miniZincConverter = new MiniZincConverter();
                IOutput output = miniZincConverter.ConvertMiniZincSolutionFile(instance, solutionFileContents);
 
                //serialise output
                string solutionFileName = "";
                if (string.IsNullOrEmpty(opts.JsonSolutionFileName))
                {
                    string nowTime = DateTime.Now.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture);
                    solutionFileName += "oven_scheduling_converted_solution" + instance.Name + nowTime + ".json";
                }
                else
                {
                    solutionFileName += opts.JsonSolutionFileName + ".json";
                }
                output.Serialize(solutionFileName.Replace(':', '-'));
                return;

            }


            if (opts.ConvertInstanceToMiniZinc || opts.ConvertInstanceToCPOptimizer)
            {
                // convert to mzn instance
                IMiniZincConverter miniZincConverter = new MiniZincConverter();

                // convert Instance to Minizinc/Cp Optimizer format
                string instanceFileContents = miniZincConverter.ConvertToMiniZincInstance(instance, weights,  
                    opts.ConvertInstanceToCPOptimizer, opts.SpecialCaseLexicographicWeights);
         
                // write instance file
                string InstanceFileName = "";
                if (opts.ConvertInstanceToCPOptimizer)
                //in CPOptimizer format
                {
                    Console.WriteLine("----------------------\nConvert instance to CP Optimizer .dat file.");
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
                    Console.WriteLine("----------------------\nConvert instance to MiniZinc .dzn file.");
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
            }

            //algorithm configuration
            //where to store the solution
            string outputFileLocation = Path.GetDirectoryName(opts.InstanceFile) + "/";

            if (!string.IsNullOrEmpty(opts.OutputFile))
            {
                outputFileLocation = opts.OutputFile;
            }
            //timelimit is not used for the implemented procedures
            IAlgorithmConfig config = new AlgorithmConfig(1, !opts.DoNotSerializeInstanceSolution, outputFileLocation, weights);

            if (opts.CalculateLowerBounds)
            {
                Console.WriteLine("----------------------\nCalculating problem-specific lower bounds for instance.");
                DateTime startLowerBounds = DateTime.Now;

                InstanceData instanceData = Preprocessor.DoPreprocessing(
                    instance,
                    weights);

                LowerBounds lb = LowerBoundsCalculator.CalculateLowerBounds(instance, instanceData, config);


                DateTime endLowerBounds = DateTime.Now;
                TimeSpan runtimeLowerBounds = endLowerBounds - startLowerBounds;
                //write runtime info to file 
                //string logFileName = "runtimeCalculationLowerBounds" + instance.Name + "-" + instance.CreationDate.ToString("ddMM-HH.mm.ss", CultureInfo.InvariantCulture) + ".txt";
                //string runtime = "time required to calculate lower bounds: " + runtimeLowerBounds.ToString("c") + " (hh:mm:ss.xxxxxxx);";
                // File.WriteAllText(logFileName.Replace(':', '-'), runtime);

                //add runtime info
                lb.LowerBoundsCalculationTime = runtimeLowerBounds;
                string lowerBoundsFilename; 
                if (string.IsNullOrEmpty(opts.lBFileName))
                {
                    lowerBoundsFilename = "Lower bounds for instance "
                        + instance.Name.Replace(':', '-')
                        + ".json";
                }
                else
                {
                    lowerBoundsFilename = opts.lBFileName + ".json";
                }
                lb.Serialize(lowerBoundsFilename);
            }

            if (opts.CalculateInstanceParams)
            {
                Console.WriteLine("----------------------\nCalculating parameters of instance.");

                RandomInstanceParameters actualParameters = RandomInstanceParameters.GetParametersInstance(instance);

                string instanceParametersFilename;
                if (string.IsNullOrEmpty(opts.lBFileName))
                {
                    instanceParametersFilename = "ActualParameters-" + instance.Name + ".json";
                }
                else
                {
                    instanceParametersFilename = opts.InstanceParamsFileName + ".json";
                }
                actualParameters.Serialize(instanceParametersFilename);
            }

            if (!string.IsNullOrEmpty(opts.InstanceCheckerFile) || opts.CheckInstance)
            {
                //perform basic satisfiability check on instance
                //bool passedSatisfiabilityTest = 
                BasicSatisfiabilityChecker.CheckSatisfiability(instance, opts.InstanceCheckerFile);
            }          
            

            if (string.IsNullOrEmpty(opts.SolutionFile) && opts.UseGreedyHeuristic)
            {
            //
            // solve the instance 
            //
            Console.WriteLine("----------------------\nSolving instance with greedy construction heuristic.");

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
                Console.WriteLine("----------------------\nValidate solution for given instance.");

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
