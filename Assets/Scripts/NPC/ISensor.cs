namespace FPSDemo.Sensors
{
    public interface ISensor
    {
        float TickRate { get; }
        float NextTickTime { get; set; }
        void Tick(AIContext context);
    }
}