using UnityEngine;

namespace FPSDemo.NPC.Domains
{
    public abstract class AICompoundTaskDefinition : ScriptableObject
    {
        public abstract void Add(AIDomainBuilder builder);
    }
}