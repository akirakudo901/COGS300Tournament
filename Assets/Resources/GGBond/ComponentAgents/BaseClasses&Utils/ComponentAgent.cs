using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Actuators;

// This file implements an abstract "ComponentAgent" class which forms the basis of any 'component' agent
// to the overarching player agent
public partial class GGBond
{
    // nested abstarct class as template for agents
    abstract class ComponentAgent {

        public const string name = "COMPONENT_AGENT"; 
        protected GGBond ggbond;

        // constructor taking the parent GGBond instance explicitly passed in c#
        public ComponentAgent(GGBond ggbond) 
        {
            this.ggbond = ggbond;
        }
        
        // Initialize values specific for the template agent before simulation starts
        public virtual void ComponentAgentStart() 
        {
            // do something if needed
        }

        // Called when OnEnable of GGBond is called
        public virtual void ComponentAgentOnEnable() 
        {
            // do something if needed
        }

        // Called when OnDisable of GGBond is called
        public virtual void ComponentAgentOnDisable() 
        {
            // do something if needed
        }

        // Decides control of AI based on inputs
        public abstract void ComponentAgentHeuristic(in ActionBuffers actionsOut);

        // Returns the name of this agent
        public virtual string GetName() {return name;}

    } 

}