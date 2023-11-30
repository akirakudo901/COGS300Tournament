using System; // Added for Abs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

// This file implements a "collector" agent.
public partial class GGBond
{
    // whether to shoot when seeing the enemy
    public bool shootWheneverSeeingTheEnemy = true;
    // tracks the number of objects in home base in 
    // order to add the correct reward when the player
    // drops targets in the home base
    private int latestNumTargetInHomeBase = 0;
    // tracks the number of targets carried by the enemy
    // to add bonus when an attack is successful on an
    // enemy player with more objects
    private int latestNumTargetCarriedByEnemy = 0;
    // sensors used to generate observations for this agent mode
    private List<ISensor> collectorSensors;
    // length of the raycast used to detect walls
    private const float RAY_DISTANCE = 10f;
    // length of the laser, as defined with CogsAgent (private so not accessible)
    private const float LASER_LENGTH = 20f;
    // size of vector observation to be taken by this neural network;
    // specified as "Space Size" under "Vector Observation" in the 
    // "Behavior Parameters" component menu
    private const int VECTOR_OBSERVATION_SIZE = 59;
    // number of vector observation to stack;
    // specified as "Stacked Vectors" under "Vector Observation" in the 
    // "Behavior Parameters" component menu
    private const int NUM_STACKED_VECTOR_OBSERVATIONS = 3;

    // ------------------BASIC MONOBEHAVIOR FUNCTIONS-------------------
    // initialize a neural network to work with, as well as the observation specs
    protected void CollectorOnEnable()
    {
        collectorSensors = new List<ISensor>();
        collectorSensors = SetUpGGBondSensors(ref collectorSensors, VECTOR_OBSERVATION_SIZE, NUM_STACKED_VECTOR_OBSERVATIONS);
    }
    
    // --------------------AGENT FUNCTIONS-------------------------

    // Get relevant information from the environment
    public void GetCollectorObservations(VectorSensor sensor)
    {
        // gets angle & distance to the given game object with respect to this object
        (float yAngle, float distance) getAngleAndDistance(GameObject go) {
            return (
                GetYAngle(go), //angle
                Vector3.Distance(transform.localPosition, go.transform.localPosition) //distance
                );
        }

        // CODE TAKEN FROM lab2-self-driving IN CarControlAPI.cs! THANKS!
        //Takes in an angle as degrees, where 0 is the front and 180,-180 are the back.
        //Returns a bool indicating whether there was an object within RAY_DISTANCE in the yAngleOffset direction.
        bool Raycast(float yAngleOffset)
        {
            var direction = Quaternion.Euler(0, yAngleOffset, 0) * transform.forward;
            var position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);

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
        var localVelocity = transform.InverseTransformDirection(rBody.velocity);
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);
        
        // Time remaning
        sensor.AddObservation(timer.GetComponent<Timer>().GetTimeRemaning());  

        // Agent's current rotation
        var localRotation = transform.rotation;
        sensor.AddObservation(localRotation.y);

        // REMOVED AGENT'S ABSOLUTE POSITION!
        // Home base's position relative to agent
        var baseLocation = getAngleAndDistance(myBase);
        sensor.AddObservation(baseLocation.yAngle);
        sensor.AddObservation(baseLocation.distance);
    
        // for each target in the environment, add: its position realtive to the player, 
        // whether it is being carried by any team, and whether it is in a base of any team
        foreach (GameObject target in targets){
            var targetLocation = getAngleAndDistance(target);
            sensor.AddObservation(targetLocation.yAngle);
            sensor.AddObservation(targetLocation.distance);
            sensor.AddObservation(target.GetComponent<Target>().GetCarried()); //indicates the team as int
            sensor.AddObservation(target.GetComponent<Target>().GetInBase()); //indicates the team as int
        }
        
        // Whether the agent is frozen
        sensor.AddObservation(IsFrozen());

