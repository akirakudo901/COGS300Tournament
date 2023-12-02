using System;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

namespace CopiedCode
{
    /// <summary>
    /// Defines the structure of the actions to be used by the Actuator system.
    /// </summary>
    [Serializable]
    public struct CopiedActionSpec
    {
        [SerializeField]
        int m_NumContinuousActions;

        /// <summary>
        /// An array of branch sizes for discrete actions.
        ///
        /// For an IActuator that uses discrete actions, the number of
        /// branches is the Length of the Array and each index contains the branch size.
        /// The cumulative sum of the total number of discrete actions can be retrieved
        /// by the <see cref="SumOfDiscreteBranchSizes"/> property.
        ///
        /// For an IActuator with a Continuous it will be null.
        /// </summary>
        public int[] BranchSizes;

        /// <summary>
        /// The number of continuous actions that an Agent can take.
        /// </summary>
        public int NumContinuousActions { get { return m_NumContinuousActions; } set { m_NumContinuousActions = value; } }

        /// <summary>
        /// The number of branches for discrete actions that an Agent can take.
        /// </summary>
        public int NumDiscreteActions { get { return BranchSizes == null ? 0 : BranchSizes.Length; } }

        /// <summary>
        /// Get the total number of Discrete Actions that can be taken by calculating the Sum
        /// of all of the Discrete Action branch sizes.
        /// </summary>
        public int SumOfDiscreteBranchSizes { get { return BranchSizes == null ? 0 : BranchSizes.Sum(); } }

        /// <summary>
        /// Creates a Continuous <see cref="CopiedActionSpec"/> with the number of actions available.
        /// </summary>
        /// <param name="numActions">The number of continuous actions available.</param>
        /// <returns>An Continuous CopiedActionSpec initialized with the number of actions available.</returns>
        public static CopiedActionSpec MakeContinuous(int numActions)
        {
            var actuatorSpace = new CopiedActionSpec(numActions, null);
            return actuatorSpace;
        }

        /// <summary>
        /// Creates a Discrete <see cref="CopiedActionSpec"/> with the array of branch sizes that
        /// represents the action space.
        /// </summary>
        /// <param name="branchSizes">The array of branch sizes for the discrete actions.  Each index
        /// contains the number of actions available for that branch.</param>
        /// <returns>An Discrete CopiedActionSpec initialized with the array of branch sizes.</returns>
        public static CopiedActionSpec MakeDiscrete(params int[] branchSizes)
        {
            var actuatorSpace = new CopiedActionSpec(0, branchSizes);
            return actuatorSpace;
        }

        /// <summary>
        /// Create an CopiedActionSpec initialized with the specified action sizes.
        /// </summary>
        /// <param name="numContinuousActions">The number of continuous actions available.</param>
        /// <param name="discreteBranchSizes">The array of branch sizes for the discrete actions.  Each index
        /// contains the number of actions available for that branch.</param>
        public CopiedActionSpec(int numContinuousActions = 0, int[] discreteBranchSizes = null)
        {
            m_NumContinuousActions = numContinuousActions;
            BranchSizes = discreteBranchSizes ?? Array.Empty<int>();
        }

        /// <summary>
        /// Check that the CopiedActionSpec uses either all continuous or all discrete actions.
        /// This is only used when connecting to old versions of the trainer that don't support this.
        /// </summary>
        /// <exception cref="UnityAgentsException"></exception>
        public void CheckAllContinuousOrDiscrete()
        {
            if (NumContinuousActions > 0 && NumDiscreteActions > 0)
            {
                throw new UnityAgentsException(
                    "Action spaces with both continuous and discrete actions are not supported by the trainer. " +
                    "CopiedActionSpecs must be all continuous or all discrete."
                );
            }
        }

        /// <summary>
        /// Combines a list of actions specs and allocates a new array of branch sizes if needed.
        /// </summary>
        /// <param name="specs">The list of action specs to combine.</param>
        /// <returns>An CopiedActionSpec which represents the aggregate of the CopiedActionSpecs passed in.</returns>
        public static CopiedActionSpec Combine(params CopiedActionSpec[] specs)
        {
            var numContinuous = 0;
            var numDiscrete = 0;
            for (var i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                numContinuous += spec.NumContinuousActions;
                numDiscrete += spec.NumDiscreteActions;
            }

            if (numDiscrete <= 0)
            {
                return MakeContinuous(numContinuous);
            }

            var branchSizes = new int[numDiscrete];
            var offset = 0;
            for (var i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                if (spec.BranchSizes.Length == 0)
                {
                    continue;
                }
                var branchSizesLength = spec.BranchSizes.Length;
                Array.Copy(spec.BranchSizes,
                    0,
                    branchSizes,
                    offset,
                    branchSizesLength);
                offset += branchSizesLength;
            }
            return new CopiedActionSpec(numContinuous, branchSizes);
        }

        // DEFINED MY OWN TO CAST CopiedActionSpec TO ActionSpec
        public static ActionSpec ConvertToActionSpec(CopiedActionSpec copiedActionSpec)
        {
            return new ActionSpec(copiedActionSpec.NumContinuousActions, copiedActionSpec.BranchSizes);
        }
    }
}
