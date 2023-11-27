using System; // Added for Abs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class GGBond : CogsAgent
{
    // tracks the number of objects in home base in 
    // order to add the correct reward when the player
    // drops targets in the home base
    private int latestNumTargetInHomeBase = 0;
    // tracks the number of targets carried by the enemy
    // to add bonus when an attack is successful on an
    // enemy player with more objects
    private int latestNumTargetCarriedByEnemy = 0;
    // length of the raycast used to detect walls
    private const float RAY_DISTANCE = 10f;
    // length of the laser, as defined with CogsAgent (private so not accessible)
    private const float LASER_LENGTH = 20f;

    // ------------------BASIC MONOBEHAVIOR FUNCTIONS-------------------
    
    // Initialize values
    protected override void Start()
    {
        base.Start();
        AssignBasicRewards();
        latestNumTargetInHomeBase = 0;
        latestNumTargetCarriedByEnemy = 0;
    }

    // For actual actions in the environment (e.g. movement, shoot laser)
    // that is done continuously
    protected override void FixedUpdate() {
        base.FixedUpdate();
        
        LaserControl();
        // Movement based on DirToGo and RotateDir
        moveAgent(dirToGo, rotateDir);
        // update the number of targets in the home base
        checkStateOfTargetsInBase();
        // update the number of targets carried by the enemy
        checkStateOfEnemyCarrying();
        // check whether we are showing our back to shape behavior with rewards
        checkIfShowingBackToEnemy();
    }


    
    // --------------------AGENT FUNCTIONS-------------------------

    // Get relevant information from the environment to effectively learn behavior
    public override void CollectObservations(VectorSensor sensor)
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
        
        // ORIGINAL: BEFORE MAKING MOVEMENTS RELATIVE TO THE PLAYER
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

        // // ADDED BY AKIRA: The exact position of the enemy
        // sensor.AddObservation(enemy.transform.localPosition);
        // ORIGINAL END
        
    }

    // For manual override of controls. This function will use keyboard presses to simulate output from your NN 
    public override void Heuristic(in ActionBuffers actionsOut)
{
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0; //Simulated NN output 0
        discreteActionsOut[1] = 0; //....................1
        discreteActionsOut[2] = 0; //....................2
        discreteActionsOut[3] = 0; //....................3
        discreteActionsOut[4] = 0;

        if (Input.GetKey(KeyCode.UpArrow))
        {
            discreteActionsOut[0] = 1;
        }       
        if (Input.GetKey(KeyCode.DownArrow))
        {
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            discreteActionsOut[1] = 2; 
        }
        

        //Shoot
        if (Input.GetKey(KeyCode.Space)){
            discreteActionsOut[2] = 1;
        }

        //GoToNearestTarget
        if (Input.GetKey(KeyCode.A)){
            discreteActionsOut[3] = 1;
        }


        if (Input.GetKey(KeyCode.B)){
            discreteActionsOut[4] = 1;
        }
    }

        // What to do when an action is received (i.e. when the Brain gives the agent information about possible actions)
        public override void OnActionReceived(ActionBuffers actions){

        int forwardAxis = (int)actions.DiscreteActions[0]; //NN output 0

        int rotateAxis = (int)actions.DiscreteActions[1]; 
        int shootAxis = (int)actions.DiscreteActions[2]; 
        int goToTargetAxis = (int)actions.DiscreteActions[3];
        
        int goToBaseAxis = (int)actions.DiscreteActions[4];

        MovePlayer(forwardAxis, rotateAxis, shootAxis, goToTargetAxis, goToBaseAxis);
    }
