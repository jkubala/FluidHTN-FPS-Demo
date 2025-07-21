using FluidHTN;
using FPSDemo.FSM;
using UnityEngine;

namespace FPSDemo.NPC.FSMs.WeaponStates
{
    public class ReloadState : IState
    {
        public int Id => (int)WeaponStateType.Reload;

        public void Enter(IFiniteStateMachine mgr, IContext ctx)
        {
            if (ctx is AIContext c)
            {
                c.SetState(AIWorldState.IsReloading, true, EffectType.Permanent);
                c.ThisController.Reload();
            }
        }

        public void Exit(IFiniteStateMachine mgr, IContext ctx)
        {

        }

        public void Tick(IFiniteStateMachine mgr, IContext ctx)
        {
            if (ctx is AIContext c)
            {
                // We refuse to change weapon state until the weapon has finished reloading.
                if (c.ThisController.IsReloading == false)
                {
                    c.SetState(AIWorldState.IsReloading, false, EffectType.Permanent);
                    if (c.HasWeaponState(WeaponStateType.Reload))
                    {
                        c.SetWeaponState(WeaponStateType.HoldYourFire, EffectType.Permanent);
                        mgr.ChangeState((int)WeaponStateType.HoldYourFire, ctx);
                    }
                    else
                    {
                        mgr.ChangeState((int)c.GetWeaponState(), ctx);
                    }
                }
            }
        }
    }
}