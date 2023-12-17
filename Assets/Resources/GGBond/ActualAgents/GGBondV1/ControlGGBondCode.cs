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
using ComponentAgents;

// This file holds the almost original agent code, excluding everything related
// to handling the action determining logic. This should generally not change anymore!

public partial class GGBond : CogsAgent
{
    [SerializeField]
    // whether to use the keyboard control
    public bool UseKeyboardControl = false;
    // whether to shoot when seeing the enemy
    public bool ShootWheneverSeeingTheEnemy = true;
    // whether to not shoot outside of when seeing the enemy
    public bool NotShootOutsideOfSeeingTheEnemy = true;
    // used to control which mode / behavior type the agent takes
    protected string agentMode = "default";
    // list of agent modes that are available to this agent as of right now
    protected List<ComponentAgent> allAgentModes;
    
    // length of the laser, as defined with CogsAgent (private so not accessible)
    public const float LASER_LENGTH = 20f;
    // a manula laser cooldown so that our agent doesn't get stuck 
    // while shooting
    protected const float LASER_COOLDOWN = 0.5f;
    // last shoot time for laser to set up cooldownss
    protected float LaserShotTime;
    
    // ------------------BASIC MONOBEHAVIOR FUNCTIONS-------------------

    // called when enabling the object in the editor
    protected override void OnEnable()
    {
        base.OnEnable();

        if (GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.InferenceOnly) {
            // set keyboard control to false
            UseKeyboardControl = false;
            // initialize the agent modes
            InitializeAgentModes();
            // if in heuristic mode, call OnEnable for each ComponentAgent enabled right now
            foreach (ComponentAgent ca in allAgentModes) ca.ComponentAgentOnEnable();
        }
    }

    // called when disabling the object in the editor;
    // cleanups the sensors allocated via SetUpGGBondSensors
    protected override void OnDisable() 
    {
        base.OnDisable();
        
        if (GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.InferenceOnly) {
            // if in heuristic mode, call OnDisable for each ComponentAgent enabled right now
            foreach (ComponentAgent ca in allAgentModes) ca.ComponentAgentOnDisable();
        }
    }
    
    // Initialize values before simulation starts
    protected override void Start()
    {
        base.Start();
        AssignBasicRewards();
        if (GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.InferenceOnly) {
            // make a call to the Start method for each ComponentAgent enabled right now
            foreach (ComponentAgent ca in allAgentModes) ca.ComponentAgentStart();
        } else if (GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.InferenceOnly) {
            Debug.Log("If you are trying different neural networks in Inference Mode,"
             + " don't forget to add the right observations inside OtherGGBondCode.cs!");
        }
    }

    // For actual actions in the environment (e.g. movement, shoot laser)
    // that is done continuously
    protected override void FixedUpdate() {
        base.FixedUpdate();
        // Determines the agent mode at this time point
        if (GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.InferenceOnly) {
            // change mode only if we don't use keyboard control
            if (!UseKeyboardControl) {

                string previousAgentMode = agentMode;
                ActionDeterminingLogic();
                // log when a switch in agent mode happens
                if (agentMode != previousAgentMode) 
                {
                    Debug.Log("Switching agent " + GetTeam() + " from " + previousAgentMode + " to " + agentMode + "!");
                }

            }
        }
        
        LaserControl();
        // Movement based on DirToGo and RotateDir
        moveAgent(dirToGo, rotateDir);
        // if we are in training, call this update
        // RewardRelatedFixedUpdate();
    }

    
    // --------------------AGENT FUNCTIONS-------------------------

    // For manual override of controls. This function will use keyboard presses to simulate output from your NN 
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        if (UseKeyboardControl || agentMode == "default") {
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

    // What to do when an action is received (i.e. when the Brain gives the agent information about possible actions)
    public override void OnActionReceived(ActionBuffers actions){
        
        int forwardAxis = (int)actions.DiscreteActions[0]; //NN output 0
        int rotateAxis = (int)actions.DiscreteActions[1];
        int shootAxis = (int)actions.DiscreteActions[2];
        int goToTargetAxis = (int)actions.DiscreteActions[3];
        int goToBaseAxis = (int)actions.DiscreteActions[4];

        // if the enemy is right in front and shootWheneverSeeingTheEnemy is true, we shoot
        // we also don't shoot when the enemy is already frozen
        // we also apply a cooldown to shooting so that we're not locked in just shooting
        if (ShootWheneverSeeingTheEnemy && Math.Abs(GetYAngle(enemy)) < 5
        && Vector3.Distance(transform.localPosition, enemy.transform.localPosition) < LASER_LENGTH
        && !enemy.GetComponent<CogsAgent>().IsFrozen()
        && Time.time >= LaserShotTime + LASER_COOLDOWN)
        {
            shootAxis = 1;
            LaserShotTime = Time.time;
        // otherwise, we might not shoot outside of when we see the enemy
        } else if (NotShootOutsideOfSeeingTheEnemy) {
            shootAxis = 0;
        }

        MovePlayer(forwardAxis, rotateAxis, shootAxis, goToTargetAxis, goToBaseAxis);
    }

    // request an action (call the RequestAction function) to the current agent mode
    // if it is a NeuralNetworkAgent
    public void RequestActionToNNAgent() {
        // only valid when BehaviorType is not InferenceOnly
        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.InferenceOnly) return;

        ComponentAgent currentAgentMode = allAgentModes.Find(x => x.GetName() == agentMode);
        if (currentAgentMode is NeuralNetworkAgent) {
            NeuralNetworkAgent nnAgent = (NeuralNetworkAgent) currentAgentMode;
            nnAgent.RequestNNAction();
        }
    }

