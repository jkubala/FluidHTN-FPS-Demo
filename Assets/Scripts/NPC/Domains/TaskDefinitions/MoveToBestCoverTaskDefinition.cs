using UnityEngine;

namespace FPSDemo.NPC.Domains.TaskDefinitions
{
    [CreateAssetMenu(fileName = "New MoveToBestCoverTask", menuName = "FPSDemo/AI/MoveToBestCoverTask")]
    public class MoveToBestCoverTaskDefinition : AICompoundTaskDefinition
    {
        public override void Add(AIDomainBuilder builder)
        {
            builder.MoveToBestCover();
        }
    }
}