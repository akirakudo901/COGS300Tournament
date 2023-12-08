using System; // Added for Abs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

// my code
using CopiedCode;

// This file implements a "collector" agent.
namespace ComponentAgents
{

    public class Collector : NeuralNetworkAgent 
    {
        new public const string name = "COLLECTOR";
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


        public Collector(GGBond instance, NNModel model) : base(instance, model) {}

        // ------------------BASIC MONOBEHAVIOR FUNCTIONS-------------------
        // initialize a neural network to work with, as well as the observation specs
        public override void ComponentAgentOnEnable()
        {
            // call the base's setup
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
            var localVelocity = ggbond.transform.InverseTransformDirection(
                ggbond.GetComponent<Rigidbody>().velocity);
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

        //  --------------------------HELPERS---------------------------- 
        // ANYTHING?

        //######################################################################
        //  --------------------------REWARD RELATED----------------------------
        //######################################################################

        // tracks the number of objects in home base in 
        // order to add the correct reward when the player
        // drops targets in the home base
        private int latestNumTargetInHomeBase = 0;
        // tracks the number of targets carried by the enemy
        // to add bonus when an attack is successful on an
        // enemy player with more objects
        private int latestNumTargetCarriedByEnemy = 0;


        // ------------------BASIC MONOBEHAVIOR FUNCTIONS-------------------
        
        // Fixed updates specific to the collector agent, handling reward related
        protected void CollectorFixedUpdate() {
            // update the number of targets in the home base
            checkStateOfTargetsInBase();
            // update the number of targets carried by the enemy
            checkStateOfEnemyCarrying();
            // check whether we are showing our back to shape behavior with rewards
            checkIfShowingBackToEnemy();
        }

        // --------------------AGENT FUNCTIONS-------------------------

        private void CollectorMovePlayer(int forwardAxis, int rotateAxis, int shootAxis, int goToTargetAxis, int goToBaseAxis)
        {
            if (shootAxis == 1) {
                // add punishment if we are shooting either when far from the enemy
                // or when we are facing away from the enemy
                float ANGLE_LEEWAY = 5f;
                float distanceToEnemy = Vector3.Distance(ggbond.transform.localPosition, ggbond.GetEnemy().transform.localPosition);
                float angleToEnemy = Math.Abs(ggbond.GetYAngle(ggbond.GetEnemy()));
                if (distanceToEnemy > GGBond.LASER_LENGTH) {
                    ggbond.AddReward(ggbond.GetRewardDict()["shoot-outside-laser-range-punish-multiplier"] * (distanceToEnemy - GGBond.LASER_LENGTH));
                }
                if (angleToEnemy > ANGLE_LEEWAY) {
                    ggbond.AddReward(ggbond.GetRewardDict()["shoot-not-toward-enemy-punish-multiplier"] * (angleToEnemy - ANGLE_LEEWAY));
                }            
            }
        }

        // ----------------------ONTRIGGER AND ONCOLLISION FUNCTIONS------------------------

        protected void CollectorOnCollisionEnter(Collision collision) 
        {
            //target is not in my base and is not being carried and I am not frozen
            if (collision.gameObject.CompareTag("Target") 
            && collision.gameObject.GetComponent<Target>().GetInBase() != ggbond.GetTeam() 
            && collision.gameObject.GetComponent<Target>().GetCarried() == 0 && !ggbond.IsFrozen())
            {
                // add reward for picking up a target
                ggbond.AddReward(ggbond.GetRewardDict()["pick-up-target"]);
                // add bonus for picking up a taget from the enemy base
                // *0 is the default tag for a target that is not in any base
                if (collision.gameObject.GetComponent<Target>().GetInBase() != 0) {
                    ggbond.AddReward(ggbond.GetRewardDict()["bonus-stealing-from-enemy"]);
                }
            }

            if (collision.gameObject.CompareTag("Wall"))
            {
                //add a punishment for knowing into a wall
                ggbond.AddReward(ggbond.GetRewardDict()["bump-into-wall"]);
            }
            // base.OnCollisionEnter(collision);
        }

        //  --------------------------HELPERS---------------------------- 
        private void CollectorAssignBasicRewards() {            
            float rewardHittingWithLaser = 0.0075f;
            float targetPickUp = 0.0075f;
            float stealingBonus = 0.0075f;

            ggbond.GetRewardDict().Add("hit-enemy", rewardHittingWithLaser); //reward for hitting an enemy with the laser
            ggbond.GetRewardDict().Add("frozen",   -rewardHittingWithLaser); //punishment per tick being frozen
            ggbond.GetRewardDict().Add("shooting-laser", -0.0001f); //punishment for the act of shooting lasers (since it might lose you time)
            ggbond.GetRewardDict().Add("dropped-one-target", -rewardHittingWithLaser); // punishment per target dropped when shot by laser
            // GetRewardDict().Add("dropped-targets", 0f); // punishment when hit by laser (same to freeze?)
            // added by AKIRA:
            ggbond.GetRewardDict().Add("carry-one-target-back-to-base", 0.01f); // reward per target brought back to base
            ggbond.GetRewardDict().Add("pick-up-target", targetPickUp); // reward when picking up a target
            ggbond.GetRewardDict().Add("bump-into-wall", -0.005f); // punishment for bumping into walls
            ggbond.GetRewardDict().Add("enemy-stole-one-target", -(stealingBonus + targetPickUp)); // punishment when the enemy steals a target from your base
            ggbond.GetRewardDict().Add("bonus-stealing-from-enemy", stealingBonus); // bonus for picking up a target in the enemy base
            //bonus for hitting the enemy and making them drop balls (per target made drop)
            ggbond.GetRewardDict().Add("bonus-per-target-made-drop-when-hitting-the-enemy", rewardHittingWithLaser); 

            // punishment to shape the agent's actions
            // punish for firing when the enemy is farther than the laser range
            // punishment is multiplier * (distance to enemy - GGBond.LASER_LENGTH)
            ggbond.GetRewardDict().Add("shoot-outside-laser-range-punish-multiplier", -0.0001f);
            // punish for firing when you are not facing the enemy
            // punishment is multiplier * (abs(angle to enemy) - ANGLE_LEEWAY)
            ggbond.GetRewardDict().Add("shoot-not-toward-enemy-punish-multiplier", -0.00002f);
            // punish for showing your backside to the enemy while they are facing you
            // punishment is roughly:
            // multiplier * (abs(angle to enemy) - LEEWAY1)  <- how much your showing your back
            //   * (LEEWAY2 - abs(enemy angle to you))       <- how much the enemy faces you
            //   * (DISTANCE_LEEWAY - distance to enemy)     <- how close the enemy is (closer -> more punish)
            ggbond.GetRewardDict().Add("punishment-showing-back-to-enemy", -0.0000003f);
        }

        private void checkStateOfTargetsInBase() {
            int currentNumTargetInHomeBase = ggbond.GetMyBase().GetComponent<HomeBase>().GetCaptured();
            // if the number of objects changed
            if (currentNumTargetInHomeBase != latestNumTargetInHomeBase) {
            
                // if the number increased, we gathered more targets; reward agent for this
                if (currentNumTargetInHomeBase > latestNumTargetInHomeBase) { 
                    ggbond.AddReward(ggbond.GetRewardDict()["carry-one-target-back-to-base"] * (currentNumTargetInHomeBase - latestNumTargetInHomeBase));
                // if the number decreased, the enemy stole some; punish the agent for this
                } else if (currentNumTargetInHomeBase < latestNumTargetInHomeBase) {
                    ggbond.AddReward(ggbond.GetRewardDict()["enemy-stole-one-target"] * (latestNumTargetInHomeBase - currentNumTargetInHomeBase));
                }
                // TODO NOTES: THIS DOES NOT DEAL WITH THE VERY RARE CASE WHERE THE ENEMY STOLE
                // A TARGET AT THE SAME TIME AS WE DROP THE SAME NUMBER. HOPEFULLY ISN'T A BIG DEAL. 
            }
            // update latestNumTargetInHomeBase
            latestNumTargetInHomeBase = currentNumTargetInHomeBase;
        }

        private void checkStateOfEnemyCarrying() {
            int currentNumTargetCarriedByEnemy = ggbond.GetEnemy().GetComponent<CogsAgent>().GetCarrying();

            // if the number of objects changed
            if (currentNumTargetCarriedByEnemy != latestNumTargetCarriedByEnemy) { 

                // add reward for hitting the enemy proportional to the number of objects they had
                if (currentNumTargetCarriedByEnemy == 0 && ggbond.GetEnemy().GetComponent<CogsAgent>().IsFrozen()) { 
                    ggbond.AddReward(ggbond.GetRewardDict()["bonus-per-target-made-drop-when-hitting-the-enemy"] * (latestNumTargetCarriedByEnemy));
                }
            }
            // update latestNumTargetCarriedByEnemy
            latestNumTargetCarriedByEnemy = currentNumTargetCarriedByEnemy;
        }

        private void checkIfShowingBackToEnemy() {
            float ANGLE_LEEWAY_FOR_BACK = 90f;
            float ANGLE_LEEWAY_FOR_ENEMY_FORWARD = 45f;
            float DISTANCE_LEEWAY = GGBond.LASER_LENGTH * 2;
            // checks if the agent is showing their back to the enemy to punish accordingly
            Vector3 enemyDir = ggbond.transform.position - ggbond.GetEnemy().transform.position;
            float enemyAngleToThisAgent = Vector3.SignedAngle(enemyDir, ggbond.GetEnemy().transform.forward, Vector3.up);
            float distanceToEnemy = Vector3.Distance(ggbond.transform.localPosition, ggbond.GetEnemy().transform.localPosition);
        
            if (Math.Abs(ggbond.GetYAngle(ggbond.GetEnemy())) > ANGLE_LEEWAY_FOR_BACK && 
            Math.Abs(enemyAngleToThisAgent) < ANGLE_LEEWAY_FOR_ENEMY_FORWARD && 
            distanceToEnemy < DISTANCE_LEEWAY) {
                float r = (ggbond.GetRewardDict()["punishment-showing-back-to-enemy"] * 
                (Math.Abs(ggbond.GetYAngle(ggbond.GetEnemy())) - ANGLE_LEEWAY_FOR_BACK) * 
                (ANGLE_LEEWAY_FOR_ENEMY_FORWARD - Math.Abs(enemyAngleToThisAgent)) * 
                (DISTANCE_LEEWAY - distanceToEnemy));
                ggbond.AddReward(r);
            }
        }
    }

}
