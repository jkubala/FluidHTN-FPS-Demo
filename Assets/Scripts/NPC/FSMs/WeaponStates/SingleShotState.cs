using FluidHTN;
using FPSDemo.FSM;
using UnityEngine;

namespace FPSDemo.NPC.FSMs.WeaponStates
{
    public class SingleShotState : IState
    {
        public int Id => (int)WeaponStateType.SingleShot;

        private float _lastShotTime;

        public void Enter(IFiniteStateMachine mgr, IContext ctx)
        {
            _lastShotTime = 0;
        }

        public void Exit(IFiniteStateMachine mgr, IContext ctx)
        {

        }

        public void Tick(IFiniteStateMachine mgr, IContext ctx)
        {
            var timeSinceLastShot = Time.time - _lastShotTime;
            if (ctx is AIContext c)
            {
                // If we're no longer in single shot state, transition!
                if (c.HasWeaponState(WeaponStateType.SingleShot) == false)
                {
                    StopShooting(c);
                    mgr.ChangeState((int)c.GetWeaponState(), ctx);
                    return;
                }

                // We shouldn't shoot if we don't have the enemy in sight
                // This will block things like suppression fire. So evaluate options!
                if (c.HasState(AIWorldState.HasEnemyInSight) == false)
                {
                    StopShooting(c);
                    mgr.ChangeState((int)WeaponStateType.HoldYourFire, ctx);
                    return;
                }

                // If we are already shooting, check whether we should stop shooting.
                if (c.ThisController.IsShooting)
                {
                    if (timeSinceLastShot > 0.15f) // TODO: Ideally we should be able to call a single shot on the controller rather than add a timer.
                    {
                        StopShooting(c);
                    }
                }
                // If it's been long enough since our last shot, take another shot!
                else if (timeSinceLastShot > 1.0f + Random.value * 0.25f) // TODO: Move minimum single shot spacing into npc/weapon config.
                {
                    StartShooting(c);
                }

                // TODO: Empty clip check/transition, but we need to be able to access the NPC weapon and gauge its ammo first.
            }
        }

        private void StartShooting(AIContext c)
        {
            if (c.ThisController.IsShooting == false)
            {
                c.SetState(AIWorldState.IsShooting, true, EffectType.Permanent);
                c.ThisController.StartShooting();
                _lastShotTime = Time.time;
            }
        }

        private void StopShooting(AIContext c)
        {
            if (c.ThisController.IsShooting)
            {
                c.SetState(AIWorldState.IsShooting, false, EffectType.Permanent);
                c.ThisController.StopShooting();
            }
        }
    }
}