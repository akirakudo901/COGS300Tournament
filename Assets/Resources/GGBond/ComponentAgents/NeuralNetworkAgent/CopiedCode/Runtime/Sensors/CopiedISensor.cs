using System;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;

namespace CopiedCode
{

    /// <summary>
    /// Sensor interface for generating observations.
    /// </summary>
    public interface CopiedISensor : ISensor
    {

        // THIS IS THE SOLE FUNCTION I HAVE TO REWRITE IN ORDER FOR ObservationWriter TO 
        // WORK AS INTENDED...

        /// <summary>
        /// Write the observation data directly to the <see cref="ObservationWriter"/>.
        /// Note that this (and  <see cref="GetCompressedObservation"/>) may
        /// be called multiple times per agent step, so should not mutate any internal state.
        /// </summary>
        /// <param name="writer">Where the observations will be written to.</param>
        /// <returns>The number of elements written.</returns>
        int Write(CopiedObservationWriter writer);

    }
}