using System.Collections.Generic;
using FPSDemo.Target;

namespace FPSDemo.NPC
{
    public class TargetData
    {
        // ========================================================= PUBLIC FIELDS

        public float awarenessOfThisTarget = 0;
        public List<VisibleBodyPart> visibleBodyParts = new();
    }
}
