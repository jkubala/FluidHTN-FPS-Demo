using FluidHTN.Operators;
using FluidHTN;
using UnityEngine.AI;
using UnityEngine;

namespace FPSDemo.NPC.Operators
{
    public class EmergencyRepositionOperator : IOperator
    {
        private float _originalSpeed;

        public TaskStatus StartNavigation(AIContext c)
        {
            var safePosition = c.GetNearestSafePosition();
            if (safePosition == null)
            {
                // If no safe position available, try to move away from current enemy
                if (c.CurrentEnemy != null)
                {
                    var directionAwayFromEnemy = (c.ThisNPC.transform.position - c.CurrentEnemy.transform.position).normalized;
                    var fallbackDestination = c.ThisNPC.transform.position + directionAwayFromEnemy * 15f;
                    
                    if (NavMesh.SamplePosition(fallbackDestination, out var fallbackHit, 5.0f, NavMesh.AllAreas))
                    {
                        c.ThisController.SetDestination(fallbackHit.position);
                        // Store original speed and set higher movement speed for emergency repositioning
                        _originalSpeed = c.ThisController.Speed;
                        c.ThisController.Speed = _originalSpeed * 1.5f;
                        return TaskStatus.Continue;
                    }
                }
                
                return TaskStatus.Failure;
            }

            if (NavMesh.SamplePosition(safePosition.Position, out var hit, 2.0f, NavMesh.AllAreas))
            {
                c.ThisController.SetDestination(hit.position);
                // Store original speed and set higher movement speed for emergency repositioning
                _originalSpeed = c.ThisController.Speed;
                c.ThisController.Speed = _originalSpeed * 1.5f;
                return TaskStatus.Continue;
            }

            return TaskStatus.Failure;
        }

        public TaskStatus UpdateNavigation(AIContext c)
        {
            if (c.ThisController.DistanceToDestination <= c.ThisController.StoppingDistance)
            {
                c.ThisController.SetDestination(null);
                // Reset speed to normal
                ResetMovementSpeed(c);
                return TaskStatus.Success;
            }

            return TaskStatus.Continue;
        }

        public TaskStatus Update(IContext ctx)
        {
            if (ctx is AIContext c)
            {
                if (c.ThisController.IsStopped)
                {
                    return StartNavigation(c);
                }
                else
                {
                    return UpdateNavigation(c);
                }
            }

            return TaskStatus.Failure;
        }

        public void Stop(IContext ctx)
        {
            if (ctx is AIContext c)
            {
                c.ThisController.SetDestination(null);
                ResetMovementSpeed(c);
            }
        }

        public void Aborted(IContext ctx)
        {
            Stop(ctx);
        }

        private void ResetMovementSpeed(AIContext c)
        {
            // Reset to original speed
            if (_originalSpeed > 0)
            {
                c.ThisController.Speed = _originalSpeed;
            }
        }
    }
}