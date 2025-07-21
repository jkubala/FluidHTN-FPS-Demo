using FluidHTN;
using FPSDemo.FSM;

namespace FPSDemo.NPC.FSMs.WeaponStates
{
    public class HoldYourFireState : IState
    {
        public int Id => (int)WeaponStateType.HoldYourFire;

        public void Enter(IFiniteStateMachine mgr, IContext ctx)
        {
            //TODO: Lower weapon
        }

        public void Exit(IFiniteStateMachine mgr, IContext ctx)
        {

        }

        public void Tick(IFiniteStateMachine mgr, IContext ctx)
        {
            if (ctx is AIContext npcCtx)
            {
                if (npcCtx.HasWeaponState(WeaponStateType.HoldYourFire) == false)
                {
                    mgr.ChangeState((int)npcCtx.GetWeaponState(), ctx);
                }
            }
        }
    }
}