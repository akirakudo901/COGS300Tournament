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

public partial class GGBond : CogsAgent
{
    // ################################################################
    // USE THIS AS TEMPLATE TO INSTANTIATE YOUR OWN MODEL, TO WHICH YOU 
    // COULD THEN ASSIGN A MODEL FROM THE INSPECTOR!
    // 1. COPY & PASTE THIS TO MAKE A NEW ENTRY.
    // 2. SET THE PRIVATE FIELD NAME TO YOUR CHOICE, e.g. m_MyNNAgentModel
    // 3. CHANGE THE ENTRIES WITHIN THE PUBLIC PROPERTY TO MATCH YOUR NEW
    //    PRIVATE FIELD NAME.
    // 4. SET THE NAME OF YOUR PUBLIC PROPERTY TO YOUR CHOICE, e.g. MyNNAgentModel
    // Then, go down to InitializeAgentModes (also as specified at the top of this
    //    document) and instantiate your Agent example, passing to it MyNNAgentModel
    //    as the nn model.
    [SerializeField]
    private NNModel m_nnTemplateModel;

    public NNModel NNTemplateModel; 
    {
        get {return m_nnTemplateModel;} // getter method; CHANGE PRIVATE FIELD NAME!
        set { reinitializeNNAgent(m_nnTemplateModel, value); m_nnTemplateModel = value; }  // set method; CHANGE PRIVATE FIELD NAME!
    }
    // ################################################################




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

    public void InitializeAgentModes() 
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
        allAgentModes.Add(new TemplateAgent(this));
        allAgentModes.Add(new Collector(this, CollectorModel));
        allAgentModes.Add(new Harasser(this, HarasserModel));
        allAgentModes.Add(new NNTemplateAgent(this, NNTemplateModel));
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