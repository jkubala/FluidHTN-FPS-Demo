using System;

namespace FPSDemo.NPC.Domains.ConditionDefinitions
{
    [Serializable]
    public struct HasBoolState
    {
        public AIWorldState State;
        public bool Value;
    }
}