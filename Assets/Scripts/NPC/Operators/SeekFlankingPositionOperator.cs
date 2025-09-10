using FluidHTN.Operators;
using FluidHTN;
using UnityEngine.AI;
using UnityEngine;
using System.Linq;
using FPSDemo.NPC.Utilities;

namespace FPSDemo.NPC.Operators
{
    public class SeekFlankingPositionOperator : IOperator
    {
        private TacticalPosition _targetFlankingPosition;

        public TaskStatus StartNavigation(AIContext c)
        {
            var flankingPositions = c.GetFlankingPositions();
            if (flankingPositions == null || flankingPositions.Count == 0)
            {
                return TaskStatus.Failure;
            }

            // Select the best flanking position based on current enemy
            _targetFlankingPosition = SelectBestFlankingPosition(c, flankingPositions);
            
            if (_targetFlankingPosition == null)
            {
                return TaskStatus.Failure;
            }

            if (NavMesh.SamplePosition(_targetFlankingPosition.Position, out var hit, 2.0f, NavMesh.AllAreas))
            {
                c.ThisController.SetDestination(hit.position);
                return TaskStatus.Continue;
            }

            return TaskStatus.Failure;
        }

        public TaskStatus UpdateNavigation(AIContext c)
        {
            if (_targetFlankingPosition == null)
            {
                return TaskStatus.Failure;
            }

            if (c.ThisController.DistanceToDestination <= c.ThisController.StoppingDistance)
            {
                c.ThisController.SetDestination(null);
                
                // Orient towards enemy for flanking attack
                if (c.CurrentEnemy != null)
                {
                    var directionToEnemy = (c.CurrentEnemy.transform.position - c.ThisNPC.transform.position).normalized;
                    var targetRotation = Quaternion.LookRotation(directionToEnemy);
                    c.ThisNPC.transform.rotation = Quaternion.Slerp(
                        c.ThisNPC.transform.rotation,
                        targetRotation,
                        Time.deltaTime * 3f
                    );
                }
                
                return TaskStatus.Success;
            }

            // Check if flanking is still viable
            if (!IsFlankingStillViable(c))
            {
                return TaskStatus.Failure;
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
                _targetFlankingPosition = null;
            }
        }

        public void Aborted(IContext ctx)
        {
            Stop(ctx);
        }

        private TacticalPosition SelectBestFlankingPosition(
            AIContext context, 
            System.Collections.Generic.List<TacticalPosition> flankingPositions)
        {
            if (context.CurrentEnemy == null) return null;

            var enemyPosition = context.CurrentEnemy.transform.position;
            var npcPosition = context.ThisNPC.transform.position;

            // Score flanking positions based on tactical advantage
            var scoredPositions = flankingPositions.Select(pos => new {
                Position = pos,
                Score = ScoreFlankingPosition(pos, enemyPosition, npcPosition, context.IdealEnemyRange)
            }).OrderByDescending(x => x.Score);

            return scoredPositions.FirstOrDefault()?.Position;
        }

        private float ScoreFlankingPosition(TacticalPosition position, Vector3 enemyPos, Vector3 npcPos, float idealRange)
        {
            float score = 0f;

            // Distance to ideal engagement range
            float distanceToEnemy = Vector3.Distance(position.Position, enemyPos);
            float rangeScore = 1f - Mathf.Abs(distanceToEnemy - idealRange) / idealRange;
            score += rangeScore * 0.4f;

            // Flanking angle (prefer 90-degree angles)
            Vector3 enemyToNpc = (npcPos - enemyPos).normalized;
            Vector3 enemyToPosition = (position.Position - enemyPos).normalized;
            float flankingAngle = Vector3.Angle(enemyToNpc, enemyToPosition);
            float angleScore = Mathf.Sin(flankingAngle * Mathf.Deg2Rad); // Peaks at 90 degrees
            score += angleScore * 0.3f;

            // Accessibility (closer is better for quick flanking)
            float distanceToPosition = Vector3.Distance(npcPos, position.Position);
            float accessibilityScore = Mathf.Clamp01(1f - distanceToPosition / 30f);
            score += accessibilityScore * 0.3f;

            return score;
        }

        private bool IsFlankingStillViable(AIContext context)
        {
            // Check if enemy has moved significantly or if flanking opportunity is lost
            if (context.CurrentEnemy == null || _targetFlankingPosition == null)
            {
                return false;
            }

            // If enemy is aware of us and facing our direction, flanking may be compromised
            var enemyToNpc = (context.ThisNPC.transform.position - context.CurrentEnemy.transform.position).normalized;
            var enemyForward = context.CurrentEnemy.transform.forward;
            float enemyAwarenessAngle = Vector3.Angle(enemyForward, enemyToNpc);

            // If enemy is directly facing us (within 45 degrees), flanking is compromised
            if (enemyAwarenessAngle < 45f && context.CurrentEnemyData != null && context.CurrentEnemyData.awarenessOfThisTarget > context.AlertAwarenessThreshold * 0.8f)
            {
                return false;
            }

            return true;
        }
    }
}