using UnityEngine;

namespace FPSDemo.NPC.Domains.TaskDefinitions
{
    [CreateAssetMenu(fileName = "New AIIdleTask", menuName = "FPSDemo/AI/IdleTask")]
    public class IdleTaskDefinition : AICompoundTaskDefinition
    {
        public override void Add(AIDomainBuilder builder)
        {
            builder.Sequence("Idle");
            {
                // TODO: What does the AI do when idle?
            }
            builder.End();
        }
    }
}