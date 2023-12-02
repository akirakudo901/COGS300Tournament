using Unity.Barracuda;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Inference;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

// These codes were directly copied from the MLAgents original files, such that 
// I can instantiate a BarracudaPolicy object to use for neural network agents.

namespace CopiedCode
{

    /// <summary>
    /// The Barracuda Policy uses a Barracuda Model to make decisions at
    /// every step. It uses a ModelRunner that is shared across all
    /// Barracuda Policies that use the same model and inference devices.
    /// </summary>
    public class CopiedBarracudaPolicy
    {
        protected CopiedModelRunner m_ModelRunner;
        ActionBuffers m_LastActionBuffer;

        int m_AgentId;

        /// <summary>
        /// Sensor shapes for the associated Agents. All Agents must have the same shapes for their Sensors.
        /// </summary>
        List<int[]> m_SensorShapes;
        CopiedActionSpec m_ActionSpec;

        /// <summary>
        /// Instantiate a BarracudaPolicy with the necessary objects for it to run.
        /// </summary>
        /// <param name="actionSpec">The action spec of the behavior.</param>
        /// <param name="model">The Neural Network to use.</param>
        /// <param name="inferenceDevice">Which device Barracuda will run on.</param>
        public CopiedBarracudaPolicy(
            CopiedActionSpec actionSpec,
            NNModel model,
            InferenceDevice inferenceDevice
        )
        {
            //last two entries are inferenceSeed and deterministicInference
            var modelRunner = new CopiedModelRunner(model, actionSpec, inferenceDevice, 0);
            m_ModelRunner = modelRunner;
            m_ActionSpec = actionSpec;
        }

        /// <inheritdoc />
        public void RequestDecision(CopiedAgentInfo info, List<CopiedISensor> sensors)
        {
            m_AgentId = info.episodeId;
            m_ModelRunner?.PutObservations(info, sensors);
        }

        /// <inheritdoc />
        public ref readonly ActionBuffers DecideAction()
        {
            if (m_ModelRunner == null)
            {
                m_LastActionBuffer = ActionBuffers.Empty;
            }
            else
            {
                m_ModelRunner?.DecideBatch();
                m_LastActionBuffer = m_ModelRunner.GetAction(m_AgentId);
            }
            return ref m_LastActionBuffer;
        }

        public void Dispose()
        {
        }
    }
}
