using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine.Profiling;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

// mycode
using CopiedCode;

// These codes were directly copied from the MLAgents original files, such that 
// I can instantiate a BarracudaPolicy object to use for neural network agents.

namespace CopiedCode
{
    public struct CopiedAgentInfo
    {
        /// <summary>
        /// Keeps track of the last actions taken by the Brain.
        /// </summary>
        public ActionBuffers storedActions;

        /// <summary>
        /// For discrete control, specifies the actions that the agent cannot take.
        /// An element of the mask array is <c>true</c> if the action is prohibited.
        /// </summary>
        public bool[] discreteActionMasks;

        /// <summary>
        /// The current agent reward.
        /// </summary>
        public float reward;

        /// <summary>
        /// The current group reward received by the agent.
        /// </summary>
        public float groupReward;

        /// <summary>
        /// Whether the agent is done or not.
        /// </summary>
        public bool done;

        /// <summary>
        /// Whether the agent has reached its max step count for this episode.
        /// </summary>
        public bool maxStepReached;

        /// <summary>
        /// Episode identifier each agent receives at every reset. It is used
        /// to separate between different agents in the environment.
        /// </summary>
        public int episodeId;

        /// <summary>
        /// MultiAgentGroup identifier.
        /// </summary>
        public int groupId;

        public void ClearActions()
        {
            storedActions.Clear();
        }

        public void CopyActions(ActionBuffers actionBuffers)
        {
            var continuousActions = storedActions.ContinuousActions;
            for (var i = 0; i < actionBuffers.ContinuousActions.Length; i++)
            {
                continuousActions[i] = actionBuffers.ContinuousActions[i];
            }
            var discreteActions = storedActions.DiscreteActions;
            for (var i = 0; i < actionBuffers.DiscreteActions.Length; i++)
            {
                discreteActions[i] = actionBuffers.DiscreteActions[i];
            }
        }
    }

    public struct AgentInfoSensorsPair
    {
        public CopiedAgentInfo agentInfo;
        public List<CopiedISensor> sensors;
    }

    public class CopiedModelRunner
    {
        List<AgentInfoSensorsPair> m_Infos = new List<AgentInfoSensorsPair>();
        Dictionary<int, ActionBuffers> m_LastActionsReceived = new Dictionary<int, ActionBuffers>();
        List<int> m_OrderedAgentsRequestingDecisions = new List<int>();

        ITensorAllocator m_TensorAllocator;
        CopiedTensorGenerator m_TensorGenerator;
        CopiedTensorApplier m_TensorApplier;

        NNModel m_Model;
        string m_ModelName;
        InferenceDevice m_InferenceDevice;
        IWorker m_Engine;
        bool m_Verbose = false;
        string[] m_OutputNames;
        IReadOnlyList<CopiedTensorProxy> m_InferenceInputs;
        List<CopiedTensorProxy> m_InferenceOutputs;
        Dictionary<string, Tensor> m_InputsByName;
        Dictionary<int, List<float>> m_Memories = new Dictionary<int, List<float>>();

        bool m_ObservationsInitialized;

        /// <summary>
        /// Initializes the Brain with the Model that it will use when selecting actions for
        /// the agents
        /// </summary>
        /// <param name="model"> The Barracuda model to load </param>
        /// <param name="actionSpec"> Description of the actions for the Agent.</param>
        /// <param name="inferenceDevice"> Inference execution device. CPU is the fastest
        /// option for most of ML Agents models. </param>
        /// <param name="seed"> The seed that will be used to initialize the RandomNormal
        /// and Multinomial objects used when running inference.</param>
        /// <exception cref="UnityAgentsException">Throws an error when the model is null
        /// </exception>
        public CopiedModelRunner(
            NNModel model,
            CopiedActionSpec actionSpec,
            InferenceDevice inferenceDevice,
            int seed = 0)
        {
            Model barracudaModel;
            m_Model = model;
            m_ModelName = model.name;
            m_InferenceDevice = inferenceDevice;
            m_TensorAllocator = new TensorCachingAllocator();
            if (model != null)
            {

                D.logEnabled = m_Verbose;

                barracudaModel = ModelLoader.Load(model);

                var failedCheck = CopiedBarracudaModelParamLoader.CheckModelVersion(
                        barracudaModel
                    );
                if (failedCheck != null)
                {
                    if (failedCheck.CheckType == CopiedBarracudaModelParamLoader.FailedCheck.CheckTypeEnum.Error)
                    {
                        throw new UnityAgentsException(failedCheck.Message);
                    }
                }

                WorkerFactory.Type executionDevice;
                switch (inferenceDevice)
                {
                    case InferenceDevice.CPU:
                        executionDevice = WorkerFactory.Type.CSharp;
                        break;
                    case InferenceDevice.GPU:
                        executionDevice = WorkerFactory.Type.ComputePrecompiled;
                        break;
                    case InferenceDevice.Burst:
                        executionDevice = WorkerFactory.Type.CSharpBurst;
                        break;
                    case InferenceDevice.Default: // fallthrough
                    default:
                        executionDevice = WorkerFactory.Type.CSharpBurst;
                        break;
                }
                m_Engine = WorkerFactory.CreateWorker(executionDevice, barracudaModel, m_Verbose);
            }
            else
            {
                barracudaModel = null;
                m_Engine = null;
            }

            m_InferenceInputs = barracudaModel.GetInputTensors();
            m_OutputNames = barracudaModel.GetOutputNames();
            m_TensorGenerator = new CopiedTensorGenerator(
                seed, m_TensorAllocator, m_Memories, barracudaModel);
            m_TensorApplier = new CopiedTensorApplier(
                actionSpec, seed, m_TensorAllocator, m_Memories, barracudaModel);
            m_InputsByName = new Dictionary<string, Tensor>();
            m_InferenceOutputs = new List<CopiedTensorProxy>();
        }

