
using UnityEngine;

namespace FPSDemo.NPC.Sensors
{
    public class SensorySystem
    {
        // ========================================================= PRIVATE FIELDS

        private readonly ISensor[] _sensors;


        // ========================================================= CONSTRUCTION

        public SensorySystem(NPC agent)
        {
            _sensors = agent.GetComponents<ISensor>();
        }


        // ========================================================= TICK

        public void Tick(AIContext context)
        {
            foreach (var sensor in _sensors)
            {
                var t = Time.time;
                if (t >= sensor.NextTickTime)
                {
                    sensor.NextTickTime = t + sensor.TickRate;
                    sensor.Tick(context);
                }
            }
        }
    }
}