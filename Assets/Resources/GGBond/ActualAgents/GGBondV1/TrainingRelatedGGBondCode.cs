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
using CopiedCode;

// This file holds the almost original agent code, excluding everything related
// to handling the action determining logic. This should generally not change anymore!

public partial class GGBond : CogsAgent
{
    // // tracks the number of objects in home base in 
    // // order to add the correct reward when the player
    // // drops targets in the home base
    // private int latestNumTargetInHomeBase = 0;
    // // tracks the number of targets carried by the enemy
    // // to add bonus when an attack is successful on an
    // // enemy player with more objects
    // private int latestNumTargetCarriedByEnemy = 0;
    
    // // --------------------AGENT FUNCTIONS-------------------------

    // // An optional way to mask some of the actions
    // // for example, one can use it to let the agent shoot in a hard-coded way, and
    // // the agent has to learn in a world where shooting occurs then and only then
    // public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    // {
    //     actionMask.SetActionEnabled(2, 1, false); //this disables shooting
    // }

    // // Get relevant information from the environment to effectively learn behavior
    // private const float RAY_DISTANCE = 10f;
    // public override void CollectObservations(VectorSensor sensor)
    // {
    //     // DEFAULT OBSERVATIONS CODE STORED AT THE END OF SCRIPT!
    //     // gets angle & distance to the given game object with respect to this object
    //     (float yAngle, float distance) getAngleAndDistance(GameObject go) {
    //         return (
    //             GetYAngle(go), //angle
    //             Vector3.Distance(transform.localPosition, go.transform.localPosition) //distance
    //             );
    //     }

    //     // CODE TAKEN FROM lab2-self-driving IN CarControlAPI.cs! THANKS!
    //     //Takes in an angle as degrees, where 0 is the front and 180,-180 are the back.
    //     //Returns a bool indicating whether there was an object within RAY_DISTANCE in the yAngleOffset direction.
    //     bool Raycast(float yAngleOffset)
    //     {
    //         var direction = Quaternion.Euler(0, yAngleOffset, 0) * transform.forward;
    //         var position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);

    //         RaycastHit hit;
    //         // Does the ray intersect any objects excluding the player layer
    //         if (Physics.Raycast(position, direction, out hit, RAY_DISTANCE, 1, QueryTriggerInteraction.Ignore))
    //         {
    //             // Debug.DrawRay(position, direction * hit.distance, Color.yellow);
    //             return (hit.transform.tag == "Wall");
    //         }
    //         else
    //         {
    //             // Debug.DrawRay(position, direction * 50, Color.red);
    //             return false;
    //         }
    //     }

    //     // Agent velocity in x and z axis relative to the agent's forward
    //     var localVelocity = transform.InverseTransformDirection(rBody.velocity);
    //     sensor.AddObservation(localVelocity.x);
    //     sensor.AddObservation(localVelocity.z);
        
    //     // Time remaning
    //     sensor.AddObservation(timer.GetComponent<Timer>().GetTimeRemaning());  

    //     // Agent's current rotation
    //     var localRotation = transform.rotation;
    //     sensor.AddObservation(localRotation.y);

    //     // REMOVED AGENT'S ABSOLUTE POSITION!
    //     // Home base's position relative to agent
    //     var baseLocation = getAngleAndDistance(myBase);
    //     sensor.AddObservation(baseLocation.yAngle);
    //     sensor.AddObservation(baseLocation.distance);
    
    //     // for each target in the environment, add: its position realtive to the player, 
    //     // whether it is being carried by any team, and whether it is in a base of any team
    //     foreach (GameObject target in targets){
    //         var targetLocation = getAngleAndDistance(target);
    //         sensor.AddObservation(targetLocation.yAngle);
    //         sensor.AddObservation(targetLocation.distance);
    //         sensor.AddObservation(target.GetComponent<Target>().GetCarried()); //indicates the team as int
    //         sensor.AddObservation(target.GetComponent<Target>().GetInBase()); //indicates the team as int
    //     }
        
    //     // Whether the agent is frozen
    //     sensor.AddObservation(IsFrozen());

