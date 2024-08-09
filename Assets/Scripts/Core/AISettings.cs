using UnityEngine;

namespace FPSDemo.Core
{
    [CreateAssetMenu(fileName = "New AISettings", menuName = "FPSDemo/AISettings")]
    public class AISettings : ScriptableObject
    {
        public float VisionSensorTickRate = 1f;
    }
}