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

/**
* Although we touted using Neural network in the video, our final agent was 
* strongest using purely hard-coded strategies.
* It combines the strength of two strategies:
* 1) It switches between two modes:
*   1 - collect-one-ball-at-a-time mode, where it collects one ball and brings it back to base
*   2 - chase mode, where it will chase the enemy down and shoot at them, while taking their backside
* 2) The agent starts off in the collect-one-ball mode. It will keep collecting balls, except:
*   > if the enemy is carrying more balls than we do, and it faces away, it will chase them
*   > if we have successfully collected 5 balls, we turn to chase
*   > if we have collected more balls than the enemy by the 1:00 mark, we turn to chase
*    Also, under certain conditions, we go back to collector mode:
*   > when the enemy is carrying no balls
*   > when we collect a ball while in chase mode
*   > if we are tied, or are behind, in terms of the number of balls we hold. 
**/

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
    // [SerializeField]
    // private NNModel m_nnTemplateModel;

    // public NNModel NNTemplateModel
    // {
    //     get {return m_nnTemplateModel;} // getter method; CHANGE PRIVATE FIELD NAME!
    //     set { reinitializeNNAgent(m_nnTemplateModel, value); m_nnTemplateModel = value; }  // set method; CHANGE PRIVATE FIELD NAME!
    // }
    // ################################################################




    // model for collector agent
    // [SerializeField]
    // private NNModel m_collectorModel;
    // public NNModel CollectorModel 
    // {
    //     get {return m_collectorModel;} // getter method
    //     set { reinitializeNNAgent(m_collectorModel, value); m_collectorModel = value; }  // set method
    // }
    // // model for harasser agent
    // [SerializeField]
    // private NNModel m_harasserModel;

    // public NNModel HarasserModel 
    // {
    //     get {return m_harasserModel;} // getter method
    //     set { reinitializeNNAgent(m_harasserModel, value); m_harasserModel = value; }  // set method
    // }
    
    // --------------------AGENT FUNCTIONS-------------------------

    // This function handles the main logic behind which behavior type the 
    // agent takes at any single time. It will specify states which 
    // Heuristic use to take the associated actions.
    public virtual void ActionDeterminingLogic() {
        // start with the classic grab one, go back strategy

        // if we are already in Chase mode, only break out when the enemy is frozen
        if ((agentMode == "CHASE_HEURISTIC " && !enemy.GetComponent<CogsAgent>().IsFrozen()) || 
        // if the enemy carries more than us, and are facing away, enter chase mode
            (enemy.GetComponent<CogsAgent>().GetCarrying() > GetCarrying() && 
             EnemyIsFacingAway()) ||
        // if you've collected four objects, also turn to chasing
            (myBase.GetComponent<HomeBase>().GetCaptured() > 5) ||
        // if you've collected more objects than the enemy by the 1:00 mark, also turn to chasing
            (myBase.GetComponent<HomeBase>().GetCaptured() > GetEnemyCaptured() && 
            timer.GetComponent<Timer>().GetTimeRemaning() < 60)
            )
        {
            agentMode = "CHASE_HEURISTIC";
            
            // if we have fallen to equal or less captured number, however, turn to
            // the classic grab one go back strat
            if (myBase.GetComponent<HomeBase>().GetCaptured() <= GetEnemyCaptured())
            {
                agentMode = "CLASSIC_GRAB_ONE_GO_BACK";
            // also, if we stumble upon a target, bring it back to base
            } else if (GetCarrying() > 0) {
                agentMode = "CLASSIC_GRAB_ONE_GO_BACK";
            }
        } else {
            // this is the default agent mode
            agentMode = "CLASSIC_GRAB_ONE_GO_BACK";
        }
    }

    public virtual void InitializeAgentModes() 
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
        allAgentModes.Add(new ChaseHeuristic(this));
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

    public bool EnemyIsFacingAway() {
            float ANGLE_LEEWAY_FOR_BACK = 90f;
            float ANGLE_LEEWAY_FOR_ENEMY_FORWARD = 90f;
            float DISTANCE_LEEWAY = GGBond.LASER_LENGTH * 2;
            // checks if the agent is showing their back to the enemy to punish accordingly
            Vector3 enemyDir = transform.position - GetEnemy().transform.position;
            float enemyAngleToThisAgent = Vector3.SignedAngle(enemyDir, GetEnemy().transform.forward, Vector3.up);
            float distanceToEnemy = Vector3.Distance(transform.localPosition, GetEnemy().transform.localPosition);

            return (//Math.Abs(GetYAngle(GetEnemy())) > ANGLE_LEEWAY_FOR_BACK && 
                    Math.Abs(enemyAngleToThisAgent) > ANGLE_LEEWAY_FOR_ENEMY_FORWARD);
                    // distanceToEnemy < DISTANCE_LEEWAY)
    }

}