using System;
using System.Collections;
using System.Collections.Generic;
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
* 3. Don't forget to set the agent mode to InferenceOnly.
* 4. Try and see if it works!
* Definitely let me know if there are bugs / things you wanna double check.
**/

public partial class GGBond : CogsAgent
{
    // whether to use the keyboard control
    public bool useKeyboardControl = true;
    // used to control which mode / behavior type the agent takes
    private string agentMode = "default";
    // sensors allocated to this agent using SetUpGGBondSensors
    private List<List<ISensor>> allSensorsAdditionallyAllocated;
    // list of agent modes that are available to this agent as of right now
    private List<ComponentAgent> allAgentModes;
    
    // ------------------BASIC MONOBEHAVIOR FUNCTIONS-------------------

    // called when enabling the object in the editor;
    // sets up allSensorsAdditionallyAllocated which will hold the sensors
    // that are allocated using SetUpGGBondSensors, which will then be 
    // deallocated correctly with call to OnDisable
    protected override void OnEnable()
    {
        base.OnEnable();
        allSensorsAdditionallyAllocated = new List<List<ISensor>>();
        
        //###################################################
        // SPECIFY WHICH AGENT MODES YOU WANT TO USE HERE!
        // You should instantiate a new instance of your newly defined class 
        // and put into the list; that is, you wanna write:
        // new YOUR_AGENT_CLASS_NAME(this)
        // and add it into the brackets following the "new List<ComponentAgent>()" part.
        allAgentModes = new List<ComponentAgent>() {new TemplateAgent(this)};
        //###################################################

        // make a call to the OnEnable for each ComponentAgent enabled right now
        foreach (ComponentAgent ca in allAgentModes) ca.ComponentAgentOnEnable();
    }

    // called when disabling the object in the editor;
    // cleanups the sensors allocated via SetUpGGBondSensors
    protected override void OnDisable() 
    {
        base.OnDisable();
        // make a call to the OnEnable for each ComponentAgent enabled right now
        foreach (ComponentAgent ca in allAgentModes) ca.ComponentAgentOnDisable();
        // clean up sensors
        CleanupGGBondSensors();
    }
    
