using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

// my code
using ComponentAgents;

// TODO ADD AN EXPLANATION OF WHAT THIS AGENT IS ABOUT!

// This file holds the almost original agent code.

/**
* The following was written for my teammates on how to write their own agent code! Akira
* 
* ############### HOW TO ADD A NEW HEURISTIC AGENT #################
* If you want to add a new entirely-heuristic agent mode to use for the ActionDeterminingLogic:
* 1. Go to the TemplateAgent.cs file and follow its instructions to make your own agent.
* 2. Come back to this script. Then, go to the InitializeAgentModes function and add a new 
* instance of your class to the list 'allAgentModes'.
* 
* Once you're done, go to "SET UP ON GGBOND'S SIDE" below! 
* 
* ############### HOW TO ADD A NEW NEURAL NETWORK AGENT #################
* If you want to add a new neural network agent mode to use for the ActionDeterminingLogic:
* 1. Go to the NNTemplateAgent.cs file and follow its instructions to make your own agent.
*
* Once you're done, come back here! 
*
* 2. Go right below GGBond's declaration to find how to set up a new field & inspector window to add your neural network model.
* 3. Go to the InitializeAgentModes function and add a new instance of your class to the list 'allAgentModes',
*    utilizing your newly set field.
* 4. Go to the Unity editor and slot your model into the newly created inspector under GGBond script!
* Then go to "SET UP ON GGBOND'S SIDE" below! 
* 
* ############### SET UP ON GGBOND'S SIDE #################
* 1. Go to ActionDeterminingLogic and specify your logic to switch between agent modes, referring
*    to your own agent by the name you gave it in step 3. of TemplateAgent.cs / NNTemplateAgent.cs's first steps!
* 2. Don't forget to set the agent mode to InferenceOnly.
* 3. Try and see if it works!

* Definitely let me know if there are bugs / things you wanna double check.
**/

public partial class GGBondV2 : GGBond
{   
    // --------------------AGENT FUNCTIONS-------------------------

    // This function handles the main logic behind which behavior type the 
    // agent takes at any single time. It will specify states which 
    // Heuristic use to take the associated actions.
    public override void ActionDeterminingLogic() {
        // start with COLLECTOR mode
        // as soon as our score surpasses the enemy's, turn to harass 
        if (myBase.GetComponent<HomeBase>().GetCaptured() > GetEnemyCaptured() ||
        myBase.GetComponent<HomeBase>().GetCaptured() >= 4)
        {
            agentMode = "HARASSER";
        } 
        else 
        {
            // this is the default agent mode
            agentMode = "COLLECTOR";
        }
    }

    public override void InitializeAgentModes() 
    {
        //###################################################
        // SPECIFY WHICH AGENT MODES YOU WANT TO USE HERE!
        // You should instantiate a new instance of your newly defined class 
        // and put into the list; that is, you wanna write:
        // new YOUR_AGENT_CLASS_NAME(this)
        // and add it into the brackets following the "new List<ComponentAgent>()" part.

        // If you are making a neural network agent, additionally pass the model
        // you want it to use as second parameter!
        allAgentModes = new List<ComponentAgent>();
        // allAgentModes.Add(new Collector(this, CollectorModel));
        // allAgentModes.Add(new Harasser(this, HarasserModel));
        // allAgentModes.Add(new HarasserHeuristic(this));
        allAgentModes.Add(new ClassicGrabOneGoBack(this));
        //###################################################
    }

    // -------HELPER FUNCTIONS BELOW!-----------






    






    // ------------------HELPER USEFUL FOR ACTION DETERMINING LOGIC-----------
}