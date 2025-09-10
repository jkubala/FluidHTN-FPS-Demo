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

        // ========================================================= TACTICAL POSITIONING METHODS

        public AIDomainBuilder EmergencyReposition()
        {
            Action("Emergency reposition from compromised position");
            {
                HasState(AIWorldState.CurrentPositionCompromised);
                
                if (Pointer is IPrimitiveTask task)
                {
                    task.SetOperator(new Operators.EmergencyRepositionOperator());
                }
                
                SetState(AIWorldState.IsPursuingEnemy, EffectType.PlanAndExecute);
                SetState(AIWorldState.CurrentPositionCompromised, false, EffectType.PlanOnly);
            }
            End();
            return this;
        }

        public AIDomainBuilder MoveToBestCover()
        {
            Action("Move to best available cover position");
            {
                HasState(AIWorldState.HasBetterCoverAvailable);
                HasStateGreaterThan(AIWorldState.CoverQualityScore, 64); // Minimum quality threshold
                
                if (Pointer is IPrimitiveTask task)
                {
                    task.SetOperator(new Operators.MoveToBestCoverOperator());
                }
                
                SetState(AIWorldState.IsPursuingEnemy, EffectType.PlanAndExecute);
                SetState(AIWorldState.HasBetterCoverAvailable, false, EffectType.PlanOnly);
            }
            End();
            return this;
        }

        public AIDomainBuilder SeekFlankingPosition()
        {
            Action("Seek flanking position on enemy");
            {
                HasState(AIWorldState.FlankingOpportunityAvailable);
                HasState(AIWorldState.AwareOfEnemy);
                
                if (Pointer is IPrimitiveTask task)
                {
                    task.SetOperator(new Operators.SeekFlankingPositionOperator());
                }
                
                SetState(AIWorldState.IsPursuingEnemy, EffectType.PlanAndExecute);
            }
            End();
            return this;
        }

        public AIDomainBuilder HoldDefensivePosition()
        {
            Action("Hold current defensive position");
            {
                HasState(AIWorldState.InEffectiveCoverPosition);
                HasState(AIWorldState.HasBetterCoverAvailable, (byte)0);
                
                if (Pointer is IPrimitiveTask task)
                {
                    task.SetOperator(new Operators.HoldDefensivePositionOperator());
                }
                
                // No state changes - maintaining position
            }
            End();
            return this;
        }

        public AIDomainBuilder TacticalPositioningSelector()
        {
            Select("Tactical Positioning Decision Tree");
            {
                // Priority 1: Emergency repositioning
                EmergencyReposition();
                
                // Priority 2: Opportunistic flanking  
                SeekFlankingPosition();
                
                // Priority 3: Better cover available
                MoveToBestCover();
                
                // Priority 4: Hold position if already good
                HoldDefensivePosition();
            }
            End();
            return this;
        }
    }
}