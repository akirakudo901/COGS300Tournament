﻿using System;
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
    // whether to use the keyboard control
    public bool useKeyboardControl = true;
    // used to control which mode / behavior type the agent takes
    private string agentMode = "default";
    // sensors allocated to this agent using SetUpGGBondSensors
    private List<CopiedISensor> allSensorsAdditionallyAllocated;
    // list of agent modes that are available to this agent as of right now
    private List<ComponentAgent> allAgentModes;
    
    // length of the laser, as defined with CogsAgent (private so not accessible)
    private const float LASER_LENGTH = 20f;
    
    // ------------------BASIC MONOBEHAVIOR FUNCTIONS-------------------

    // called when enabling the object in the editor;
    // sets up allSensorsAdditionallyAllocated which will hold the sensors
    // that are allocated using SetUpGGBondSensors, which will then be 
    // deallocated correctly with call to OnDisable
    protected override void OnEnable()
    {
        base.OnEnable();

        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly) {
            allSensorsAdditionallyAllocated = new List<CopiedISensor>();
            // initialize the agent modes
            initializeAgentModes();
            // if in heuristic mode, call OnEnable for each ComponentAgent enabled right now
            foreach (ComponentAgent ca in allAgentModes) ca.ComponentAgentOnEnable();
        }
    }

    // called when disabling the object in the editor;
    // cleanups the sensors allocated via SetUpGGBondSensors
    protected override void OnDisable() 
    {
        base.OnDisable();
        
        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly) {
            // if in heuristic mode, call OnDisable for each ComponentAgent enabled right now
            foreach (ComponentAgent ca in allAgentModes) ca.ComponentAgentOnDisable();
            // clean up sensors
            CleanupSensors();
        }
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

    // request an action (call the RequestAction function) to the current agent mode
    // if it is a NeuralNetworkAgent
    public void RequestActionToNNAgent() {
        // only valid when BehaviorType is HeuristicOnly
        if (GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.HeuristicOnly) return;

        ComponentAgent currentAgentMode = allAgentModes.Find(x => x.GetName() == agentMode);
        if (currentAgentMode is NeuralNetworkAgent) {
            NeuralNetworkAgent nnAgent = (NeuralNetworkAgent) currentAgentMode;
            nnAgent.RequestNNAction();
        }
    }

    // request a decision (call the RequestDecision function) to the current agent mode
    // if it is a NeuralNetworkAgent
    public void RequestDecisionToNNAgent() {
        // only valid when BehaviorType is HeuristicOnly
        if (GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.HeuristicOnly) return;

        ComponentAgent currentAgentMode = allAgentModes.Find(x => x.GetName() == agentMode);
        if (currentAgentMode is NeuralNetworkAgent) {
            NeuralNetworkAgent nnAgent = (NeuralNetworkAgent) currentAgentMode;
            nnAgent.RequestNNDecision();
        }
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

    // useful in cleaning up sensors that were allocated using SetUpGGBondSensors
    void CleanupSensors()
    {
        // for each sensor added to allSensorsAdditionallyAllocated
        foreach (CopiedISensor sensor in allSensorsAdditionallyAllocated) 
        {
            // Dispose sensor
            if (sensor is IDisposable disposableSensor)
            {
                disposableSensor.Dispose();
            }
        }
    }

    // given an NNModel, reinitialize the neural network agent that had this as their neural network
    public void reinitializeNNAgent(NNModel oldModel, NNModel newModel) 
    {
        ComponentAgent agentToReinitialize = allAgentModes.Find((a) => {
            if (a is NeuralNetworkAgent) {
                NeuralNetworkAgent nnAgent = (NeuralNetworkAgent) a;
                return object.ReferenceEquals(oldModel, nnAgent.Model);
            } else { return false; }
        });
        Debug.Log("Did we find the agent to reinitialize? " + (agentToReinitialize != null).ToString()); // TODO REMOVE
        // if not found, do nothing
        if (agentToReinitialize == null) return;
        // otherwise, cast to NeuralNetworkAgent
        NeuralNetworkAgent nnAgent = (NeuralNetworkAgent) agentToReinitialize;
        // this setter will then automatically call and reinitialize the policy
        if (newModel != null) nnAgent.Model = newModel;
        Debug.Log("Model of agent " + nnAgent.GetName() + " reinitialized!"); // TODO REMOVE
    }

    // assigns basic rewards
     private void AssignBasicRewards() {
        // FOR DIFFERENT AGENT IMPLEMENTATIONS
        // only used when training NNs - if training, choose one implementation from DifferentAgents
        rewardDict = new Dictionary<string, float>();
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