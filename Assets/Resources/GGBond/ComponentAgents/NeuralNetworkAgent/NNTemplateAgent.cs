using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

// my code
using CopiedCode;

// ################# HOW TO MAKE YOUR OWN NEURAL NETWORK AGENT #################
// !!!!!!!!!!!! IMPORTANT, PLS READ !!!!!!!!!!!
// DUE TO SOME TECHNICAL DIFFICULTIES, NEURAL NETWORKS LOADED USING THIS CLASS WILL
// BEHAVE SLIGHTLY DIFFERENTLY FROM HOW THEY DO WHEN RUNNING AGENTS IN INFERENCE MODE.
// IT DOES NOT SEEM TO BE A SUBTANTIAL DIFFERENCE, AND SHOULD STILL SHOW FLEXIBILITY
// BUT DOUBLE CHECK IF THE PERFORMANCE DOES NOT SIGNIFIACNTLY DETERIORATE!
/**
* This file shows you how to implement an agent acting based on a neural network!
* I would advise you copy and use this as template!
* 1. Rename the class name to your own class name, e.g. MyNNAgent.
* 2. Rename the constructor (the method named NNTemplateAgent) to your name in 1, e.g MyNNAgent.
* 3. Set a name for your agent which will be used to refer to your agent in GGBond's ActionDeterminingLogic.
*    e.g. NN_TEMPLATE_AGENT, MY_NN_AGENT, etc.
* 4. Change the entries for VECTOR_OBSERVATION_SIZE and NUM_STACKED_VECTOR_OBSERVATIONS as specified below.
* 5. Setup the function GetObservation, so that it matches the CollectObservation function you
*    used to train your neural network.
* 
* Once you do those steps, you can go to GGBond.cs and do the following changes 
* (also specified in GGBond's top):
* 1. Go at the top to find how to set up a new field & inspector window to add your neural network model.
* 2. Go to the InitializeAgentModes function and add a new instance of your class to the list 'allAgentModes',
*    utilizing your newly set field.
* 3. Go to the Unity editor and slot your model into the newly created inspector under GGBond script!
*
*  BELOW WILL BE THE SAME AS WITH SETTING UP HEURISTIC AGENTS! 
* 4. Go to ActionDeterminingLogic and specify your logic to switch between agent modes, referring
*    to your own agent by the name you gave it in step 3. of NNTemplateAgent.cs's first steps!
* 5. Don't forget to set the agent mode to InferenceOnly.
* 6. Try and see if it works!
* Definitely let me know if there are bugs / things you wanna double check.
**/

namespace ComponentAgents
{

    public class NNTemplateAgent : NeuralNetworkAgent 
    {

        // ------------------THINGS YOU WILL WANT TO CHANGE-------------------

        // CHANGE THIS TO YOUR NAME OF CHOICE!
        new public const string name = "NN_TEMPLATE_AGENT";
        
        // CHANGE THIS!
        // size of vector observation to be taken by this neural network;
        // specified as "Space Size" under "Vector Observation" in the 
        // "Behavior Parameters" component menu
        private const int VECTOR_OBSERVATION_SIZE = 59;
        // CHANGE THIS AS WELL!
        // number of vector observation to stack;
        // specified as "Stacked Vectors" under "Vector Observation" in the 
        // "Behavior Parameters" component menu
        private const int NUM_STACKED_VECTOR_OBSERVATIONS = 3;

