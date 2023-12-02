using System;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.MLAgents;

namespace GGBondUtilities
{
    /// <summary>
    /// On top of the original functionality by the DecisionRequester, 
    /// the NNAgentDecisionRequester also appropriately requests decisions
    /// to NeuralNetworkAgents attached to the Agent object. 
    /// Check DecisionRequester.cs for a detailing of the original functionality! /// </summary>    
    [AddComponentMenu("GGBondUtilities / NNAgent Decision Requester", 50)]
    [RequireComponent(typeof(Agent))]
    public class NNAgentDecisionRequester : MonoBehaviour
    {
        /// <summary>
        /// The frequency with which the agent requests a decision. A DecisionPeriod of 5 means
        /// that the Agent will request a decision every 5 Academy steps. /// </summary>
        [Range(1, 20)]
        [Tooltip("The frequency with which the agent requests a decision. A DecisionPeriod " +
            "of 5 means that the Agent will request a decision every 5 Academy steps.")]
        public int DecisionPeriod = 5;

        /// <summary>
        /// Indicates whether or not the agent will take an action during the Academy steps where
        /// it does not request a decision. Has no effect when DecisionPeriod is set to 1.
        /// </summary>
        [Tooltip("Indicates whether or not the agent will take an action during the Academy " +
            "steps where it does not request a decision. Has no effect when DecisionPeriod " +
            "is set to 1.")]
        [FormerlySerializedAs("RepeatAction")]
        public bool TakeActionsBetweenDecisions = true;

        [NonSerialized]
        GGBond m_Agent;

        /// <summary>
        /// Get the Agent attached to the DecisionRequester.
        /// </summary>
        public GGBond Agent
        {
            get => m_Agent;
        }

        internal void Awake()
        {
            m_Agent = gameObject.GetComponent<GGBond>();
            Debug.Assert(m_Agent != null, "Agent component was not found on this gameObject and is required.");
            Academy.Instance.AgentPreStep += MakeRequests;
        }

        void OnDestroy()
        {
            if (Academy.IsInitialized)
            {
                Academy.Instance.AgentPreStep -= MakeRequests;
            }
        }

        /// <summary>
        /// Information about Academy step used to make decisions about whether to request a decision.
        /// </summary>
        public struct DecisionRequestContext
        {
            /// <summary>
            /// The current step count of the Academy, equivalent to Academy.StepCount.
            /// </summary>
            public int AcademyStepCount;
        }

        /// <summary>
        /// Method that hooks into the Academy in order inform the Agent on whether or not it should request a
        /// decision, and whether or not it should take actions between decisions.
        /// </summary>
        /// <param name="academyStepCount">The current step count of the academy.</param>
        void MakeRequests(int academyStepCount)
        {
            var context = new DecisionRequestContext
            {
                AcademyStepCount = academyStepCount
            };

            if (ShouldRequestDecision(context))
            {
                m_Agent?.RequestDecision();
                m_Agent?.RequestDecisionToNNAgent();
            }

            if (ShouldRequestAction(context))
            {
                m_Agent?.RequestAction();
                m_Agent?.RequestActionToNNAgent();
            }
        }

        /// <summary>
        /// Whether Agent.RequestDecision should be called on this update step.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual bool ShouldRequestDecision(DecisionRequestContext context)
        {
            return context.AcademyStepCount % DecisionPeriod == 0;
        }

        /// <summary>
        /// Whether Agent.RequestAction should be called on this update step.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual bool ShouldRequestAction(DecisionRequestContext context)
        {
            return TakeActionsBetweenDecisions;
        }
    }
}
