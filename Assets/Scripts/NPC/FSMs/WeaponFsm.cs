using FPSDemo.FSM;
using FPSDemo.NPC.FSMs.WeaponStates;

namespace FPSDemo.NPC.FSMs
{
    public class WeaponFsm : FiniteStateMachine<WeaponFsm>
    {
        public WeaponFsm()
        {
            AddState<HoldYourFireState>();
            AddState<ReloadState>();
            AddState<SingleShotState>();
            // AddState<BurstShotState>();
            // AddState<AutoShotState>();
            AddState<EmptyClipState>();
        }
    }
}