// ----------------------ONTRIGGER AND ONCOLLISION FUNCTIONS------------------------
    // Called when object collides with or trigger (similar to collide but without physics) other objects
    protected override void OnTriggerEnter(Collider collision)
    {
        
        if (collision.gameObject.CompareTag("HomeBase") && collision.gameObject.GetComponent<HomeBase>().team == GetTeam())
        {
            //Add rewards here for when the agent goes back to the homebase
            // if there is any target carried, those would be dropped
            // add rewards for the dropped targets
            // WANTED TO DEAL WITH THIS HERE BUT COLLISION BY HOME BASE IS PROCESSED FIRST, 
            // SO I WILL LEAVE THIS REWARD TO checkStateOfTargetsInbase.
        }
        base.OnTriggerEnter(collision);
    }

    protected override void OnCollisionEnter(Collision collision) 
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
        base.OnCollisionEnter(collision);
    }



    //  --------------------------HELPERS---------------------------- 
     private void AssignBasicRewards() {
        rewardDict = new Dictionary<string, float>();
        
        float rewardHittingWithLaser = 0.005f;
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
        rewardDict.Add("shoot-outside-laser-range-punish-multiplier", -0.00001f);
        // punish for firing when you are not facing the enemy
        // punishment is multiplier * (abs(angle to enemy) - ANGLE_LEEWAY)
        rewardDict.Add("shoot-not-toward-enemy-punish-multiplier", -0.000002f);
        // punish for showing your backside to the enemy while they are facing you
        // punishment is roughly:
        // multiplier * (abs(angle to enemy) - LEEWAY1)  <- how much your showing your back
        //   * (LEEWAY2 - abs(enemy angle to you))       <- how much the enemy faces you
        //   * (DISTANCE_LEEWAY - distance to enemy)     <- how close the enemy is (closer -> more punish)
        rewardDict.Add("punishment-showing-back-to-enemy", -0.00000005f);
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
    
    private void MovePlayer(int forwardAxis, int rotateAxis, int shootAxis, int goToTargetAxis, int goToBaseAxis)
    {
        dirToGo = Vector3.zero;
        rotateDir = Vector3.zero;

        Vector3 forward = transform.forward;
        Vector3 backward = -transform.forward;
        Vector3 right = transform.up;
        Vector3 left = -transform.up;

        //fowardAxis: 
            // 0 -> do nothing
            // 1 -> go forward
            // 2 -> go backward
        if (forwardAxis == 1) dirToGo = forward;
        else if (forwardAxis == 2) dirToGo = backward;

        //rotateAxis: 
            // 0 -> do nothing
            // 1 -> go right
            // 2 -> go left
        
        if (rotateAxis == 1)  rotateDir = right;
        else if (rotateAxis == 2) rotateDir = left;

        //shoot
        if (shootAxis == 1) {
            SetLaser(true);
            // add punishment if we are shooting either when far from the enemy
            // or when we are facing away from the enemy
            float ANGLE_LEEWAY = 45f;
            float distanceToEnemy = Vector3.Distance(transform.localPosition, enemy.transform.localPosition);
            float angleToEnemy = Math.Abs(GetYAngle(enemy));
            if (distanceToEnemy > LASER_LENGTH) {
                AddReward(rewardDict["shoot-outside-laser-range-punish-multiplier"] * (distanceToEnemy - LASER_LENGTH));
            }
            if (angleToEnemy > ANGLE_LEEWAY) {
                AddReward(rewardDict["shoot-not-toward-enemy-punish-multiplier"] * (angleToEnemy - ANGLE_LEEWAY));
            }            
        }
        // HYPOTHETICAL SURGERY TO LET THE AI SHOOT EVERY TIME THE 
        // ENEMY IS RIGHT IN FRONT OF IT
        // else if (GetYAngle(enemy) <= 5 
        // && GetYAngle(enemy) >= -5 
        // && Vector3.Distance(transform.localPosition, enemy.transform.localPosition) <= LASER_LENGTH) SetLaser(true);
        else SetLaser(false);

        //go to the nearest target
        if (goToTargetAxis == 1){
            GoToNearestTarget();
        }
        
        // goToBaseAxis
        if (goToBaseAxis == 1){
            GoToBase();
        }
        
    }

    // Go to home base
    private void GoToBase(){
        TurnAndGo(GetYAngle(myBase));
    }

    // Go to the nearest target
    private void GoToNearestTarget(){
        GameObject target = GetNearestTarget();
        if (target != null){
            float rotation = GetYAngle(target);
            TurnAndGo(rotation);
        }        
    }

    // Rotate and go in specified direction
    private void TurnAndGo(float rotation){

        if(rotation < -5f){
            rotateDir = transform.up;
        }
        else if (rotation > 5f){
            rotateDir = -transform.up;
        }
        else {
            dirToGo = transform.forward;
        }
    }

    // return reference to nearest target
    protected GameObject GetNearestTarget(){
        float distance = 200;
        GameObject nearestTarget = null;
        foreach (var target in targets)
        {
            float currentDistance = Vector3.Distance(target.transform.localPosition, transform.localPosition);
            if (currentDistance < distance && target.GetComponent<Target>().GetCarried() == 0 && target.GetComponent<Target>().GetInBase() != team){
                distance = currentDistance;
                nearestTarget = target;
            }
        }
        return nearestTarget;
    }

    private float GetYAngle(GameObject target) {
        
       Vector3 targetDir = target.transform.position - transform.position;
       Vector3 forward = transform.forward;

      float angle = Vector3.SignedAngle(targetDir, forward, Vector3.up);
      return angle; 
        
    }
}
