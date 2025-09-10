using UnityEngine;

namespace FPSDemo.NPC.Domains.TaskDefinitions
{
    [CreateAssetMenu(fileName = "New EmergencyRepositionTask", menuName = "FPSDemo/AI/EmergencyRepositionTask")]
    public class EmergencyRepositionTaskDefinition : AICompoundTaskDefinition
    {
        public override void Add(AIDomainBuilder builder)
        {
            builder.EmergencyReposition();
        }
    }
}