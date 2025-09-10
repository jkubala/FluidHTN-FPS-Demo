using FluidHTN.Operators;
using FluidHTN;
using UnityEngine.AI;
using UnityEngine;
using FPSDemo.NPC.Utilities;

namespace FPSDemo.NPC.Operators
{
    public class MoveToBestCoverOperator : IOperator
    {
        public TaskStatus StartNavigation(AIContext c)
        {
            var bestPosition = c.GetBestCoverPosition();
            if (bestPosition == null)
            {
                return TaskStatus.Failure;
            }

            if (NavMesh.SamplePosition(bestPosition.Position, out var hit, 2.0f, NavMesh.AllAreas))
            {
                c.ThisController.SetDestination(hit.position);
                return TaskStatus.Continue;
            }

            return TaskStatus.Failure;
        }

        public TaskStatus UpdateNavigation(AIContext c)
        {
            var bestPosition = c.GetBestCoverPosition();
            if (bestPosition == null)
            {
                return TaskStatus.Failure;
            }

            if (c.ThisController.DistanceToDestination <= c.ThisController.StoppingDistance)
            {
                c.ThisController.SetDestination(null);
                
                // Align with cover direction if needed
                if (bestPosition.mainCover.type != CoverType.Normal)
                {
                    var targetRotation = bestPosition.mainCover.rotationToAlignWithCover;
                    c.ThisNPC.transform.rotation = Quaternion.Slerp(
                        c.ThisNPC.transform.rotation, 
                        targetRotation, 
                        Time.deltaTime * 2f
                    );
                }
                
                return TaskStatus.Success;
            }

            // Update destination if best cover has changed significantly
            var currentDestination = c.ThisController.Destination;
            if (Vector3.Distance(currentDestination, bestPosition.Position) > 3f)
            {
                if (NavMesh.SamplePosition(bestPosition.Position, out var hit, 2.0f, NavMesh.AllAreas))
                {
                    c.ThisController.SetDestination(hit.position);
                }
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
            }
        }

        public void Aborted(IContext ctx)
        {
            Stop(ctx);
        }
    }
}