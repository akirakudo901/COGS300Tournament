using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Actuators;

// This file implements a "harasser" robot working with heuristics. He initially zooms 
// to the enemy base, then once he meets a wall (detected through raycast), starts 
// spinning around by applying rotation to shoot and freeze anybody who's in front of 
// them (again detected by ray cast).

namespace ComponentAgents
{ 

    public class HarasserHeuristic : HeuristicAgent {

        new public const string name = "HARASSER_HEURISTIC";

        // constructor to be specified to instantiate an example of this class
        public HarasserHeuristic(GGBond instance) : base(instance) {}

        // Initialize values specific for the template agent before simulation starts
        public override void ComponentAgentStart()
        {
            // if you wanna do something to be called as part of Start(), put it here.
        }

        // Decides control of AI based on inputs
        private int rotateAxis = 0;
        private int shootAxis = 0;
        public override void ComponentAgentHeuristic(in ActionBuffers actionsOut)
        {
            int forwardAxis = 1;
            // int rotateAxis = rotateAxis;
            // int shootAxis = shootAxis;
            int goToTargetAxis = 0;
            int goToBaseAxis = 0;

            float ahead = Raycast(0);

            shootAxis = Fire();

            if (ahead <= 8)
            {
                forwardAxis = 0; 
                ggbond.StartCoroutine(MyCoroutine());  
            }
            
            var discreteActionsOut = actionsOut.DiscreteActions;

            discreteActionsOut[0] = forwardAxis;
            discreteActionsOut[1] = rotateAxis;
            discreteActionsOut[2] = shootAxis;
            discreteActionsOut[3] = goToTargetAxis;
            discreteActionsOut[4] = goToBaseAxis;

        }

        // Returns the name of this agent
        public override string GetName() {return name;}

        // -----HELPERS-------
        
        // Lab 2 Raycast
        float Raycast(float yAngleOffset)
        {
            var direction = Quaternion.Euler(0, yAngleOffset, 0) * ggbond.transform.forward;
            var position = new Vector3(ggbond.transform.position.x, ggbond.transform.position.y + 0.5f, ggbond.transform.position.z);

            RaycastHit hitInfo;
            // Does the ray intersect any objects excluding the player layer
            if (Physics.Raycast(position, direction, out hitInfo, Mathf.Infinity))
            {
                //Debug.DrawRay(position, direction * hitInfo.distance, Color.yellow);
                return hitInfo.distance;

            }
            else
            {
                //Debug.DrawRay(position, direction * 50, Color.red);
                return 1000f;
            }
        }

        string Raycast2(float yAngleOffset)
        {
            var direction = Quaternion.Euler(0, yAngleOffset, 0) * ggbond.transform.forward;
            var position = new Vector3(ggbond.transform.position.x, ggbond.transform.position.y + 0.5f, ggbond.transform.position.z);

            RaycastHit hitInfo2;
            // Does the ray intersect any objects excluding the player layer
            if (Physics.Raycast(position, direction, out hitInfo2, 40f))

            {
                if (hitInfo2.transform.tag == "Player") {
                Debug.DrawRay(position, direction * hitInfo2.distance, Color.blue);
                return "hit";
                }
                else { 
                    Debug.DrawRay(position, direction * hitInfo2.distance, Color.red);
                    return ""; 
                }
            }
            else
            {
                Debug.DrawRay(position, direction * hitInfo2.distance, Color.red);
                return ""; 
            }

        }
        
        
        IEnumerator MyCoroutine()
        {
            yield return new WaitForSeconds(0.5f); // Wait for 1 seconds
            rotateAxis = 1; 
            yield return new WaitForSeconds(0.5f); // Wait for 1 seconds 
            rotateAxis = 2;
        }

        IEnumerator MyCor2()
        {
            yield return new WaitForSeconds(0.5f); 
            shootAxis = 0; 
        }

        public int Fire() 
        {
            string ahead2 = Raycast2(0); 
            if (ahead2 == "hit") {
                Debug.Log("Hit!");
                return 1; 
            } 
            else {
                // StartCoroutine(MyCor2());
                return 0; 
            }
        }
    }
}