    // Initialize values before simulation starts
    protected override void Start()
    {
        base.Start();
        AssignBasicRewards();
        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly) {
            // make a call to the Start method for each ComponentAgent enabled right now
            foreach (ComponentAgent ca in allAgentModes) ca.ComponentAgentStart();
        }
    }

    // For actual actions in the environment (e.g. movement, shoot laser)
    // that is done continuously
    protected override void FixedUpdate() {
        base.FixedUpdate();
        // Determines the agent mode at this time point
        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly) {
            ActionDeterminingLogic();
        }
        
        LaserControl();
        // Movement based on DirToGo and RotateDir
        moveAgent(dirToGo, rotateDir);
    }

    
    // --------------------AGENT FUNCTIONS-------------------------

    // This function handles the main logic behind which behavior type the 
    // agent takes at any single time. It will specify states which 
    // Heuristic use to take the associated actions.
    public void ActionDeterminingLogic() {
        // if we want to use keyboard control, don't change the mode
        if (useKeyboardControl) return;

        string previousAgentMode = agentMode;
        
        // FOR DIFFERENT AGENT IMPLEMENTATIONS
        // e.g. if I wanted to use my "TemplateAgent" agent controls while we have
        // less than 110 seconds remaining:
        
        if ((int) timer.GetComponent<Timer>().GetTimeRemaning() < 110) {
            agentMode = "TEMPLATE_AGENT";
        } else {
            agentMode = "default";
        }

        // log when a switch in agent mode happens
        if (agentMode != previousAgentMode) {
            Debug.Log("Switching from " + previousAgentMode + " to " + agentMode + "!");
        }
    }

    // For manual override of controls. This function will use keyboard presses to simulate output from your NN 
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        if (useKeyboardControl || agentMode == "default") {
            int NUM_AXIS = 5;
            for (int i=0; i < NUM_AXIS; i++) discreteActionsOut[i] = 0;
            // Movement
            if (Input.GetKey(KeyCode.UpArrow)) discreteActionsOut[0] = 1;
            if (Input.GetKey(KeyCode.DownArrow)) discreteActionsOut[0] = 2;
            if (Input.GetKey(KeyCode.RightArrow)) discreteActionsOut[1] = 1;
            if (Input.GetKey(KeyCode.LeftArrow)) discreteActionsOut[1] = 2;
            // Shoot
            if (Input.GetKey(KeyCode.Space)) discreteActionsOut[2] = 1;
            // GoToNearestTarget
            if (Input.GetKey(KeyCode.A)) discreteActionsOut[3] = 1;
            // GoToBase
            if (Input.GetKey(KeyCode.B)) discreteActionsOut[4] = 1;
        } else {
            ComponentAgent currentAgentMode = allAgentModes.Find(x => x.GetName() == agentMode);
            currentAgentMode.ComponentAgentHeuristic(actionsOut);
        }
    }

    // Get relevant information from the environment to effectively learn behavior
    public override void CollectObservations(VectorSensor sensor)
    {
        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.InferenceOnly) {
            GetCollectorObservations(sensor);
        }
        // DEFAULT OBSERVATIONS CODE STORED AT THE END OF SCRIPT!

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
            // STUB
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
           // STUB
        }
        base.OnCollisionEnter(collision);
    }



    //  --------------------------HELPERS---------------------------- 
    // useful for setting up vector observation with specific shapes, to be passed
    // and initialize in pair with a neural network
    // pass to it the corresponding List<ISensor> object, and we return an appropriate
    // value by reference
    private ref List<ISensor> SetUpGGBondSensors(ref List<ISensor> sensors, int vectorObservationSize, int numStackedVectorObservations) {
        // CODE HIGHLY BASED ON InitializeSensors() FOUND HERE, THANKS!: 
        // [https://github.com/Unity-Technologies/ml-agents/blob/209d258dabc57af1212f94cf8d1fac9193675690/com.unity.ml-agents/Runtime/Agent.cs#L977]
        VectorSensor collectObservationsSensor = new VectorSensor(vectorObservationSize);
            if (numStackedVectorObservations > 1)
            {
                StackingSensor stackedCollectObservationsSensor = new StackingSensor(
                    collectObservationsSensor, numStackedVectorObservations);
                sensors.Add(stackedCollectObservationsSensor);
            }
            // else
            // {
            //     sensors.Add(collectObservationsSensor);
            // }
        // Sort the Sensors by name to ensure determinism
        // Implementation of SortSensors as part of SensorUtils
        sensors.Sort((x, y) => string.Compare(x.GetName(), y.GetName(), StringComparison.InvariantCulture));
        // then track that sensors have been allocated by adding them to allSensorsAdditionallyAllocated
        allSensorsAdditionallyAllocated.Add(sensors);

        return ref sensors;
    }

    // useful in cleaning up sensors that were allocated using SetUpGGBondSensors
    void CleanupGGBondSensors()
    {
        // for each sensor lists added to allSensorsAdditionallyAllocated
        foreach (List<ISensor> sensors in allSensorsAdditionallyAllocated) 
        {
            // Dispose all attached sensor
            for (var i = 0; i < sensors.Count; i++)
            {
                var sensor = sensors[i];
                if (sensor is IDisposable disposableSensor)
                {
                    disposableSensor.Dispose();
                }
            }
            // then cleans up the original List<ISensor> object ...? HOW DOES THAT HAPPEN? DOUBLE CHECK
        }
    }

    // useful in obtaining policy objects that we would want to additionally load when calling 
    // return new SentisPolicy(actionSpec, actuatorManager, m_Model, m_InferenceDevice, m_BehaviorName, m_DeterministicInference);

    // assigns basic rewards
     private void AssignBasicRewards() {
        // FOR DIFFERENT AGENT IMPLEMENTATIONS
        // only used when training NNs - if training, choose one implementation from DifferentAgents
        CollectorAssignBasicRewards();
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
        SetLaser(shootAxis == 1);

        //go to the nearest target
        if (goToTargetAxis == 1) GoToNearestTarget();
        
        // goToBaseAxis
        if (goToBaseAxis == 1) GoToBase();
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