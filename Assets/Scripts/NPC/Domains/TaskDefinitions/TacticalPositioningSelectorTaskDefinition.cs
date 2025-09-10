using UnityEngine;

namespace FPSDemo.NPC.Domains.TaskDefinitions
{
    [CreateAssetMenu(fileName = "New TacticalPositioningSelectorTask", menuName = "FPSDemo/AI/TacticalPositioningSelectorTask")]
    public class TacticalPositioningSelectorTaskDefinition : AICompoundTaskDefinition
    {
        public override void Add(AIDomainBuilder builder)
        {
            builder.TacticalPositioningSelector();
        }
    }
}