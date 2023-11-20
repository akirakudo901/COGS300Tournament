using System.Collections;
using System.Collections.Generic;
using System.Linq; // TODO RECONSIDER; ADDED FOR WHERE FUNCTIONALITY
using UnityEngine;
using UnityEngine.UI;


// THIS IS THE COPY VERSION I TWEAK AROUND!!!

public class FinalStageManager : MonoBehaviour
{
    // Start is called before the first frame update
    public bool isTraining = false;
    public Text winnerTextbox;
    public GameObject timer;

    public GameObject agent1;
    public GameObject agent2;
    public GameObject base1;
    public GameObject base2;
    public Text base1CountTxt;
    public Text base2CountTxt;
    public Camera cam1, cam2;

    GameObject[] targets;
    GameObject[] players;

    CogsAgent agent1Script, agent2Script;
    private bool isReset = false; //Added to control resetting of multiple stages

    void Awake() {
        // HAVE TO SPECIFICALLY CHECK FOR PLAYER OBJECTS WITHIN THIS TRANSFORM
        if (GameObject.FindGameObjectsWithTag("Player").Where(p => p.transform.parent == transform).ToArray().Length == 0) { //Modified
        // ORIGINAL if (GameObject.FindGameObjectsWithTag("Player").Length == 0) {
            agent1 = Resources.Load<GameObject>(WorldConstants.agent1ID + "/" + WorldConstants.agent1ID);
            agent2 = Resources.Load<GameObject>(WorldConstants.agent2ID + "/" + WorldConstants.agent2ID);
            agent1 = Instantiate(agent1);
            agent2 = Instantiate(agent2);

            players = GameObject.FindGameObjectsWithTag("Player").Where(p => p.transform.parent == transform).ToArray(); // Added 
            
            agent1.name = "Agent 1";
            agent2.name = "Agent 2";
        }
        else {
            players = GameObject.FindGameObjectsWithTag("Player").Where(p => p.transform.parent == transform).ToArray();//Modified
            // ORIGINAL players = GameObject.FindGameObjectsWithTag("Player"); 
            agent1 = players[0];
            agent2 = players[1];

            agent1.name = "Agent 1";
            agent2.name = "Agent 2";
        }
    }
    
    void Start()
    {
        // have to specifically link targets objects to this field, and no other
        // filters the targets based on their parent's parent (should be a specific field)
        targets = GameObject.FindGameObjectsWithTag("Target").Where(t => t.transform.parent.parent == transform).ToArray();//Modified
        // ORIGINAL targets = GameObject.FindGameObjectsWithTag("Target");

        agent1.transform.SetParent(transform);
        agent2.transform.SetParent(transform);
        cam1.transform.SetParent(agent1.transform);
        cam2.transform.SetParent(agent2.transform);
        cam1.transform.localPosition = new Vector3(0f, 1.5f, -5f);
        cam2.transform.localPosition = new Vector3(0f, 1.5f, -5f);
        cam1.transform.localRotation = Quaternion.identity;
        cam2.transform.localRotation = Quaternion.identity;
        
        winnerTextbox.enabled = false;
        agent1Script = agent1.GetComponent(WorldConstants.agent1ID) as CogsAgent;
        Debug.Log(agent1Script);
        agent2Script = agent2.GetComponent(WorldConstants.agent2ID) as CogsAgent;
         Debug.Log(agent2Script);
    }


    // Update is called once per frame
    void FixedUpdate()
    { 
        bool timerIsRunning = timer.GetComponent<Timer>().GetTimerIsRunning();

        int base1Num = base1.GetComponent<HomeBase>().GetCaptured();
        int base2Num = base2.GetComponent<HomeBase>().GetCaptured();
        int agent1Carry = agent1Script.GetCarrying();
        int agent2Carry = agent2Script.GetCarrying();

        float agent1BaseDist = agent1Script.DistanceToBase();
        float agent2BaseDist = agent2Script.DistanceToBase();

        base1CountTxt.text = "[A1] " + WorldConstants.agent1ID + ": " + base1Num.ToString();
        base2CountTxt.text = "[A2] " + WorldConstants.agent2ID + ": " + base2Num.ToString();
     
        if (!timerIsRunning)
        {
            if (base1Num > base2Num)
            {
                agent1Script.SetReward(1f);
                agent2Script.SetReward(-1f);
                Debug.Log("Agent 1 wins by capture");
                winnerTextbox.enabled = true;
                winnerTextbox.text = "Agent 1 wins";
            }
            
            else if (base2Num > base1Num)
            {
                agent1Script.SetReward(-1f);
                agent2Script.SetReward(1f);
                Debug.Log("Agent 2 wins by capture");                
                winnerTextbox.enabled = true;
                winnerTextbox.text = "Agent 2 wins";
            }
            else if (agent1Carry > agent2Carry)
            {
                agent1Script.SetReward(1f);
                agent2Script.SetReward(-1f);
                Debug.Log("Agent 1 wins by carry");
                winnerTextbox.enabled = true;
                winnerTextbox.text = "Agent 1 wins";
            }
            
            else if (agent2Carry > agent1Carry)
            {
                agent1Script.SetReward(-1f);
                agent2Script.SetReward(1f);
                Debug.Log("Agent 2 wins by carry");                
                winnerTextbox.enabled = true;
                winnerTextbox.text = "Agent 2 wins";
            }
            else if (agent1BaseDist < agent2BaseDist && agent1Carry != 0)
            {
                agent1Script.SetReward(1f);
                agent2Script.SetReward(-1f);
                Debug.Log("Agent 1 wins by distance");
                winnerTextbox.enabled = true;
                winnerTextbox.text = "Agent 1 wins";
            }
            
            else if (agent2BaseDist < agent1BaseDist && agent2Carry != 0)
            {
                agent1Script.SetReward(-1f);
                agent2Script.SetReward(1f);
                Debug.Log("Agent 2 wins by distance");                
                winnerTextbox.enabled = true;
                winnerTextbox.text = "Agent 2 wins";
            }
            
            else {
                agent1Script.SetReward(0f);
                agent2Script.SetReward(0f);
                Debug.Log("Draw!");

                winnerTextbox.enabled = true;
                winnerTextbox.text = "Draw";
            }

            if (isTraining) {
                Reset();
            }
            else {
                StopGame();
            }
            
        }
    }

    void Reset() {

        // ORIGINAL timer.GetComponent<Timer>().Reset();
        base1.GetComponent<HomeBase>().Reset();
        base2.GetComponent<HomeBase>().Reset();
        foreach (GameObject target in targets)
        {
            target.GetComponent<Target>().ResetGame();
        }
        
        agent1Script.EndEpisode();
        agent2Script.EndEpisode();

        
        // Added START
        setReset(true);
        bool allOtherStagesAreReset = true;
        foreach (GameObject stageManager in GameObject.FindGameObjectsWithTag("TrainingArea"))
        {
            allOtherStagesAreReset = allOtherStagesAreReset && stageManager.GetComponent<FinalStageManager>().hasBeenReset();
        }

        if (allOtherStagesAreReset) {
            foreach (GameObject stageManager in GameObject.FindGameObjectsWithTag("TrainingArea"))
            {
                stageManager.GetComponent<FinalStageManager>().setReset(false);
            }
            timer.GetComponent<Timer>().Reset();
        }
        winnerTextbox.enabled = false;
        // Added END
        
    }

    void StopGame() {
        Time.timeScale = 0;
    }

    //ADDED START
    // Added Getters & Setters
    public bool hasBeenReset() { return isReset; }
    void setReset(bool isReset) { this.isReset = isReset; }
    // ADDED END

}