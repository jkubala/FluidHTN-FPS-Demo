using FluidHTN.Operators;
using FluidHTN;
using UnityEngine;

namespace FPSDemo.NPC.Operators
{
    public class HoldDefensivePositionOperator : IOperator
    {
        private float _holdStartTime;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private bool _isHolding;

        public TaskStatus Update(IContext ctx)
        {
            if (ctx is AIContext c)
            {
                if (!_isHolding)
                {
                    StartHoldingPosition(c);
                }

                return UpdateHoldingPosition(c);
            }

            return TaskStatus.Failure;
        }

        private void StartHoldingPosition(AIContext context)
        {
            _holdStartTime = Time.time;
            _initialPosition = context.ThisNPC.transform.position;
            _initialRotation = context.ThisNPC.transform.rotation;
            _isHolding = true;

            // Stop any movement
            context.ThisController.SetDestination(null);

            // Orient towards enemy if available
            if (context.CurrentEnemy != null)
            {
                var directionToEnemy = (context.CurrentEnemy.transform.position - context.ThisNPC.transform.position).normalized;
                var targetRotation = Quaternion.LookRotation(directionToEnemy);
                context.ThisNPC.transform.rotation = targetRotation;
            }
        }

        private TaskStatus UpdateHoldingPosition(AIContext context)
        {
            // Maintain position and orientation
            if (context.CurrentEnemy != null)
            {
                // Track enemy with rotation
                var directionToEnemy = (context.CurrentEnemy.transform.position - context.ThisNPC.transform.position).normalized;
                var targetRotation = Quaternion.LookRotation(directionToEnemy);
                context.ThisNPC.transform.rotation = Quaternion.Slerp(
                    context.ThisNPC.transform.rotation,
                    targetRotation,
                    Time.deltaTime * 2f
                );
            }

            // Check if we need to abandon defensive position
            if (ShouldAbandonPosition(context))
            {
                return TaskStatus.Failure;
            }

            // Successful defensive holding - this task continues indefinitely until conditions change
            return TaskStatus.Continue;
        }

        private bool ShouldAbandonPosition(AIContext context)
        {
            // Check if current position is no longer safe
            if (context.HasState(AIWorldState.CurrentPositionCompromised) && 
                context.GetState(AIWorldState.CurrentPositionCompromised) > 0)
            {
                return true;
            }

            // Check if we're being overwhelmed (could check for multiple enemies in future)
            if (context.CurrentEnemyData != null && 
                context.CurrentEnemyData.awarenessOfThisTarget >= context.AlertAwarenessThreshold &&
                context.CurrentEnemy != null)
            {
                float distanceToEnemy = Vector3.Distance(
                    context.ThisNPC.transform.position, 
                    context.CurrentEnemy.transform.position
                );
                
                // If enemy is too close and we're detected, consider abandoning
                if (distanceToEnemy < context.IdealEnemyRange * 0.5f)
                {
                    return true;
                }
            }

            // Check if a significantly better position became available
            if (context.HasState(AIWorldState.HasBetterCoverAvailable) &&
                context.GetState(AIWorldState.HasBetterCoverAvailable) > 0)
            {
                var coverQualityScore = context.HasState(AIWorldState.CoverQualityScore) ? 
                    context.GetState(AIWorldState.CoverQualityScore) : 0;
                
                // Only abandon for significantly better positions
                if (coverQualityScore > 200) // Threshold for abandoning current position
                {
                    return true;
                }
            }

            return false;
        }

        public void Stop(IContext ctx)
        {
            _isHolding = false;
        }

        public void Aborted(IContext ctx)
        {
            Stop(ctx);
        }
    }
}