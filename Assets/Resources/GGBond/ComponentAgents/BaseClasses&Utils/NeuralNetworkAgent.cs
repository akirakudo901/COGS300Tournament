using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

// my own code
using CopiedCode;
using GGBondUtilities;

// This file implements the "NeuralNetworkAgent" class which forms the basis of any agent that uses neural networks
public partial class GGBond
{
    
    // nested abstarct class as template for agents who leverages neural networks
    abstract class NeuralNetworkAgent : ComponentAgent {

        new public const string name = "NEURAL_NETWORK_AGENT";
        
        // action spec for the agent - globally be 5 branch Discrete, (3, 3, 2, 2, 2)
        // identical to what specified in BehaviorParameters Component in editor
        public CopiedActionSpec ActSpec {
            get {return CopiedActionSpec.MakeDiscrete(3, 3, 2, 2, 2);}
        }

        // an internal CopiedBaraccudaPolicy object to get actions from the network
        public CopiedBarracudaPolicy Policy { get; internal set; }

        private NNModel m_model;
        // model to be loaded to BehaviorParameters
        public NNModel Model {
            get {return m_model;}
            internal set {m_model = value; UpdatePolicy();}
        }

        // the number of vector observations to stack - to be readjusted for every inheriting agent
        public int NumStackedVectorObservations { get; set; }

        // the size of vector observation - to be readjusted for every inheriting agent
        public int VectorObservationSize { get; set; } 

        // sensors used to generate observations for this agent mode
        public List<CopiedISensor> NNAgentSensors;
        // vector sensor used for observation
        public CopiedVectorSensor NNVectorSensor;
        // agent info stored - really there just to make the remaining code work
        private CopiedAgentInfo m_info;
        // whether a decision has been requested
        private bool m_decisionRequested = false;
        // whether an action has been requested
        private bool m_actionRequested = false;
        // the action from the immediate previous time
        private ActionBuffers m_previousAction;
        // whether there is a NNAgentDecisionRequester object assigned to the GGBond instance
        private bool m_nnAgentDecisionRequesterAttached;

        
        // constructor to be specified to instantiate an example of this class
        public NeuralNetworkAgent(GGBond instance, NNModel model) : base(instance) {
            this.m_model = model;
        }

        // request a decision - essentially meaning that in the next ComponentAgentHeuristic call,
        // we call Policy.RequestDecision
        public void RequestNNDecision() {
            m_decisionRequested = true;
            RequestNNAction();
        }

        // request an action - essentially meaning that in the next ComponentAgentHeuristic call, 
        // we repeat the last performed action
        public void RequestNNAction() {
            m_actionRequested = true;
        }

        //------------------ABSTRACT FUNCTIONS---------------
        // Get relevant information from the environment
        public abstract void GetObservations(CopiedVectorSensor sensor);

        //------------------MONOBEHAVIOUR FUNCTIONS---------------
        // every inheriting agent has to call SetupSensors appropriately to do some inference
        public override void ComponentAgentOnEnable() 
        {
            UpdatePolicy();
            this.m_info = new CopiedAgentInfo();
            this.m_info.episodeId = 99; //should not matter whatever number we put here
            // if there is an NNAgentDecisionRequester attached to instance, remember as so
            m_nnAgentDecisionRequesterAttached = (ggbond.GetComponent<NNAgentDecisionRequester>() != null);
        }
        
        // called when the parent agent is disabled
        public override void ComponentAgentOnDisable()
        {
            CleanupSensors();
            Policy?.Dispose();
        }

        //------------------COMPONENTAGENT FUNCTIONS---------------
        // Obtain observations from the environment to be passed to the policy, and
        // determine appropriate actions
        // we can switch between using NNAgentDecisionRequester and not
        public override void ComponentAgentHeuristic(in ActionBuffers actionsOut) 
        {
            // if any decision were requested recently
            if (!m_nnAgentDecisionRequesterAttached || m_decisionRequested) 
            {
                // set m_decisionRequested to false
                m_decisionRequested = false;

                // collect the observation
                UpdateSensors(); //first call UpdateSensors, as it will clear the content of NNVectorSensor
                GetObservations(NNVectorSensor);

                // request a decision (let the model runner know that this agent needs a decision)
                Policy.RequestDecision(m_info, NNAgentSensors);
                // get the action
                m_previousAction = Policy.DecideAction();
            }

            var discreteActionsOut = actionsOut.DiscreteActions;
            
            // if any action were requested recently 
            if (!m_nnAgentDecisionRequesterAttached || m_actionRequested) 
            {
                // set m_actionRequested to false
                m_actionRequested = false;

                // adjust actionsOut based on the previous action
                for (int i=0; i < 5; i++) discreteActionsOut[i] = m_previousAction.DiscreteActions[i];
            
            // otherwise no action was requested
            } 
            else 
            {
                // set the default action (do nothing)
                for (int i=0; i < 5; i++) discreteActionsOut[i] = 0;
            }
            
        }

