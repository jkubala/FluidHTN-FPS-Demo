using UnityEngine;

namespace FPSDemo.NPC
{
    [CreateAssetMenu(fileName = "New NPCSettings", menuName = "FPSDemo/NPCSettings")]
    public class NPCSettings : ScriptableObject
    {
        // ========================================================= PUBLIC FIELDS

        public float AlertAwarenessThreshold = 2f;
        public float AwarenessDeterioration = 0.1f;
    }
}