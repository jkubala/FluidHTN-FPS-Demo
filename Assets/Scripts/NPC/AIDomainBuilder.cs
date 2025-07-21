using FluidHTN;
using FluidHTN.Factory;
using FluidHTN.PrimitiveTasks;
using FPSDemo.NPC.Conditions;
using FPSDemo.NPC.Effects;
using FPSDemo.NPC.FSMs.WeaponStates;
using FPSDemo.NPC.Operators;

namespace FPSDemo.NPC
{
    public class AIDomainBuilder : BaseDomainBuilder<AIDomainBuilder, AIContext>
    {
        public AIDomainBuilder(string domainName) : base(domainName, new DefaultFactory())
        {
        }

        public AIDomainBuilder HasState(AIWorldState state)
        {
            var condition = new HasWorldStateCondition(state);
            Pointer.AddCondition(condition);
            return this;
        }

        public AIDomainBuilder HasState(AIWorldState state, byte value)
        {
            var condition = new HasWorldStateCondition(state, value);
            Pointer.AddCondition(condition);
            return this;
        }

        public AIDomainBuilder HasWeaponState(WeaponStateType weaponState)
        {
            var condition = new HasWorldStateCondition(AIWorldState.WeaponState, (byte)weaponState);
            Pointer.AddCondition(condition);
            return this;
        }
        public AIDomainBuilder HasStateGreaterThan(AIWorldState state, byte value)
        {
            var condition = new HasWorldStateGreaterThanCondition(state, value);
            Pointer.AddCondition(condition);
            return this;
        }

        public AIDomainBuilder SetState(AIWorldState state, EffectType type)
        {
            if (Pointer is IPrimitiveTask task)
            {
                var effect = new SetWorldStateEffect(state, type);
                task.AddEffect(effect);
            }
            return this;
        }

        public AIDomainBuilder SetState(AIWorldState state, bool value, EffectType type)
        {
            if (Pointer is IPrimitiveTask task)
            {
                var effect = new SetWorldStateEffect(state, value, type);
                task.AddEffect(effect);
            }
            return this;
        }

        public AIDomainBuilder SetState(AIWorldState state, byte value, EffectType type)
        {
            if (Pointer is IPrimitiveTask task)
            {
                var effect = new SetWorldStateEffect(state, value, type);
                task.AddEffect(effect);
            }
            return this;
        }

        public AIDomainBuilder SetWeaponState(WeaponStateType weaponState, EffectType type)
        {
            if (Pointer is IPrimitiveTask task)
            {
                var effect = new SetWorldStateEffect(AIWorldState.WeaponState, (byte)weaponState, type);
                task.AddEffect(effect);
            }
            return this;
        }

        public AIDomainBuilder IncrementState(AIWorldState state, EffectType type)
        {
            if (Pointer is IPrimitiveTask task)
            {
                var effect = new IncrementWorldStateEffect(state, type);
                task.AddEffect(effect);
            }
            return this;
        }

        public AIDomainBuilder IncrementState(AIWorldState state, byte value, EffectType type)
        {
            if (Pointer is IPrimitiveTask task)
            {
                var effect = new IncrementWorldStateEffect(state, value, type);
                task.AddEffect(effect);
            }
            return this;
        }

        public AIDomainBuilder MoveToPlayer()
        {
            Action("Move to enemy");
            {
                HasState(AIWorldState.AwareOfEnemy);

                if (Pointer is IPrimitiveTask task)
                {
                    task.SetOperator(new MoveToPlayerOperator());
                }

                SetState(AIWorldState.IsPursuingEnemy, EffectType.PlanAndExecute);
            }
            End();
            return this;
        }

        public AIDomainBuilder ShootPlayer()
        {
            Action("Shoot player!");
            {
                HasState(AIWorldState.HasEnemyInSight);

                if (Pointer is PrimitiveTask task)
                {
                    task.SetOperator(new ShootOperator());
                }

                SetState(AIWorldState.IsShooting, EffectType.PlanOnly);
            }
            End();
            return this;
        }

        // TODO: Move to cover if we are reloading
    }
}