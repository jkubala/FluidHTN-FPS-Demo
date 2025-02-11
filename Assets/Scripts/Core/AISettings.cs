using UnityEngine;

namespace FPSDemo.Core
{
    [CreateAssetMenu(fileName = "New AISettings", menuName = "FPSDemo/AISettings")]
    public class AISettings : ScriptableObject
    {
        // ========================================================= PUBLIC FIELDS

        public float VisionSensorTickRate = 1f;
        public float EnemySensorTickRate = 1f;
    }
}