        // Get relevant information from the environment
        public override void GetObservations(CopiedVectorSensor sensor)
        {
            const float RAY_DISTANCE = 10f;
            // ################################
            // !!!!!!!   TO BE CHNAGED   !!!!!!
            // ################################

            // SET UP THE OBSERVATIONS SUCH THAT THEY MATCH THE OBSERVATIONS 
            // THAT WERE PASSED WHEN YOUR NEURAL NETWORK WAS TRAINED!

            // AS AN EXAMPLE, THE OBSERVATION BELOW WILL CAPTURE A LOT OF INFORMATION,
            // FOR A TOTAL OF 59 VECTOR ENTRIES.
            
            // gets angle & distance to the given game object with respect to this object
            (float yAngle, float distance) getAngleAndDistance(GameObject go) {
                return (
                    ggbond.GetYAngle(go), //angle
                    Vector3.Distance(ggbond.transform.localPosition, go.transform.localPosition) //distance
                    );
            }

            // CODE TAKEN FROM lab2-self-driving IN CarControlAPI.cs! THANKS!
            //Takes in an angle as degrees, where 0 is the front and 180,-180 are the back.
            //Returns a bool indicating whether there was an object within RAY_DISTANCE in the yAngleOffset direction.
            bool Raycast(float yAngleOffset)
            {
                var direction = Quaternion.Euler(0, yAngleOffset, 0) * ggbond.transform.forward;
                var position = new Vector3(ggbond.transform.position.x, ggbond.transform.position.y + 0.5f, ggbond.transform.position.z);

                RaycastHit hit;
                // Does the ray intersect any objects excluding the player layer
                if (Physics.Raycast(position, direction, out hit, RAY_DISTANCE, 1, QueryTriggerInteraction.Ignore))
                {
                    // Debug.DrawRay(position, direction * hit.distance, Color.yellow);
                    return (hit.transform.tag == "Wall");
                }
                else
                {
                    // Debug.DrawRay(position, direction * 50, Color.red);
                    return false;
                }
            }

            // Agent velocity in x and z axis relative to the agent's forward
            var localVelocity = ggbond.transform.InverseTransformDirection(ggbond.GetComponent<Rigidbody>().velocity);
            sensor.AddObservation(localVelocity.x);
            sensor.AddObservation(localVelocity.z);
            
            // Time remaning
            sensor.AddObservation(ggbond.GetTimer().GetComponent<Timer>().GetTimeRemaning());  

            // Agent's current rotation
            var localRotation = ggbond.transform.rotation;
            sensor.AddObservation(localRotation.y);

            // REMOVED AGENT'S ABSOLUTE POSITION!
            // Home base's position relative to agent
            var baseLocation = getAngleAndDistance(ggbond.GetMyBase());
            sensor.AddObservation(baseLocation.yAngle);
            sensor.AddObservation(baseLocation.distance);
        
            // for each target in the environment, add: its position realtive to the player, 
            // whether it is being carried by any team, and whether it is in a base of any team
            foreach (GameObject target in ggbond.GetTargetsInEnvironment()){
                var targetLocation = getAngleAndDistance(target);
                sensor.AddObservation(targetLocation.yAngle);
                sensor.AddObservation(targetLocation.distance);
                sensor.AddObservation(target.GetComponent<Target>().GetCarried()); //indicates the team as int
                sensor.AddObservation(target.GetComponent<Target>().GetInBase()); //indicates the team as int
            }
            
            // Whether the agent is frozen
            sensor.AddObservation(ggbond.IsFrozen());

            // ADDED BY AKIRA: The position of the enemy relative to the player 
            // as well as its front direction, x, z speed, 
            // whether it is frozen and whether it is shooting anything
            // front direction
            var enemyLocalForward = ggbond.transform.InverseTransformDirection(ggbond.GetEnemy().transform.forward);
            sensor.AddObservation(enemyLocalForward.x);
            sensor.AddObservation(enemyLocalForward.z);
            // enemy position
            var enemyLocation = getAngleAndDistance(ggbond.GetEnemy());
            sensor.AddObservation(enemyLocation.yAngle);
            sensor.AddObservation(enemyLocation.distance);
            // enemy speed and angle
            var enemyLocalVelocity = ggbond.transform.InverseTransformDirection(ggbond.GetEnemy().GetComponent<Rigidbody>().velocity);
            sensor.AddObservation(enemyLocalVelocity.x); //x movement relative to this agent
            sensor.AddObservation(enemyLocalVelocity.z); //z movement relative to this agent
            // is the enemy frozen?
            sensor.AddObservation(ggbond.GetEnemy().GetComponent<CogsAgent>().IsFrozen());
            // is the enemy shooting and not frozen (a threat)?
            sensor.AddObservation(ggbond.GetEnemy().GetComponent<CogsAgent>().IsLaserOn() && !ggbond.GetEnemy().GetComponent<CogsAgent>().IsFrozen());

            // Raycast to check if there is a wall in any of the eight directions
            // around the player
            sensor.AddObservation(Raycast(0));
            sensor.AddObservation(Raycast(45));
            sensor.AddObservation(Raycast(90));
            sensor.AddObservation(Raycast(135));
            sensor.AddObservation(Raycast(180));
            sensor.AddObservation(Raycast(-135));
            sensor.AddObservation(Raycast(-90));
            sensor.AddObservation(Raycast(-45));
        }






        // ------------------THINGS YOU MIGHT NOT NEED TO TOUCH-------------------

        public NNTemplateAgent(GGBond instance, NNModel model) : base(instance, model) {}

        // ------------------BASIC MONOBEHAVIOR FUNCTIONS-------------------
        // initialize a neural network to work with, as well as the observation specs
        public override void ComponentAgentOnEnable()
        {
            // call the base's setup - leave it here as it is important!
            base.ComponentAgentOnEnable();
            // sets up the sensors, optionally as specified from the given two integers
            SetUpSensors(vectorObservationSize : VECTOR_OBSERVATION_SIZE, 
            numStackedVectorObservations : NUM_STACKED_VECTOR_OBSERVATIONS);
        }
        
        // --------------------AGENT FUNCTIONS-------------------------


        // Returns the name of this agent
        public override string GetName() {return name;}
    
    }
}