    //     // ADDED BY AKIRA: The position of the enemy relative to the player 
    //     // as well as its front direction, x, z speed, 
    //     // whether it is frozen and whether it is shooting anything
    //     // front direction
    //     var enemyLocalForward = transform.InverseTransformDirection(enemy.transform.forward);
    //     sensor.AddObservation(enemyLocalForward.x);
    //     sensor.AddObservation(enemyLocalForward.z);
    //     // enemy position
    //     var enemyLocation = getAngleAndDistance(enemy);
    //     sensor.AddObservation(enemyLocation.yAngle);
    //     sensor.AddObservation(enemyLocation.distance);
    //     // enemy speed and angle
    //     var enemyLocalVelocity = transform.InverseTransformDirection(enemy.GetComponent<Rigidbody>().velocity);
    //     sensor.AddObservation(enemyLocalVelocity.x); //x movement relative to this agent
    //     sensor.AddObservation(enemyLocalVelocity.z); //z movement relative to this agent
    //     // is the enemy frozen?
    //     sensor.AddObservation(enemy.GetComponent<CogsAgent>().IsFrozen());
    //     // is the enemy shooting and not frozen (a threat)?
    //     sensor.AddObservation(enemy.GetComponent<CogsAgent>().IsLaserOn() && !enemy.GetComponent<CogsAgent>().IsFrozen());

    //     // Raycast to check if there is a wall in any of the eight directions
    //     // around the player
    //     sensor.AddObservation(Raycast(0));
    //     sensor.AddObservation(Raycast(45));
    //     sensor.AddObservation(Raycast(90));
    //     sensor.AddObservation(Raycast(135));
    //     sensor.AddObservation(Raycast(180));
    //     sensor.AddObservation(Raycast(-135));
    //     sensor.AddObservation(Raycast(-90));
    //     sensor.AddObservation(Raycast(-45));
    // }

    // // Fixed updates specific to the collector agent, handling reward related
    // protected void RewardRelatedFixedUpdate() {
    //     // update the number of targets in the home base
    //     checkStateOfTargetsInBase();
    //     // update the number of targets carried by the enemy
    //     checkStateOfEnemyCarrying();
    //     // check whether we are showing our back to shape behavior with rewards
    //     checkIfShowingBackToEnemy();
    //     // passively punish for every frame to encourage moving
    //     AddReward(rewardDict["passive-punishment-per-step"]);
    // }
    

    // // ----------------------ONTRIGGER AND ONCOLLISION FUNCTIONS------------------------
    // // Called when object collides with or trigger (similar to collide but without physics) other objects
    // protected override void OnTriggerEnter(Collider collision)
    // {
        
    //     if (collision.gameObject.CompareTag("HomeBase") && collision.gameObject.GetComponent<HomeBase>().team == GetTeam())
    //     {
    //         //Add rewards here for when the agent goes back to the homebase
    //         // if there is any target carried, those would be dropped
    //         // add rewards for the dropped targets
    //         // WANTED TO DEAL WITH THIS HERE BUT COLLISION BY HOME BASE IS PROCESSED FIRST, 
    //         // SO I WILL LEAVE THIS REWARD TO checkStateOfTargetsInbase.
    //     }
    //     base.OnTriggerEnter(collision);
    // }

    // protected override void OnCollisionEnter(Collision collision) 
    // {
    //     //target is not in my base and is not being carried and I am not frozen
    //     if (collision.gameObject.CompareTag("Target") 
    //     && collision.gameObject.GetComponent<Target>().GetInBase() != GetTeam() 
    //     && collision.gameObject.GetComponent<Target>().GetCarried() == 0 && !IsFrozen())
    //     {
    //         // add reward for picking up a target
    //         AddReward(rewardDict["pick-up-target"]);
    //         // add bonus for picking up a taget from the enemy base
    //         // *0 is the default tag for a target that is not in any base
    //         if (collision.gameObject.GetComponent<Target>().GetInBase() != 0) {
    //             AddReward(rewardDict["bonus-stealing-from-enemy"]);
    //         }
    //     }

