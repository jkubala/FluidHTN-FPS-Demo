using FPSDemo.NPC.Domains;
using UnityEngine;

namespace FPSDemo.NPC
{
    [CreateAssetMenu(fileName = "New NPCSettings", menuName = "FPSDemo/NPCSettings")]
    public class NPCSettings : ScriptableObject
    {
        // ========================================================= PUBLIC FIELDS

        public AIDomainDefinition AIDomain;
        public float AlertAwarenessThreshold = 2f;
        public float AwarenessDeterioration = 0.1f;
    }
}