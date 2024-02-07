using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace OvenSchedulingAlgorithm.Interface.Implementation
{
    /// <summary>
    /// An output of the oven scheduling algorithm
    /// </summary>
    public class Output : IOutput
    {
        /// <summary>
        /// The name of the output
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The date the output was created
        /// </summary>
        public DateTime CreationDate { get; }

        /// <summary>
        /// The list of batch assignments that the algorithm generated
        /// </summary>
        public IList<IBatchAssignment> BatchAssignments { get; }

        /// <summary>
        /// The list of solution types 
        /// (if more than one entry: the instance has been solved using the SplitAndSolve algorithm 
        /// and solution types are given for every short intervall)
        /// </summary>
        public IList<SolutionType> SolutionTypes { get; }

        /// <summary>
        /// Creates the list of batches that the algorithm generated from the list of batch assignments
        /// </summary>        
        /// <returns>List of batches.</returns>
        public IList<IBatch> GetBatches()
        {
            IList<IBatch> batches = new List<IBatch>();

            foreach (IBatchAssignment assignment in BatchAssignments)
            {
                bool containsBatch = batches.Any(x => x.IsEqual(assignment.AssignedBatch));

                if (!containsBatch)
                {
                    batches.Add(assignment.AssignedBatch);
                }
            }           

            return batches;
        }

        /// <summary>
        /// Creates the dictionary of batches that the algorithm generated from the list of batches
        /// </summary>
        /// <returns>The dictionary of batches, keys are (machineId, batch position = batchId).</returns>
        public IDictionary<(int mach, int pos), IBatch>  GetBatchDictionary()
        {
            IList<IBatch> batches = GetBatches();

            //create dictionary of list of batches per machine
            IDictionary<int, List<IBatch>> batchesOnMachine = new Dictionary<int, List<IBatch>>();
            foreach (IBatch batch in batches)
            {
                int machineId = batch.AssignedMachine.Id;

                //there is no entry for this machine in the dictionary yet
                if (!batchesOnMachine.ContainsKey(machineId))
                {
                    batchesOnMachine.Add(machineId, new List<IBatch>());
                }
                batchesOnMachine[machineId].Add(batch);
            }

            //create batch dictionary 
            IDictionary<(int mach, int pos), IBatch> batchDictionary = new Dictionary<(int mach, int pos), IBatch>();


            foreach (int machineId in batchesOnMachine.Keys)
            {
                int batchPos = 1;

                //for every machine, sort list of batches by starting time
                batchesOnMachine[machineId]
                    .Sort((x, y) => x.StartTime.CompareTo(y.StartTime));

                //run through sorted list and create entry in batchDictionary
                for (int i = 0; i < batchesOnMachine[machineId].Count; i++)
                {
                    batchDictionary.Add((machineId, batchPos), batchesOnMachine[machineId][i]);
                    batchPos ++;
                }

            }

            return batchDictionary;


        }

        /// <summary>
        /// Creates a dictionary of setup times and costs before batches,
        /// for a given instance and a given dictionary of batches.
        /// The keys are (machineId, batch position = batchId) 
        /// </summary>   
        /// <param name="instance">the given instance</param>
        /// <param name="batchDictionary">the given dictionary of batches</param> 
        /// <returns>Dictionary of setup times and costs before batches.</returns>
        public static IDictionary<(int mach, int pos), (int setupTime, int setupCost)>
            GetSetupTimesAndCostsDictionary(IInstance instance,
            IDictionary<(int mach, int pos), IBatch> batchDictionary)
        {
            IDictionary<(int mach, int pos), (int setupTime, int setupCost)> setupTimesAndCostsDictionary
                = new Dictionary<(int mach, int pos), (int setupTime, int setupCost)>();

            int n = instance.Jobs.Count; //number of jobs
            int m = instance.Machines.Count;

            //sorted list of attribute IDs
            List<int> attributeIds = instance.Attributes.Keys.ToList();
            attributeIds.Sort();
            //create dictionary of attribute Ids: key is attribute id, value is position in sorted list
            Dictionary<int, int> sortedAttributes = new Dictionary<int, int>();
            for (int i = 0; i < attributeIds.Count; i++)
            {
                int attributeId = attributeIds[i];
                sortedAttributes.Add(attributeId, i);
            }

            foreach (int machine in instance.Machines.Keys)
            {
                int batchOnMachineCount = batchDictionary.Keys.Where(k => k.mach == machine).Count();

                if (batchOnMachineCount == 0)
                {
                    continue;
                }

                //initial setup before the first batch on machine
                if (instance.InitialStates != null)
                {
                    //we assume that the machine IDs are 1, 2, ..., m
                    IAttribute initialAttribute = instance.Attributes[instance.InitialStates[machine]];
                    int firstAttributeId = batchDictionary[(machine, 1)].Attribute.Id; //attribute Id of first batch
                    int firstAttributePos = sortedAttributes[firstAttributeId]; //position of this attribute Id in sorted list of attributes
                    int setupTime = initialAttribute.SetupTimesAttribute[firstAttributePos];
                    int setupCost = initialAttribute.SetupCostsAttribute[firstAttributePos];
                    setupTimesAndCostsDictionary.Add((machine, 1), (setupTime, setupCost));
                }
                else
                {
                    setupTimesAndCostsDictionary.Add((machine, 1), (0, 0));
                }

                //setup before other batches on this machine
                for (int batchPos = 1; batchPos < batchOnMachineCount; batchPos++)
                {
                    IAttribute batchAttribute = batchDictionary[(machine, batchPos)].Attribute;
                    int nextAttributeId = batchDictionary[(machine, batchPos + 1)].Attribute.Id; //attribute Id of next batch
                    int nextAttributePos = sortedAttributes[nextAttributeId]; //position of this attribute Id in sorted list of attributes
                    int setupTime = batchAttribute.SetupTimesAttribute[nextAttributePos];
                    int setupCost = batchAttribute.SetupCostsAttribute[nextAttributePos];
                    setupTimesAndCostsDictionary.Add((machine, batchPos + 1), (setupTime, setupCost));
                }
            }

            return setupTimesAndCostsDictionary;
        }

        /// <summary>
        /// Create an output of the oven scheduling algorithm
        /// </summary>
        /// <param name="name">The name of the output</param>
        /// <param name="creationDate">The date the output was created</param>
        /// <param name="batchAssignments">The list of batch assignments that the algorithm generated</param>
        /// <param name="solutionTypes">The list of solution types</param>
        [JsonConstructor]

        public Output(string name, DateTime creationDate, IList<IBatchAssignment> batchAssignments, IList<SolutionType> solutionTypes)
        {
            Name = name;
            CreationDate = creationDate;
            BatchAssignments = batchAssignments;
            SolutionTypes = solutionTypes;
        }

        /// <summary>
        /// Create an empty output of the oven scheduling algorithm
        /// </summary>
        public Output()
        {
            Name = "Empty Oven Scheduling Problem solution";
            CreationDate = DateTime.Now;
            BatchAssignments = new List<IBatchAssignment>();

            IList<SolutionType> solutionTypes = new List<SolutionType>();
            solutionTypes.Add(SolutionType.NoSolutionFound);
            SolutionTypes = solutionTypes;
        }

        /// <summary>
        /// Serialize the solution to a json file
        /// </summary>
        /// <param name="fileName">Location of the serialized filed</param>
        public void Serialize(string fileName)
        {
            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore

            };

            StreamWriter sw = new StreamWriter(fileName.Replace(':', '-'));
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, this, typeof(IOutput));
            }
            //Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
            //serializer.Converters.Add(new Newtonsoft.Json.Converters.JavaScriptDateTimeConverter());
            //serializer.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            //serializer.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto;
            //serializer.Formatting = Newtonsoft.Json.Formatting.Indented;

            //using (StreamWriter sw = new StreamWriter(fileName))
            //using (Newtonsoft.Json.JsonWriter writer = new Newtonsoft.Json.JsonTextWriter(sw))
            //{
            //    serializer.Serialize(writer, obj, typeof(MyDocumentType));
            //}
        }

        /// <summary>
        /// Create a solution based on a serialized Object
        /// </summary>
        /// <param name="fileName">File location storing the serialized solution.</param>
        public static IOutput DeserializeSolution(string fileName)
        {
            // deserialize JSON directly from a file
            StreamReader streamReader = File.OpenText(fileName);

            //JsonSerializer serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Objects };
            JsonSerializer serializer = new JsonSerializer 
            { 
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore
            };
            Output output =
                (Output)serializer.Deserialize(streamReader, typeof(IOutput));
            streamReader.Close();

            return output;
        }
    }
}