        public InferenceDevice InferenceDevice
        {
            get { return m_InferenceDevice; }
        }

        public NNModel Model
        {
            get { return m_Model; }
        }

        void PrepareBarracudaInputs(IReadOnlyList<CopiedTensorProxy> infInputs)
        {
            m_InputsByName.Clear();
            for (var i = 0; i < infInputs.Count; i++)
            {
                var inp = infInputs[i];
                m_InputsByName[inp.name] = inp.data;
            }
        }

        public void Dispose()
        {
            if (m_Engine != null)
                m_Engine.Dispose();
            m_TensorAllocator?.Reset(false);
        }

        void FetchBarracudaOutputs(string[] names)
        {
            m_InferenceOutputs.Clear();
            foreach (var n in names)
            {
                var output = m_Engine.PeekOutput(n);
                m_InferenceOutputs.Add(TensorUtils.TensorProxyFromBarracuda(output, n));
            }
        }

        public void PutObservations(CopiedAgentInfo info, List<CopiedISensor> sensors)
        {
            m_Infos.Add(new AgentInfoSensorsPair
            {
                agentInfo = info,
                sensors = sensors
            });

            // We add the episodeId to this list to maintain the order in which the decisions were requested
            m_OrderedAgentsRequestingDecisions.Add(info.episodeId);

            if (!m_LastActionsReceived.ContainsKey(info.episodeId))
            {
                m_LastActionsReceived[info.episodeId] = ActionBuffers.Empty;
            }
            if (info.done)
            {
                // If the agent is done, we remove the key from the last action dictionary since no action
                // should be taken.
                m_LastActionsReceived.Remove(info.episodeId);
            }
        }

        public void DecideBatch()
        {
            var currentBatchSize = m_Infos.Count;
            if (currentBatchSize == 0)
            {
                return;
            }
            if (!m_ObservationsInitialized)
            {
                // Just grab the first agent in the collection (any will suffice, really).
                // We check for an empty Collection above, so this will always return successfully.
                var firstInfo = m_Infos[0];
                m_TensorGenerator.InitializeObservations(firstInfo.sensors, m_TensorAllocator);
                m_ObservationsInitialized = true;
            }

            // Prepare the input tensors to be feed into the engine
            m_TensorGenerator.GenerateTensors(m_InferenceInputs, currentBatchSize, m_Infos);
            PrepareBarracudaInputs(m_InferenceInputs);

            // Execute the Model
            m_Engine.Execute(m_InputsByName);

            FetchBarracudaOutputs(m_OutputNames);

            // Update the outputs
            m_TensorApplier.ApplyTensors(m_InferenceOutputs, m_OrderedAgentsRequestingDecisions, m_LastActionsReceived);

            m_Infos.Clear();

            m_OrderedAgentsRequestingDecisions.Clear();
        }

        public bool HasModel(NNModel other, InferenceDevice otherInferenceDevice)
        {
            return m_Model == other && m_InferenceDevice == otherInferenceDevice;
        }

        public ActionBuffers GetAction(int agentId)
        {
            if (m_LastActionsReceived.ContainsKey(agentId))
            {
                return m_LastActionsReceived[agentId];
            }
            return ActionBuffers.Empty;
        }
    }
}