        // Returns the name of this agent
        public override string GetName() {return name;}


        //------------------HELPER FUNCTIONS---------------
        
        // useful for setting up observations with specific shapes, to be passed
        // and initialize in pair with a neural network
        protected void SetUpSensors(int vectorObservationSize = -1, int numStackedVectorObservations = -1) {
            // CODE HIGHLY BASED ON InitializeSensors() FOUND HERE, THANKS!: 
            // [https://github.com/Unity-Technologies/ml-agents/blob/209d258dabc57af1212f94cf8d1fac9193675690/com.unity.ml-agents/Runtime/Agent.cs#L977]

            if (vectorObservationSize != -1) VectorObservationSize = vectorObservationSize;
            if (numStackedVectorObservations != -1) NumStackedVectorObservations = numStackedVectorObservations;
            // if NNAgentSensors had been initialized before, clean up the sensors first
            if (NNAgentSensors != null) CleanupSensors();
            NNAgentSensors = new List<CopiedISensor>();
            
            // Constructing the RayPerceptionSensorComponent3D sensor that was attached
            // to my agent as it trained. I believe the sensor came by default.
            // THERE ARE SOME ASSUMPTIONS I MAKE IN THE WAY THESE AGENTS WERE TRAINED;
            // FOR EXAMPLE, THIS PART OF THE CODE WILL LIKELY CAUSE AN ERROR IF THE 
            // NEURAL NETWORK WAS TRAINED USING SETTING UseChildSensors AS TRUE, or
            // ObservableAttribute AS *NOT* IGNORE.
            CopiedRayPerceptionSensorComponent3D ThreeDRay = ggbond.gameObject.AddComponent(typeof(CopiedRayPerceptionSensorComponent3D)) as CopiedRayPerceptionSensorComponent3D;
            
            // the following values were taken from the inspector menu
            ThreeDRay.SensorName = "RayPerceptionSensor";
            ThreeDRay.DetectableTags = new List<string>() {"Player", "Wall", "Target", "HomeBase"};
            ThreeDRay.RaysPerDirection = 5;
            ThreeDRay.MaxRayDegrees = 180;
            ThreeDRay.SphereCastRadius = 0.5f;
            ThreeDRay.RayLength = 62f;
            ThreeDRay.RayLayerMask = LayerMask.GetMask("Default", "TransparentFX", "Water", "UI");
            ThreeDRay.ObservationStacks = 3;
            ThreeDRay.StartVerticalOffset = 0f;
            ThreeDRay.EndVerticalOffset = 0f;

            CopiedISensor[] ThreeDSensors = ThreeDRay.CreateSensors();
            NNAgentSensors.Capacity += 1;
            NNAgentSensors.AddRange(ThreeDSensors);
            
            // set up the vector sensors
            NNVectorSensor = new CopiedVectorSensor(VectorObservationSize);
            if (NumStackedVectorObservations > 1)
            {
                CopiedStackingSensor stackedCollectObservationsSensor = new CopiedStackingSensor(
                    NNVectorSensor, NumStackedVectorObservations);
                NNAgentSensors.Add(stackedCollectObservationsSensor);
            }
            else
            {
                NNAgentSensors.Add(NNVectorSensor);
            }

            // Sort the Sensors by name to ensure determinism
            // Taken from SensorUtils under ISensor under MLAgents>Runtime>Sensors.
            NNAgentSensors.Sort((x, y) => string.Compare(x.GetName(), y.GetName(), System.StringComparison.InvariantCulture));
        }

        // cleans up sensors that were allocated on this agent mode
        void CleanupSensors()
        {
            // for each sensor added to NNAgentSensors
            foreach (CopiedISensor sensor in NNAgentSensors) 
            {
                
                // Dispose sensor
                if (sensor is IDisposable disposableSensor)
                {
                    disposableSensor.Dispose();
                }
            }
            NNAgentSensors = null;
        }

        // updates the content of all sensors attached to this NNAgent
        void UpdateSensors()
        {
            if (NNAgentSensors == null) return;
            foreach (var sensor in NNAgentSensors)
            {
                sensor.Update();
            }
        }

        // reinitialize the policy
        public void UpdatePolicy() {
            if (m_model == null) return;
            Policy?.Dispose();
            Policy = new CopiedBarracudaPolicy(this.ActSpec, this.Model, InferenceDevice.Default);
        }

    } 

}
