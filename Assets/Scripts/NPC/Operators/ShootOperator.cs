using FluidHTN.Operators;
using FluidHTN;
using FPSDemo.NPC.FSMs.WeaponStates;
using UnityEngine.AI;

namespace FPSDemo.NPC.Operators
{
    public class ShootOperator : IOperator
    {
        public TaskStatus Update(IContext ctx)
        {
            if (ctx is AIContext c)
            {
                // TODO: Evaluate what type of shot we want to execute. We could change this based on range and situation.
                // TODO: Long range: single shot.
                // TODO: Medium to close range: burst shot.
                // TODO: Close range + panic: auto shot until empty clip!
                c.SetWeaponState(WeaponStateType.SingleShot, EffectType.Permanent);
                return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }

        public void Stop(IContext ctx)
        {
            if (ctx is AIContext c)
            {
                c.SetWeaponState(WeaponStateType.HoldYourFire, EffectType.Permanent);
            }
        }

        public void Aborted(IContext ctx)
        {
            Stop(ctx);
        }
    }
}