    // request a decision (call the RequestDecision function) to the current agent mode
    // if it is a NeuralNetworkAgent
    public void RequestDecisionToNNAgent() {
        // only valid when BehaviorType is not InferenceOnly
        if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.InferenceOnly) return;

        ComponentAgent currentAgentMode = allAgentModes.Find(x => x.GetName() == agentMode);
        if (currentAgentMode is NeuralNetworkAgent) {
            NeuralNetworkAgent nnAgent = (NeuralNetworkAgent) currentAgentMode;
            nnAgent.RequestNNDecision();
        }
    }



    //  --------------------------HELPERS---------------------------- 

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

    private const int ROTATE_DIR_RIGHT = 1;
    private const int ROTATE_DIR_LEFT = 2;
    private const int DIR_TO_GO_FORWARD = 1;
    private const int DIR_TO_GO_BACKWARD = 2;

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
        if (forwardAxis == GGBond.DIR_TO_GO_FORWARD) dirToGo = forward;
        else if (forwardAxis == GGBond.DIR_TO_GO_BACKWARD) dirToGo = backward;

        //rotateAxis: 
            // 0 -> do nothing
            // 1 -> go right
            // 2 -> go left
        
        if (rotateAxis == GGBond.ROTATE_DIR_RIGHT)  rotateDir = right;
        else if (rotateAxis == GGBond.ROTATE_DIR_LEFT) rotateDir = left;

        //shoot
        SetLaser(shootAxis == 1);

        //go to the nearest target
        if (goToTargetAxis == 1) GoToNearestTarget();
        
        // goToBaseAxis
        if (goToBaseAxis == 1) GoToBase();
    }

    // Go to home base
    public void GoToBase(){
        TurnAndGo(GetYAngle(myBase), false);
    }

    // Go to the nearest target
    public void GoToNearestTarget(){
        GameObject target = GetNearestTarget();
        if (target != null){
            float rotation = GetYAngle(target);
            TurnAndGo(rotation, false);
        }        
    }

    // Rotate and go in specified direction
    public void TurnAndGo(float rotation, bool faceDirection = false){
        // if not faceDirection:
        // -175~-90:-up or 5~90:-up 
        // -90~-5:up or 90~175: up
        // -180~-175 or 175~180: backward
        // -5~5: forward
        // if faceDirection: 
        // -180~-5: -up
        // 5~180: up
        // -5~5: forward

        // usual turn and go
        if (faceDirection) {
            bool turned = TurnTowards(rotation, faceDirection);
            if (!turned && -5f < rotation && rotation < 5f)
            {
                dirToGo = transform.forward;
            }

        // enhanced turn and go
        } else {
            bool turned = TurnTowards(rotation, faceDirection);
            
            if (!turned && 
                ((-180f < rotation && rotation < -175f) || 
                 ( 175f < rotation && rotation < 180f )))
            {
                dirToGo = -transform.forward;
            }
            else if (!turned)
            {
                dirToGo = transform.forward;
            }
        }
    }

    // Rotate and go in specified direction
    public void TurnAndGo(GameObject target, bool faceDirection = false){
        TurnAndGo(GetYAngle(target), faceDirection);
    }

    // Rotate and go in specified direction
    public void TurnAndGo(Vector3 position, bool faceDirection = false){
        TurnAndGo(GetYAngle(position), faceDirection);
    }

    // turn towards the specific given angle
    // also returns whether we turned in this decision step
    public bool TurnTowards(float rotation, bool faceDirection = false) {
        // usual turn and go
        if (faceDirection) {
            if (-180f < rotation && rotation < -5f) 
            {
                rotateDir = transform.up;
                return true;
            } 
            else if (5f < rotation && rotation < 180f) 
            {
                rotateDir = -transform.up;
                return true;
            } else 
            {
                return false;
            }

        // enhanced turn and go
        } else {
            if ((-175f < rotation && rotation < -90f) || 
                (   5f < rotation && rotation <  90f)) 
            {
                rotateDir = -transform.up;
                return true;
            } 
            else if ((-90f < rotation && rotation <  -5f) || 
                    ( 90f < rotation && rotation < 175f && !faceDirection)) 
            {
                rotateDir = transform.up;
                return true;
            }
            else 
            {
                return false;
            }
        }
    }

    // returns which way you should turn to get to the intended position, in the axes
    // as specified in MovePlayer
    // if faceDirection is false, it will show you the fastest way either facing
    // forward or backward
    public int WhichWayToTurn(float rotation, bool faceDirection = false) {
        if (faceDirection) {
            if (-180f < rotation && rotation < -5f) return GGBond.ROTATE_DIR_RIGHT;
            else if (5f < rotation && rotation < 180f) return GGBond.ROTATE_DIR_LEFT;
            else return 0;
        // enhanced turn and go
        } else {
            if ((-175f < rotation && rotation < -90f) || 
                (   5f < rotation && rotation <  90f)) 
            {
                return ROTATE_DIR_LEFT;
            } 
            else if ((-90f < rotation && rotation <  -5f) || 
                    ( 90f < rotation && rotation < 175f && !faceDirection)) 
            {
                return ROTATE_DIR_RIGHT;
            }
            else 
            {
                return 0;
            }
        }
    }

    public int WhichWayToTurn(Vector3 position, bool faceDirection = false) {
        return WhichWayToTurn(GetYAngle(position), faceDirection);
    }

    public int WhichWayToTurn(GameObject target, bool faceDirection = false) {
        return WhichWayToTurn(GetYAngle(target), faceDirection);
    }

    // returns a value for the forward and rotate axes specifying how to move in 
    // order to realize the given rotation
    public (int forwardAxis, int rotateAxis) HowToTurnOrGo(float rotation, bool faceDirection = false, bool doBoth = false) {
        int forwardAxis = 0;
        int rotateAxis = 0; 

        if (faceDirection) {
            rotateAxis = WhichWayToTurn(rotation, faceDirection);
            if ((doBoth || rotateAxis == 0) && -5f < rotation && rotation < 5f)
            {
                forwardAxis = GGBond.DIR_TO_GO_FORWARD;
            }
            return (forwardAxis, rotateAxis);

        // enhanced turn and go
        } else {
            rotateAxis = WhichWayToTurn(rotation, faceDirection);
            
            if ((doBoth || rotateAxis == 0) && 
                ((-180f < rotation && rotation < -175f) || 
                 ( 175f < rotation && rotation < 180f )))
            {
                forwardAxis = GGBond.DIR_TO_GO_BACKWARD;
            }
            else if ((doBoth || rotateAxis == 0))
            {
                forwardAxis = GGBond.DIR_TO_GO_FORWARD;
            }
            return (forwardAxis, rotateAxis);
        }
    }

    public (int forwardAxis, int rotateAxis) HowToTurnOrGo(GameObject target, bool faceDirection = false, bool doBoth = false) {
        return HowToTurnOrGo(GetYAngle(target), faceDirection, doBoth);
    }

    public (int forwardAxis, int rotateAxis) HowToTurnOrGo(Vector3 position, bool faceDirection = false, bool doBoth = false) {
        return HowToTurnOrGo(GetYAngle(position), faceDirection, doBoth);
    }

    // return reference to nearest target
    public GameObject GetNearestTarget(){
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

    public float GetYAngle(GameObject target) {
        
    //    Vector3 targetDir = target.transform.position - transform.position;
    //    Vector3 forward = transform.forward;

    //   float angle = Vector3.SignedAngle(targetDir, forward, Vector3.up);
    //   return angle;
        return GetYAngle(target.transform.position);
    }

    public float GetYAngle(Vector3 position) {
        
       Vector3 targetDir = position - transform.position;
       Vector3 forward = transform.forward;

      float angle = Vector3.SignedAngle(targetDir, forward, Vector3.up);
      return angle; 
        
    }

    // getters and setters
    public GameObject GetEnemy() {return enemy;}
    public GameObject GetMyBase() {return myBase;}
    // returns targets in the training realm (not the ones carried by this agent!)
    public GameObject[] GetTargetsInEnvironment() {return targets;}
    // gets the timer associated (somehow) with this object
    public GameObject GetTimer() {return timer;}
}