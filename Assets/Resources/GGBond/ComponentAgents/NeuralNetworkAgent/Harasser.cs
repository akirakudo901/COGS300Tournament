using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

// my code
using CopiedCode;

// This file implements a "harasser" agent.
namespace ComponentAgents
{

    public class Harasser : NeuralNetworkAgent 
    {
        new public const string name = "HARASSER";
        // size of vector observation to be taken by this neural network;
        // specified as "Space Size" under "Vector Observation" in the 
        // "Behavior Parameters" component menu
        private const int VECTOR_OBSERVATION_SIZE = 59;
        // number of vector observation to stack;
        // specified as "Stacked Vectors" under "Vector Observation" in the 
        // "Behavior Parameters" component menu
        private const int NUM_STACKED_VECTOR_OBSERVATIONS = 3;
        // length of the raycast used to detect walls
        private const float RAY_DISTANCE = 10f;


        public Harasser(GGBond instance, NNModel model) : base(instance, model) {}

        // ------------------BASIC MONOBEHAVIOR FUNCTIONS-------------------
        // initialize a neural network to work with, as well as the observation specs
        public override void ComponentAgentOnEnable()
        {
            base.ComponentAgentOnEnable();
            
            SetUpSensors(vectorObservationSize : VECTOR_OBSERVATION_SIZE, 
            numStackedVectorObservations : NUM_STACKED_VECTOR_OBSERVATIONS);
        }
        
        // --------------------AGENT FUNCTIONS-------------------------

        // Get relevant information from the environment
        public override void GetObservations(CopiedVectorSensor sensor)
        {
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

        // Returns the name of this agent
        public override string GetName() {return name;}

    }

}
