using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

// TODO ADD AN EXPLANATION OF WHAT THIS AGENT IS ABOUT!

// This file holds the almost original agent code.

/**
* If you want to add a new agent mode to use for the ActionDeterminingLogic:
* 1. Go to the TemplateAgent.cs file and follow its instructions to make your own agent.
* 
* Once you're done, come back here and do the following changes:
* 1. Go to the OnEnable function and add a new instance of your class to the list 'allAgentModes'
* 2. Go to ActionDeterminingLogic and specify your logic to switch between agent modes, referring
*    to your own agent by the name you gave it in step 3. of TemplateAgent.cs's first steps!
* 3. Don't forget to set the agent mode to InferenceOnly in the editor.
* 4. Try and see if it works!
* Definitely let me know if there are bugs / things you wanna double check.
**/

public partial class GGBond : CogsAgent
{
    // model for collector agent
    [SerializeField]
    private NNModel m_collectorModel;
    public NNModel CollectorModel 
    {
        get {return m_collectorModel;} // getter method
        set { reinitializeNNAgent(m_collectorModel, value); m_collectorModel = value; }  // set method
    }
    // model for harasser agent
    [SerializeField]
    private NNModel m_harasserModel;

    public NNModel HarasserModel 
    {
        get {return m_harasserModel;} // getter method
        set { reinitializeNNAgent(m_harasserModel, value); m_harasserModel = value; }  // set method
    }
    
    // --------------------AGENT FUNCTIONS-------------------------

    // This function handles the main logic behind which behavior type the 
    // agent takes at any single time. It will specify states which 
    // Heuristic use to take the associated actions.
    public void ActionDeterminingLogic() {
        // start with COLLECTOR mode
        // as soon as our score surpasses the enemy's, turn to harass 
        if (myBase.GetComponent<HomeBase>().GetCaptured() > GetEnemyCaptured())
        {
            agentMode = "HARASSER";
        }
        else if (timer.GetComponent<Timer>().GetTimeRemaning() < 115)
        {
            agentMode = "COLLECTOR";
        } else {
            // this is the default agent mode
            agentMode = "default";
        }
    }

    public void initializeAgentModes() 
    {
        //###################################################
        // SPECIFY WHICH AGENT MODES YOU WANT TO USE HERE!
        // You should instantiate a new instance of your newly defined class 
        // and put into the list; that is, you wanna write:
        // new YOUR_AGENT_CLASS_NAME(this)
        // and add it into the brackets following the "new List<ComponentAgent>()" part.
        allAgentModes = new List<ComponentAgent>();
        allAgentModes.Add(new TemplateAgent(this));
        allAgentModes.Add(new Collector(this, CollectorModel));
        allAgentModes.Add(new Harasser(this, HarasserModel));
        //###################################################
    }

    // -------HELPER FUNCTIONS BELOW!-----------






    






    // ------------------HELPER USEFUL FOR ACTION DETERMINING LOGIC-----------
    [HideInInspector]
    public HomeBase enemyHomeBase;
    public int GetEnemyCaptured() {
        if (enemyHomeBase == null) {
            enemyHomeBase = Array.Find(GameObject.FindGameObjectsWithTag("HomeBase"), 
            (hb => hb.GetComponent<HomeBase>().team == enemy.GetComponent<CogsAgent>().GetTeam())
            ).GetComponent<HomeBase>();
        } 
        return enemyHomeBase.GetCaptured();
    }
}