    //     if (collision.gameObject.CompareTag("Wall"))
    //     {
    //         //add a punishment for knowing into a wall
    //         AddReward(rewardDict["bump-into-wall"]);
    //     }
    //     base.OnCollisionEnter(collision);
    // }

    // //  --------------------------HELPERS---------------------------- 

    // // assigns basic rewards
     private void AssignBasicRewards() {
    //     // FOR DIFFERENT AGENT IMPLEMENTATIONS
    //     // only used when training NNs - if training, choose one implementation from DifferentAgents
        rewardDict = new Dictionary<string, float>();

    //     float rewardHittingWithLaser = 0.0075f;
    //     float targetPickUp = 0.0075f;
    //     float stealingBonus = 0.0075f;

    //     rewardDict.Add("hit-enemy", rewardHittingWithLaser); //reward for hitting an enemy with the laser
    //     rewardDict.Add("frozen",   -rewardHittingWithLaser); //punishment per tick being frozen
    //     // shooting-laser: -0.0001f when careful reward.
    //     rewardDict.Add("shooting-laser", 0f); //punishment for the act of shooting lasers (since it might lose you time)
    //     rewardDict.Add("dropped-one-target", -rewardHittingWithLaser); // punishment per target dropped when shot by laser
    //     // rewardDict.Add("dropped-targets", 0f); // punishment when hit by laser (same to freeze?)
    //     // added by AKIRA:
    //     rewardDict.Add("carry-one-target-back-to-base", 0.01f); // reward per target brought back to base
    //     rewardDict.Add("pick-up-target", targetPickUp); // reward when picking up a target
    //     rewardDict.Add("bump-into-wall", -0.005f); // punishment for bumping into walls
    //     rewardDict.Add("enemy-stole-one-target", -(stealingBonus + targetPickUp)); // punishment when the enemy steals a target from your base
    //     rewardDict.Add("bonus-stealing-from-enemy", stealingBonus); // bonus for picking up a target in the enemy base
    //     //bonus for hitting the enemy and making them drop balls (per target made drop)
    //     rewardDict.Add("bonus-per-target-made-drop-when-hitting-the-enemy", rewardHittingWithLaser); 

    //     // punishment to shape the agent's actions
    //     // punish for firing when the enemy is farther than the laser range
    //     // punishment is multiplier * (distance to enemy - LASER_LENGTH)
    //     // rewardDict.Add("shoot-outside-laser-range-punish-multiplier", -0.0001f); //not in effect now
    //     // punish for firing when you are not facing the enemy
    //     // punishment is multiplier * (abs(angle to enemy) - ANGLE_LEEWAY)
    //     // rewardDict.Add("shoot-not-toward-enemy-punish-multiplier", -0.00002f); //not in effect now
    //     // punish for showing your backside to the enemy while they are facing you
    //     // punishment is roughly:
    //     // multiplier * (abs(angle to enemy) - LEEWAY1)  <- how much your showing your back
    //     //   * (LEEWAY2 - abs(enemy angle to you))       <- how much the enemy faces you
    //     //   * (DISTANCE_LEEWAY - distance to enemy)     <- how close the enemy is (closer -> more punish)
    //     rewardDict.Add("punishment-showing-back-to-enemy", -0.0000003f);
    //     // constant punishment to encourage moving
    //     rewardDict.Add("passive-punishment-per-step", -0.00001f);
    }


    // // ------------IMPLEMENTATION SPECIFIC FUNCTIONS------------------

    // private void checkStateOfTargetsInBase() {
    //     int currentNumTargetInHomeBase = myBase.GetComponent<HomeBase>().GetCaptured();
    //     // if the number of objects changed
    //     if (currentNumTargetInHomeBase != latestNumTargetInHomeBase) {
        
