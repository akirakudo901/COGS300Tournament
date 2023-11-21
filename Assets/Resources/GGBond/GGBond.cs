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

    // ------------------BASIC MONOBEHAVIOR FUNCTIONS-------------------
    
    // Initialize values
    protected override void Start()
    {
        base.Start();
        AssignBasicRewards();
        latestNumTargetInHomeBase = 0;
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
    }


    
    // --------------------AGENT FUNCTIONS-------------------------

    // Get relevant information from the environment to effectively learn behavior
    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent velocity in x and z axis 
        var localVelocity = transform.InverseTransformDirection(rBody.velocity);
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);

        // Time remaning
        sensor.AddObservation(timer.GetComponent<Timer>().GetTimeRemaning());  

        // Agent's current rotation
        var localRotation = transform.rotation;
        sensor.AddObservation(transform.rotation.y);

        // Agent and home base's position
        sensor.AddObservation(this.transform.localPosition);
        sensor.AddObservation(baseLocation.localPosition);

        // for each target in the environment, add: its position, whether it is being carried,
        // and whether it is in a base
        foreach (GameObject target in targets){
            sensor.AddObservation(target.transform.localPosition);
            sensor.AddObservation(target.GetComponent<Target>().GetCarried());
            sensor.AddObservation(target.GetComponent<Target>().GetInBase());
        }
        
        // Whether the agent is frozen
        sensor.AddObservation(IsFrozen());
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

        rewardDict.Add("frozen", -0.1f); //punishment per tick being frozen
        rewardDict.Add("shooting-laser", -0.01f); //punishment for the act of shooting lasers (since it might lose you time)
        rewardDict.Add("hit-enemy", 0.6f); //reward for hitting an enemy with the laser
        rewardDict.Add("dropped-one-target", -0.1f); // punishment per target dropped when shot by laser
        rewardDict.Add("dropped-targets", -0.1f); // punishment when hit by laser (same to freeze?)
        // added by AKIRA:
        rewardDict.Add("carry-one-target-back-to-base", 0.5f); // reward per target brought back to base
        rewardDict.Add("pick-up-target", 0.25f); // reward when picking up a target
        rewardDict.Add("bump-into-wall", -0.1f); // punishment for bumping into walls
        rewardDict.Add("enemy-stole-one-target", -0.5f); // punishment when the enemy steals a target from your base
        rewardDict.Add("bonus-stealing-from-enemy", 0.25f); // bonus for picking up a target in the enemy base
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
        if (shootAxis == 1) SetLaser(true);
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
