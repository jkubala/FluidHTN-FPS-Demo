using FluidHTN.Operators;
using FluidHTN;
using UnityEngine.AI;

namespace FPSDemo.NPC.Operators
{
    public class MoveToPlayerOperator : IOperator
    {
        public TaskStatus StartNavigation(AIContext c)
        {
            if (c.CurrentEnemy == null || c.CurrentEnemy.IsPlayer == false)
            {
                return TaskStatus.Failure;
            }

            var dir = (c.CurrentEnemy.transform.position - c.ThisNPC.transform.position).normalized;
            var destination = c.CurrentEnemy.transform.position + dir * c.IdealEnemyRange;
            if (NavMesh.SamplePosition(destination, out var hit, 1.0f, NavMesh.AllAreas))
            {
                c.ThisController.SetDestination(hit.position);
            }

            if (c.ThisController.DistanceToDestination > c.ThisController.StoppingDistance)
            {
                return TaskStatus.Continue;
            }


            return TaskStatus.Failure;
        }

        public TaskStatus UpdateNavigation(AIContext c)
        {
            
            if (c.CurrentEnemy == null)
            {
                return TaskStatus.Failure;
            }

            if (c.ThisController.DistanceToDestination <= c.ThisController.StoppingDistance)
            {
                c.ThisController.SetDestination(null);
                return TaskStatus.Success;
            }

            var dir = (c.CurrentEnemy.transform.position - c.ThisNPC.transform.position).normalized;
            var destination = c.CurrentEnemy.transform.position + dir * c.IdealEnemyRange;
            if (NavMesh.SamplePosition(destination, out var hit, 1.0f, NavMesh.AllAreas))
            {
                c.ThisController.SetDestination(hit.position);
            }

            if (c.ThisController.DistanceToDestination > c.ThisController.StoppingDistance)
            {
                return TaskStatus.Continue;
            }
            

            return TaskStatus.Failure;
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