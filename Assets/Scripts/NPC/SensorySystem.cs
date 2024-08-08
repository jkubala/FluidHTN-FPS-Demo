
using FPSDemo.FPSController;
using UnityEngine;

namespace FPSDemo.Sensors
{
    public class SensorySystem
    {
        private readonly ISensor[] _sensors;

        public SensorySystem(NPC agent)
        {
            _sensors = agent.GetComponents<ISensor>();
        }

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