    //         // if the number increased, we gathered more targets; reward agent for this
    //         if (currentNumTargetInHomeBase > latestNumTargetInHomeBase) { 
    //             AddReward(rewardDict["carry-one-target-back-to-base"] * (currentNumTargetInHomeBase - latestNumTargetInHomeBase));
    //         // if the number decreased, the enemy stole some; punish the agent for this
    //         } else if (currentNumTargetInHomeBase < latestNumTargetInHomeBase) {
    //             AddReward(rewardDict["enemy-stole-one-target"] * (latestNumTargetInHomeBase - currentNumTargetInHomeBase));
    //         }
    //         // TODO NOTES: THIS DOES NOT DEAL WITH THE VERY RARE CASE WHERE THE ENEMY STOLE
    //         // A TARGET AT THE SAME TIME AS WE DROP THE SAME NUMBER. HOPEFULLY ISN'T A BIG DEAL. 
    //     }
    //     // update latestNumTargetInHomeBase
    //     latestNumTargetInHomeBase = currentNumTargetInHomeBase;
    // }

    // private void checkStateOfEnemyCarrying() {
    //     int currentNumTargetCarriedByEnemy = enemy.GetComponent<CogsAgent>().GetCarrying();

    //     // if the number of objects changed
    //     if (currentNumTargetCarriedByEnemy != latestNumTargetCarriedByEnemy) { 

    //         // add reward for hitting the enemy proportional to the number of objects they had
    //         if (currentNumTargetCarriedByEnemy == 0 && enemy.GetComponent<CogsAgent>().IsFrozen()) { 
    //             AddReward(rewardDict["bonus-per-target-made-drop-when-hitting-the-enemy"] * (latestNumTargetCarriedByEnemy));
    //         }
    //     }
    //     // update latestNumTargetCarriedByEnemy
    //     latestNumTargetCarriedByEnemy = currentNumTargetCarriedByEnemy;
    // }

    // private void checkIfShowingBackToEnemy() {
    //     float ANGLE_LEEWAY_FOR_BACK = 90f;
    //     float ANGLE_LEEWAY_FOR_ENEMY_FORWARD = 45f;
    //     float DISTANCE_LEEWAY = LASER_LENGTH * 2;
    //     // checks if the agent is showing their back to the enemy to punish accordingly
    //     Vector3 enemyDir = transform.position - enemy.transform.position;
    //     float enemyAngleToThisAgent = Vector3.SignedAngle(enemyDir, enemy.transform.forward, Vector3.up);
    //     float distanceToEnemy = Vector3.Distance(transform.localPosition, enemy.transform.localPosition);
    
    //     if (Math.Abs(GetYAngle(enemy)) > ANGLE_LEEWAY_FOR_BACK && 
    //     Math.Abs(enemyAngleToThisAgent) < ANGLE_LEEWAY_FOR_ENEMY_FORWARD && 
    //     distanceToEnemy < DISTANCE_LEEWAY) {
    //         float r = (rewardDict["punishment-showing-back-to-enemy"] * 
    //         (Math.Abs(GetYAngle(enemy)) - ANGLE_LEEWAY_FOR_BACK) * 
    //         (ANGLE_LEEWAY_FOR_ENEMY_FORWARD - Math.Abs(enemyAngleToThisAgent)) * 
    //         (DISTANCE_LEEWAY - distanceToEnemy));
    //         AddReward(r);
    //     }
    // }

    // // getters and setters for the reward dict
    public Dictionary<string, float> GetRewardDict() {return rewardDict;}

}


// // this was the default observation
// // Agent velocity in x and z axis relative to the agent's forward
// var localVelocity = transform.InverseTransformDirection(rBody.velocity);
// sensor.AddObservation(localVelocity.x);
// sensor.AddObservation(localVelocity.z);

// // Time remaning
// sensor.AddObservation(timer.GetComponent<Timer>().GetTimeRemaning());  

// // Agent's current rotation
// var localRotation = transform.rotation;
// sensor.AddObservation(transform.rotation.y);

// // Agent and home base's position
// sensor.AddObservation(this.transform.localPosition);
// sensor.AddObservation(baseLocation.localPosition);

// // for each target in the environment, add: its position, whether it is being carried,
// // and whether it is in a base
// foreach (GameObject target in targets){
//     sensor.AddObservation(target.transform.localPosition);
//     sensor.AddObservation(target.GetComponent<Target>().GetCarried());
//     sensor.AddObservation(target.GetComponent<Target>().GetInBase());
// }

// // Whether the agent is frozen
// sensor.AddObservation(IsFrozen());