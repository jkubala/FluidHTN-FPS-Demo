using UnityEngine;

namespace FPSDemo.NPC.Domains.TaskDefinitions
{
    [CreateAssetMenu(fileName = "New AIMoveToPlayerTask", menuName = "FPSDemo/AI/MoveToPlayerTask")]
    public class MoveToPlayerTaskDefinition : AICompoundTaskDefinition
    {
        public override void Add(AIDomainBuilder builder)
        {
            builder.MoveToPlayer();
        }
    }
}