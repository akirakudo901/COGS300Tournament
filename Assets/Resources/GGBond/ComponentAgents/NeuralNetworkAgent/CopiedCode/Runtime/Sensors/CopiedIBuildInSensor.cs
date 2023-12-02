using Unity.MLAgents.Sensors;

namespace CopiedCode
{

    /// <summary>
    /// Interface for sensors that are provided as part of ML-Agents.
    /// User-implemented sensors don't need to use this interface.
    /// </summary>
    public interface CopiedIBuiltInSensor
    {
        /// <summary>
        /// Return the corresponding BuiltInSensorType for the sensor.
        /// </summary>
        /// <returns>A BuiltInSensorType corresponding to the sensor.</returns>
        BuiltInSensorType GetBuiltInSensorType();
    }
}
