using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OvenSchedulingAlgorithm.Interface.Implementation
{
    /// <summary>
    /// An instance for the oven scheduling algorithm
    /// </summary>
    public class Instance : IInstance
    {
        /// <summary>
        /// The name of the instance
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The time when the instance was created
        /// </summary>
        public DateTime CreationDate { get; }

        /// <summary>
        /// The dictionary of machines
        /// </summary>
        public IDictionary<int, IMachine> Machines { get; }

        /// <summary>
        /// The dictionary of initial state IDs for every machine; the keys are the machine IDs
        /// </summary>
        public IDictionary<int, int> InitialStates { get; }

        /// <summary>
        /// The list of jobs 
        /// </summary>
        public IList<IJob> Jobs { get; }

        /// <summary>
        /// The dictionary of attributes, keys are attribute IDs
        /// </summary>
        public IDictionary<int,IAttribute> Attributes { get; }

        /// <summary>
        /// The start of the scheduling horizon as a reference date
        /// </summary>
        public DateTime SchedulingHorizonStart { get; }

        /// <summary>
        /// The end of the scheduling horizon 
        /// </summary>
        public DateTime SchedulingHorizonEnd { get; }

        /// <summary>
        /// Create an instance for the oven scheduling algorithm
        /// </summary>
        /// <param name="name">The name of the instance</param>
        /// <param name="creationDate">The time when the instance was created</param>
        /// <param name="machines">The dictionary of machines</param>
        /// <param name="initialStates">The dictionary of initial states of machines</param>
        /// <param name="jobs">The list of jobs</param>
        /// <param name="attributes">The dictionary of attributes</param>
        /// <param name="schedulingHorizonStart">The start of the scheduling horizon as a reference date</param>
        /// <param name="schedulingHorizonEnd">The end of the scheduling horizon</param>
        [JsonConstructor]
        public Instance(
            string name,
            DateTime creationDate,
            IDictionary<int, IMachine> machines,
            IDictionary<int, int> initialStates,
            IList<IJob> jobs,
            IDictionary<int,IAttribute> attributes,
            DateTime schedulingHorizonStart,
            DateTime schedulingHorizonEnd)
        {
            Name = name;
            CreationDate = creationDate;
            Machines = machines;
            InitialStates = initialStates;
            Jobs = jobs;
            Attributes = attributes;
            SchedulingHorizonStart = schedulingHorizonStart;
            SchedulingHorizonEnd = schedulingHorizonEnd;

            foreach (IJob job in Jobs)
            {
                foreach (int machine in job.EligibleMachines)
                {
                    if (!Machines.ContainsKey(machine))
                    {
                        throw new ArgumentException("No machine with following id" + machine);
                    }
                    if (!Attributes.ContainsKey(job.AttributeIdPerMachine[machine]))
                    {
                        throw new ArgumentException("No attribute with following id" + job.AttributeIdPerMachine[machine]);
                    }
                }

            }
        }

        /// <summary>
        /// Serialize the instance to a json file
        /// </summary>
        /// <param name="fileName">Location of the serialized filed</param>
        public void Serialize(string fileName)
        {
            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Objects
            };

            StreamWriter sw = new StreamWriter(fileName);
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, this);
            }
        }

        /// <summary>
        /// Create an instance based on a serialized Object
        /// </summary>
        /// <param name="fileName">File location storing the serialized instance.</param>
        public static IInstance DeserializeInstance(string fileName)
        {
            // deserialize JSON directly from a file
            StreamReader streamReader = File.OpenText(fileName);

            JsonSerializer serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Objects };
            Instance instance =
                (Instance)serializer.Deserialize(streamReader, typeof(Instance));
            streamReader.Close();

            return instance;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public Instance(IInstance other)
        {
            Name = other.Name;
            CreationDate = other.CreationDate;
            Jobs = new List<IJob>(other.Jobs.Count);
            foreach (IJob job in other.Jobs) {
                Jobs.Add(new Job(job));
            }
            Machines = new Dictionary<int,IMachine>(other.Machines.Count);
            foreach (KeyValuePair<int,IMachine> machine in other.Machines)
            {
                Machines.Add(machine.Value.Id,new Machine(machine.Value));
            }
            Attributes = new Dictionary<int,IAttribute>(other.Attributes.Count);
            foreach (KeyValuePair<int,IAttribute> attribute in other.Attributes)
            {
                Attributes.Add(attribute.Value.Id, new Attribute(attribute.Value));
            }
            foreach (IJob job in Jobs)
            {
                foreach (int machine in job.EligibleMachines)
                {
                    if (!other.Machines.ContainsKey(machine))
                    {
                        throw new ArgumentException("No machine with following id" + machine);
                    }
                    if (!other.Attributes.ContainsKey(job.AttributeIdPerMachine[machine]))
                    {
                        throw new ArgumentException("No attribute with following id" + job.AttributeIdPerMachine[machine]);
                    }
                }                
            }
            SchedulingHorizonStart = other.SchedulingHorizonStart;
            SchedulingHorizonEnd = other.SchedulingHorizonEnd;
        }
    }
}