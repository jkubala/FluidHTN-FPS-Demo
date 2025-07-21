using UnityEngine;

namespace FPSDemo.NPC.Domains.TaskDefinitions
{
    [CreateAssetMenu(fileName = "New AIShootTask", menuName = "FPSDemo/AI/ShootTask")]
    public class ShootTaskDefinition : AICompoundTaskDefinition
    {
        public override void Add(AIDomainBuilder builder)
        {
            builder.ShootPlayer();
        }
    }
}