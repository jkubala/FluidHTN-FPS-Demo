using UnityEngine;

namespace FPSDemo.NPC.Domains.TaskDefinitions
{
    [CreateAssetMenu(fileName = "New SeekFlankingPositionTask", menuName = "FPSDemo/AI/SeekFlankingPositionTask")]
    public class SeekFlankingPositionTaskDefinition : AICompoundTaskDefinition
    {
        public override void Add(AIDomainBuilder builder)
        {
            builder.SeekFlankingPosition();
        }
    }
}