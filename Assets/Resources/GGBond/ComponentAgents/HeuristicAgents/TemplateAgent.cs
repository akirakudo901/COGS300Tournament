using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Actuators;

/**
* This file shows you how to implement an all heuristic agent and use it as part of GGBond!
* I would advise you copy and use this as template!
* 1. Rename the class name to your own class name, e.g. MyAgent.
* 2. Rename the constructor (the method named TemplateAgent) to your name in 1, e.g MyAgent.
* 3. Set a name for your agent which will be used to refer to your agent in GGBond's ActionDeterminingLogic.
*    e.g. TEMPLATE_AGENT, MY_AGENT, etc.
* 4. Implement the functions you need! ComponentAgentHeuristic will likely be the main one you change!
* 
* Once you do those steps, you can go to GGBond.cs and do the following changes 
* (also specified in GGBond's top):
* 1. Go to the InitializeAgentModes function and add a new instance of your class to the list 
*    'allAgentModes'.
* 2. Go to ActionDeterminingLogic and specify your logic to switch between agent modes, referring
*    to your own agent by the name you gave it in step 3. of TemplateAgent.cs's first steps!
* 3. Don't forget to set the agent mode to InferenceOnly.
* 4. Try and see if it works!
* Definitely let me know if there are bugs / things you wanna double check.
**/

public partial class GGBond
{ 

    private class TemplateAgent : HeuristicAgent {

        new public const string name = "TEMPLATE_AGENT";

        // constructor to be specified to instantiate an example of this class
        public TemplateAgent(GGBond instance) : base(instance) {}

        // Initialize values specific for the template agent before simulation starts
        public override void ComponentAgentStart()
        {
            // if you wanna do something to be called as part of Start(), put it here.
        }

        // Decides control of AI based on inputs
        public override void ComponentAgentHeuristic(in ActionBuffers actionsOut)
        {
            var discreteActionsOut = actionsOut.DiscreteActions;
            
            // If shootWheneverSeeingTheEnemy is true, shoot every time the enemy is right in front
            if (ggbond.GetYAngle(ggbond.enemy) <= 5 && ggbond.GetYAngle(ggbond.enemy) >= -5
            && Vector3.Distance(ggbond.transform.localPosition, ggbond.enemy.transform.localPosition) <= LASER_LENGTH) 
            discreteActionsOut[2] = 1;
            
            discreteActionsOut[0] = 1;
        }

        // Returns the name of this agent
        public override string GetName() {return name;}

    }

}
