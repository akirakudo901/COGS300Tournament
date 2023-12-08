using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Actuators;

// This folder implements a "chase mode" to the agent; it will essentially beam 
// towards the enemy, shooting lasers. Once the enemy is frozen, it turns to the
// enemy's back side, while keeping the harassment up.

namespace ComponentAgents
{ 

    public class ChaseHeuristic : HeuristicAgent {

        new public const string name = "CHASE_HEURISTIC";
        
        public float HOW_FAR_BACK_OF_THE_ENEMY = 5f;

        // constructor to be specified to instantiate an example of this class
        public ChaseHeuristic(GGBond instance) : base(instance) {}

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

            // if the enemy is not frozen (we still have to attack) and isn't facing away,
            // zoom up to them and harass
            if (!ggbond.GetEnemy().GetComponent<CogsAgent>().IsFrozen() && !ggbond.EnemyIsFacingAway()) {
                var howToAct = ggbond.HowToTurnOrGo(ggbond.GetEnemy(), true);
                forwardAxis = howToAct.forwardAxis;
                rotateAxis = howToAct.rotateAxis;
            } else if (!ggbond.GetEnemy().GetComponent<CogsAgent>().IsFrozen() && ggbond.EnemyIsFacingAway()) {
                var howToAct = ggbond.HowToTurnOrGo(GetNDistanceBehindEnemy(HOW_FAR_BACK_OF_THE_ENEMY), true);
                forwardAxis = howToAct.forwardAxis;
                rotateAxis = howToAct.rotateAxis;
            // otherwise, while the enemy is frozen, get behind them and stay there to avoid attacks                
            } else {
                if (Vector3.Distance(ggbond.transform.position, GetNDistanceBehindEnemy(HOW_FAR_BACK_OF_THE_ENEMY)) > 2f)
                {
                    var howToAct = ggbond.HowToTurnOrGo(GetNDistanceBehindEnemy(HOW_FAR_BACK_OF_THE_ENEMY), false);
                    forwardAxis = howToAct.forwardAxis;
                    rotateAxis = howToAct.rotateAxis;
                } else {
                    rotateAxis = ggbond.WhichWayToTurn(ggbond.GetYAngle(ggbond.GetEnemy()), true);
                }
            }

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

        // -----------------HELPERS---------------
        public Vector3 GetNDistanceBehindEnemy(float n) {
            return ggbond.GetEnemy().transform.position - ggbond.GetEnemy().transform.forward * n;
        }
    }
}
