using System.Collections.Generic;
using Unity.Barracuda;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;


// These codes were directly copied from the MLAgents original files, such that 
// I can instantiate a BarracudaPolicy object to use for neural network agents.

namespace CopiedCode
{
    /// <summary>
    /// Mapping between Tensor names and generators.
    /// A CopiedTensorGenerator implements a Dictionary of strings (node names) to an Action.
    /// The Action take as argument the tensor, the current batch size and a Dictionary of
    /// Agent to AgentInfo corresponding to the current batch.
    /// Each Generator reshapes and fills the data of the tensor based of the data of the batch.
    /// When the CopiedTensorProxy is an Input to the model, the shape of the Tensor will be modified
    /// depending on the current batch size and the data of the Tensor will be filled using the
    /// Dictionary of Agent to AgentInfo.
    /// When the CopiedTensorProxy is an Output of the model, only the shape of the Tensor will be
    /// modified using the current batch size. The data will be pre-filled with zeros.
    /// </summary>
    public class CopiedTensorGenerator
    {
        public interface IGenerator
        {
            /// <summary>
            /// Modifies the data inside a Tensor according to the information contained in the
            /// AgentInfos contained in the current batch.
            /// </summary>
            /// <param name="tensorProxy"> The tensor the data and shape will be modified.</param>
            /// <param name="batchSize"> The number of agents present in the current batch.</param>
            /// <param name="infos">
            /// List of AgentInfos containing the information that will be used to populate
            /// the tensor's data.
            /// </param>
            void Generate(
                CopiedTensorProxy tensorProxy, int batchSize, IList<AgentInfoSensorsPair> infos);
        }

        readonly Dictionary<string, IGenerator> m_Dict = new Dictionary<string, IGenerator>();
        int m_ApiVersion;

        /// <summary>
        /// Returns a new CopiedTensorGenerators object.
        /// </summary>
        /// <param name="seed"> The seed the Generators will be initialized with.</param>
        /// <param name="allocator"> Tensor allocator.</param>
        /// <param name="memories">Dictionary of AgentInfo.id to memory for use in the inference model.</param>
        /// <param name="barracudaModel"></param>
        public CopiedTensorGenerator(
            int seed,
            ITensorAllocator allocator,
            Dictionary<int, List<float>> memories,
            object barracudaModel = null)
        {
            // If model is null, no inference to run and exception is thrown before reaching here.
            if (barracudaModel == null)
            {
                return;
            }
            var model = (Model)barracudaModel;

            m_ApiVersion = model.GetVersion();

            // Generator for Inputs
            m_Dict[CopiedTensorNames.BatchSizePlaceholder] =
                new CopiedBatchSizeGenerator(allocator);
            m_Dict[CopiedTensorNames.SequenceLengthPlaceholder] =
                new CopiedSequenceLengthGenerator(allocator);
            m_Dict[CopiedTensorNames.RecurrentInPlaceholder] =
                new CopiedRecurrentInputGenerator(allocator, memories);

            m_Dict[CopiedTensorNames.PreviousActionPlaceholder] =
                new CopiedPreviousActionInputGenerator(allocator);
            m_Dict[CopiedTensorNames.ActionMaskPlaceholder] =
                new CopiedActionMaskInputGenerator(allocator);
            m_Dict[CopiedTensorNames.RandomNormalEpsilonPlaceholder] =
                new CopiedRandomNormalInputGenerator(seed, allocator);


            // Generators for Outputs
            if (model.HasContinuousOutputs())
            {
                m_Dict[model.ContinuousOutputName()] = new CopiedBiDimensionalOutputGenerator(allocator);
            }
            if (model.HasDiscreteOutputs())
            {
                m_Dict[model.DiscreteOutputName()] = new CopiedBiDimensionalOutputGenerator(allocator);
            }
            m_Dict[CopiedTensorNames.RecurrentOutput] = new CopiedBiDimensionalOutputGenerator(allocator);
            m_Dict[CopiedTensorNames.ValueEstimateOutput] = new CopiedBiDimensionalOutputGenerator(allocator);
        }

        public void InitializeObservations(List<CopiedISensor> sensors, ITensorAllocator allocator)
        {
            if (m_ApiVersion == (int)CopiedBarracudaModelParamLoader.ModelApiVersion.MLAgents1_0)
            {
                // Loop through the sensors on a representative agent.
                // All vector observations use a shared ObservationGenerator since they are concatenated.
                // All other observations use a unique ObservationInputGenerator
                var visIndex = 0;
                ObservationGenerator vecObsGen = null;
                for (var sensorIndex = 0; sensorIndex < sensors.Count; sensorIndex++)
                {
                    var sensor = sensors[sensorIndex];
                    var rank = sensor.GetObservationSpec().Rank;
                    ObservationGenerator obsGen = null;
                    string obsGenName = null;
                    switch (rank)
                    {
                        case 1:
                            if (vecObsGen == null)
                            {
                                vecObsGen = new ObservationGenerator(allocator);
                            }
                            obsGen = vecObsGen;
                            obsGenName = CopiedTensorNames.VectorObservationPlaceholder;
                            break;
                        case 2:
                            // If the tensor is of rank 2, we use the index of the sensor
                            // to create the name
                            obsGen = new ObservationGenerator(allocator);
                            obsGenName = CopiedTensorNames.GetObservationName(sensorIndex);
                            break;
                        case 3:
                            // If the tensor is of rank 3, we use the "visual observation
                            // index", which only counts the rank 3 sensors
                            obsGen = new ObservationGenerator(allocator);
                            obsGenName = CopiedTensorNames.GetVisualObservationName(visIndex);
                            visIndex++;
                            break;
                        default:
                            throw new UnityAgentsException(
                                $"Sensor {sensor.GetName()} have an invalid rank {rank}");
                    }
                    obsGen.AddSensorIndex(sensorIndex);
                    m_Dict[obsGenName] = obsGen;
                }
            }

            if (m_ApiVersion == (int)CopiedBarracudaModelParamLoader.ModelApiVersion.MLAgents2_0)
            {
                for (var sensorIndex = 0; sensorIndex < sensors.Count; sensorIndex++)
                {
                    var obsGen = new ObservationGenerator(allocator);
                    var obsGenName = CopiedTensorNames.GetObservationName(sensorIndex);
                    obsGen.AddSensorIndex(sensorIndex);
                    m_Dict[obsGenName] = obsGen;

                }
            }
        }

        /// <summary>
        /// Populates the data of the tensor inputs given the data contained in the current batch
        /// of agents.
        /// </summary>
        /// <param name="tensors"> Enumerable of tensors that will be modified.</param>
        /// <param name="currentBatchSize"> The number of agents present in the current batch
        /// </param>
        /// <param name="infos"> List of AgentsInfos and Sensors that contains the
        /// data that will be used to modify the tensors</param>
        /// <exception cref="UnityAgentsException"> One of the tensor does not have an
        /// associated generator.</exception>
        public void GenerateTensors(
            IReadOnlyList<CopiedTensorProxy> tensors, int currentBatchSize, IList<AgentInfoSensorsPair> infos)
        {
            for (var tensorIndex = 0; tensorIndex < tensors.Count; tensorIndex++)
            {
                var tensor = tensors[tensorIndex];
                if (!m_Dict.ContainsKey(tensor.name))
                {
                    throw new UnityAgentsException(
                        $"Unknown tensorProxy expected as input : {tensor.name}");
                }
                m_Dict[tensor.name].Generate(tensor, currentBatchSize, infos);
            }
        }
    }
}
