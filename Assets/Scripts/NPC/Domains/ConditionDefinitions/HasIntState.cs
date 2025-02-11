using System;

namespace FPSDemo.NPC.Domains.ConditionDefinitions
{
    [Serializable]
    public struct HasIntState
    {
        public AIWorldState State;
        public int Value;
    }
}