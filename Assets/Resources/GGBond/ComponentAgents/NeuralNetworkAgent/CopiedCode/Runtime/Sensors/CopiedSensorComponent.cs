using UnityEngine;

namespace CopiedCode
{
    /// <summary>
    /// Editor components for creating Sensors. Generally an ISensor implementation should have a
    /// corresponding CopiedSensorComponent to create it.
    /// </summary>
    public abstract class CopiedSensorComponent : MonoBehaviour
    {
        /// <summary>
        /// Create the ISensors. This is called by the Agent when it is initialized.
        /// </summary>
        /// <returns>Created ISensor objects.</returns>
        public abstract CopiedISensor[] CreateSensors();
    }
}
