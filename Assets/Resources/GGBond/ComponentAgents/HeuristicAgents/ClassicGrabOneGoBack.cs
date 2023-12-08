using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Actuators;

// This implements the classic "grab one, go back, rinse and repeat" heuristic.
// It grabs a ball, then goes back to base, repeating using the hard-coded
// "go to base" and "go to target" choices.

namespace ComponentAgents
{ 

    public class ClassicGrabOneGoBack : HeuristicAgent {

        new public const string name = "CLASSIC_GRAB_ONE_GO_BACK";

        // constructor to be specified to instantiate an example of this class
        public ClassicGrabOneGoBack(GGBond instance) : base(instance) {}

        // Initialize values specific for the template agent before simulation starts
        public override void ComponentAgentStart()
        {
            // if you wanna do something to be called as part of Start(), put it here.
        }

        // Decides control of AI based on inputs
        public override void ComponentAgentHeuristic(in ActionBuffers actionsOut)
        {
            // value initialization; no need to change
            int forwardAxis = 0;
            int rotateAxis = 0;
            int shootAxis = 0;
            int goToTargetAxis = 0;
            int goToBaseAxis = 0;

            // #########################
            // MAKE CHANGE START!

            // if we have no targets, go and grab one
            if (ggbond.GetCarrying() == 0) goToTargetAxis = 1;
            // otherwise, if we have more than 1 target carried, go back to base
            else goToBaseAxis = 1;

            // MAKE CHANGE END!
            // #########################
            
            // assign the correct value below; no need for change
            var discreteActionsOut = actionsOut.DiscreteActions;

            discreteActionsOut[0] = forwardAxis;
            discreteActionsOut[1] = rotateAxis;
            discreteActionsOut[2] = shootAxis;
            discreteActionsOut[3] = goToTargetAxis;
            discreteActionsOut[4] = goToBaseAxis;
        }

        // Returns the name of this agent
        public override string GetName() {return name;}

    }

}