        // ADDED BY AKIRA: The position of the enemy relative to the player 
        // as well as its front direction, x, z speed, 
        // whether it is frozen and whether it is shooting anything
        // front direction
        var enemyLocalForward = transform.InverseTransformDirection(enemy.transform.forward);
        sensor.AddObservation(enemyLocalForward.x);
        sensor.AddObservation(enemyLocalForward.z);
        // enemy position
        var enemyLocation = getAngleAndDistance(enemy);
        sensor.AddObservation(enemyLocation.yAngle);
        sensor.AddObservation(enemyLocation.distance);
        // enemy speed and angle
        var enemyLocalVelocity = transform.InverseTransformDirection(enemy.GetComponent<Rigidbody>().velocity);
        sensor.AddObservation(enemyLocalVelocity.x); //x movement relative to this agent
        sensor.AddObservation(enemyLocalVelocity.z); //z movement relative to this agent
        // is the enemy frozen?
        sensor.AddObservation(enemy.GetComponent<CogsAgent>().IsFrozen());
        // is the enemy shooting and not frozen (a threat)?
        sensor.AddObservation(enemy.GetComponent<CogsAgent>().IsLaserOn() && !enemy.GetComponent<CogsAgent>().IsFrozen());

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

    // Decides control of AI based on inputs
    public void CollectorHeuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        
        // If shootWheneverSeeingTheEnemy is true, shoot every time the enemy is right in front
        if (shootWheneverSeeingTheEnemy && GetYAngle(enemy) <= 5 && GetYAngle(enemy) >= -5
        && Vector3.Distance(transform.localPosition, enemy.transform.localPosition) <= LASER_LENGTH) 
        discreteActionsOut[2] = 1;
        
