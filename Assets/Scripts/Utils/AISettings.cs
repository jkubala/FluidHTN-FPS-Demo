using UnityEngine;

namespace FPSDemo.FPSController
{
    [CreateAssetMenu(fileName = "New AISettings", menuName = "FPSDemo/AISettings")]
    public class AISettings : ScriptableObject
    {
        public float VisionSensorTickRate = 1f;
    }
}