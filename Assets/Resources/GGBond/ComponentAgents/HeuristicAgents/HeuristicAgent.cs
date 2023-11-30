using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This file implements the "HeuristicAgent" class which forms the basis of any agent that uses heuristics
public partial class GGBond
{
    // nested abstarct class as template for agents who don't rely on neural networks
    abstract class HeuristicAgent : ComponentAgent {
        new public const string name = "HEURISTIC_AGENT";

        // constructor to be specified to instantiate an example of this class
        public HeuristicAgent(GGBond instance) : base(instance) {}

        // nothing on top of ComponentAgent now, but maybe in the future?

        // Returns the name of this agent
        public override string GetName() {return name;}

    } 

}