        discreteActionsOut[0] = 1;
    }
    
    //  --------------------------HELPERS---------------------------- 
    // ANYTHING?

    //######################################################################
    //  --------------------------REWARD RELATED----------------------------
    //######################################################################

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
            float distanceToEnemy = Vector3.Distance(transform.localPosition, enemy.transform.localPosition);
            float angleToEnemy = Math.Abs(GetYAngle(enemy));
            if (distanceToEnemy > LASER_LENGTH) {
                AddReward(rewardDict["shoot-outside-laser-range-punish-multiplier"] * (distanceToEnemy - LASER_LENGTH));
            }
            if (angleToEnemy > ANGLE_LEEWAY) {
                AddReward(rewardDict["shoot-not-toward-enemy-punish-multiplier"] * (angleToEnemy - ANGLE_LEEWAY));
            }            
        }
    }

    // ----------------------ONTRIGGER AND ONCOLLISION FUNCTIONS------------------------

    protected void CollectorOnCollisionEnter(Collision collision) 
    {
        //target is not in my base and is not being carried and I am not frozen
        if (collision.gameObject.CompareTag("Target") && collision.gameObject.GetComponent<Target>().GetInBase() != GetTeam() && collision.gameObject.GetComponent<Target>().GetCarried() == 0 && !IsFrozen())
        {
            // add reward for picking up a target
            AddReward(rewardDict["pick-up-target"]);
            // add bonus for picking up a taget from the enemy base
            // *0 is the default tag for a target that is not in any base
            if (collision.gameObject.GetComponent<Target>().GetInBase() != 0) AddReward(rewardDict["bonus-stealing-from-enemy"]);
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            //add a punishment for knowing into a wall
            AddReward(rewardDict["bump-into-wall"]);
        }
    }

    //  --------------------------HELPERS---------------------------- 
     private void CollectorAssignBasicRewards() {
        rewardDict = new Dictionary<string, float>();
        
        float rewardHittingWithLaser = 0.0075f;
        float targetPickUp = 0.0075f;
        float stealingBonus = 0.0075f;

        rewardDict.Add("hit-enemy", rewardHittingWithLaser); //reward for hitting an enemy with the laser
        rewardDict.Add("frozen",   -rewardHittingWithLaser); //punishment per tick being frozen
        rewardDict.Add("shooting-laser", -0.0001f); //punishment for the act of shooting lasers (since it might lose you time)
        rewardDict.Add("dropped-one-target", -rewardHittingWithLaser); // punishment per target dropped when shot by laser
        // rewardDict.Add("dropped-targets", 0f); // punishment when hit by laser (same to freeze?)
        // added by AKIRA:
        rewardDict.Add("carry-one-target-back-to-base", 0.01f); // reward per target brought back to base
        rewardDict.Add("pick-up-target", targetPickUp); // reward when picking up a target
        rewardDict.Add("bump-into-wall", -0.005f); // punishment for bumping into walls
        rewardDict.Add("enemy-stole-one-target", -(stealingBonus + targetPickUp)); // punishment when the enemy steals a target from your base
        rewardDict.Add("bonus-stealing-from-enemy", stealingBonus); // bonus for picking up a target in the enemy base
        //bonus for hitting the enemy and making them drop balls (per target made drop)
        rewardDict.Add("bonus-per-target-made-drop-when-hitting-the-enemy", rewardHittingWithLaser); 

        // punishment to shape the agent's actions
        // punish for firing when the enemy is farther than the laser range
        // punishment is multiplier * (distance to enemy - LASER_LENGTH)
        rewardDict.Add("shoot-outside-laser-range-punish-multiplier", -0.0001f);
        // punish for firing when you are not facing the enemy
        // punishment is multiplier * (abs(angle to enemy) - ANGLE_LEEWAY)
        rewardDict.Add("shoot-not-toward-enemy-punish-multiplier", -0.00002f);
        // punish for showing your backside to the enemy while they are facing you
        // punishment is roughly:
        // multiplier * (abs(angle to enemy) - LEEWAY1)  <- how much your showing your back
        //   * (LEEWAY2 - abs(enemy angle to you))       <- how much the enemy faces you
        //   * (DISTANCE_LEEWAY - distance to enemy)     <- how close the enemy is (closer -> more punish)
        rewardDict.Add("punishment-showing-back-to-enemy", -0.0000003f);
    }

    private void checkStateOfTargetsInBase() {
        int currentNumTargetInHomeBase = myBase.GetComponent<HomeBase>().GetCaptured();
        // if the number of objects changed
        if (currentNumTargetInHomeBase != latestNumTargetInHomeBase) {
        
            // if the number increased, we gathered more targets; reward agent for this
            if (currentNumTargetInHomeBase > latestNumTargetInHomeBase) { 
                AddReward(rewardDict["carry-one-target-back-to-base"] * (currentNumTargetInHomeBase - latestNumTargetInHomeBase));
            // if the number decreased, the enemy stole some; punish the agent for this
            } else if (currentNumTargetInHomeBase < latestNumTargetInHomeBase) {
                AddReward(rewardDict["enemy-stole-one-target"] * (latestNumTargetInHomeBase - currentNumTargetInHomeBase));
            }
            // TODO NOTES: THIS DOES NOT DEAL WITH THE VERY RARE CASE WHERE THE ENEMY STOLE
            // A TARGET AT THE SAME TIME AS WE DROP THE SAME NUMBER. HOPEFULLY ISN'T A BIG DEAL. 
        }
        // update latestNumTargetInHomeBase
        latestNumTargetInHomeBase = currentNumTargetInHomeBase;
    }

    private void checkStateOfEnemyCarrying() {
        int currentNumTargetCarriedByEnemy = enemy.GetComponent<CogsAgent>().GetCarrying();

        // if the number of objects changed
        if (currentNumTargetCarriedByEnemy != latestNumTargetCarriedByEnemy) { 

            // add reward for hitting the enemy proportional to the number of objects they had
            if (currentNumTargetCarriedByEnemy == 0 && enemy.GetComponent<CogsAgent>().IsFrozen()) { 
                AddReward(rewardDict["bonus-per-target-made-drop-when-hitting-the-enemy"] * (latestNumTargetCarriedByEnemy));
            }
        }
        // update latestNumTargetCarriedByEnemy
        latestNumTargetCarriedByEnemy = currentNumTargetCarriedByEnemy;
    }

    private void checkIfShowingBackToEnemy() {
        float ANGLE_LEEWAY_FOR_BACK = 90f;
        float ANGLE_LEEWAY_FOR_ENEMY_FORWARD = 45f;
        float DISTANCE_LEEWAY = LASER_LENGTH * 2;
        // checks if the agent is showing their back to the enemy to punish accordingly
        Vector3 enemyDir = transform.position - enemy.transform.position;
        float enemyAngleToThisAgent = Vector3.SignedAngle(enemyDir, enemy.transform.forward, Vector3.up);
        float distanceToEnemy = Vector3.Distance(transform.localPosition, enemy.transform.localPosition);
    
        if (Math.Abs(GetYAngle(enemy)) > ANGLE_LEEWAY_FOR_BACK && 
        Math.Abs(enemyAngleToThisAgent) < ANGLE_LEEWAY_FOR_ENEMY_FORWARD && 
        distanceToEnemy < DISTANCE_LEEWAY) {
            float r = (rewardDict["punishment-showing-back-to-enemy"] * 
            (Math.Abs(GetYAngle(enemy)) - ANGLE_LEEWAY_FOR_BACK) * 
            (ANGLE_LEEWAY_FOR_ENEMY_FORWARD - Math.Abs(enemyAngleToThisAgent)) * 
            (DISTANCE_LEEWAY - distanceToEnemy));
            AddReward(r);
        }
    }

}
