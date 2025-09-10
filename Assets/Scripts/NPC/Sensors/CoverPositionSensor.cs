using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using FluidHTN;
using FPSDemo.Core;
using FPSDemo.NPC.Data;
using FPSDemo.NPC.Utilities;
using FPSDemo.Target;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FPSDemo.NPC.Sensors
{
    [RequireComponent(typeof(HumanTarget))]
    public class CoverPositionSensor : MonoBehaviour, ISensor
    {
        // ========================================================= INSPECTOR FIELDS
        
        [SerializeField] private HumanTarget _thisTarget;
        [SerializeField] private LayerMask _raycastMask = 1 | (1 << 2);
        [SerializeField] private float _searchRadius = 50f;
        [SerializeField] private float _positionChangeThreshold = 2.0f;
        
        // ========================================================= PRIVATE FIELDS
        
        private Vector3 _lastNPCPosition;
        private List<TacticalPosition> _nearbyPositions = new();
        private TacticalPositionEvaluation _bestPosition;
        private TacticalPositionEvaluation _nearestSafePosition;
        private List<TacticalPositionEvaluation> _flankingPositions = new();
        private bool _needsReevaluation = true;
        
        // ========================================================= PROPERTIES
        
        public float TickRate => Game.AISettings?.CoverPositionSensorTickRate ?? 0.2f;
        public float NextTickTime { get; set; }
        
        // ========================================================= UNITY METHODS
        
        private void OnValidate()
        {
            if (_thisTarget == null)
            {
                _thisTarget = GetComponent<HumanTarget>();
            }
        }
        
        private void Start()
        {
            _lastNPCPosition = transform.position;
        }
        
        // ========================================================= TICK
        
        public void Tick(AIContext context)
        {
            // Check if we need to reevaluate positions
            bool positionChanged = Vector3.Distance(transform.position, _lastNPCPosition) > _positionChangeThreshold;
            bool hasCurrentEnemy = context.CurrentEnemy != null;
            
            if (_needsReevaluation || positionChanged || hasCurrentEnemy)
            {
                _lastNPCPosition = transform.position;
                UpdateNearbyPositions();
                EvaluatePositions(context);
                UpdateWorldState(context);
                _needsReevaluation = false;
            }
        }
        
        // ========================================================= POSITION EVALUATION
        
        private void UpdateNearbyPositions()
        {
            _nearbyPositions.Clear();
            
            // Get tactical settings from Game manager - proper data access pattern
            var tacticalSettings = Game.TacticalSettings;
            if (tacticalSettings == null) return;
            
            // Get all generation contexts and extract positions from them
            var contexts = tacticalSettings.GetContextsFor(TacticalPositionGenerator.CoverGenerationMode.All);
            
            // Also include manual positions
            var manualPositions = tacticalSettings.GetManualPositionData();
            if (manualPositions != null)
            {
                AddNearbyPositionsFromData(manualPositions);
            }
            
            // Add positions from all generation contexts
            foreach (var context in contexts)
            {
                if (context?.positionData != null)
                {
                    AddNearbyPositionsFromData(context.positionData);
                }
            }
        }
        
        private void AddNearbyPositionsFromData(TacticalPositionData positionData)
        {
            var npcPosition = transform.position;
            var searchRadiusSquared = _searchRadius * _searchRadius; // Avoid sqrt in distance checks
            
            foreach (var position in positionData.Positions)
            {
                // Fast distance check using squared distance
                var deltaX = position.Position.x - npcPosition.x;
                var deltaZ = position.Position.z - npcPosition.z;
                var distanceSquared = deltaX * deltaX + deltaZ * deltaZ;
                
                if (distanceSquared <= searchRadiusSquared)
                {
                    _nearbyPositions.Add(position);
                }
            }
        }
        
        private void EvaluatePositions(AIContext context)
        {
            var evaluations = new List<TacticalPositionEvaluation>();
            
            foreach (var position in _nearbyPositions)
            {
                var evaluation = EvaluatePosition(position, context);
                if (evaluation != null)
                {
                    evaluations.Add(evaluation);
                }
            }
            
            // Sort by composite score - avoiding LINQ for performance
            evaluations.Sort((a, b) => b.CompositeScore.CompareTo(a.CompositeScore));
            
            // Update cached evaluations - manual filtering instead of LINQ
            _bestPosition = evaluations.Count > 0 ? evaluations[0] : null;
            
            // Find nearest safe position
            _nearestSafePosition = null;
            float nearestSafeDistance = float.MaxValue;
            foreach (var eval in evaluations)
            {
                if (eval.SafetyScore > 0.7f && eval.DistanceToPosition < nearestSafeDistance)
                {
                    _nearestSafePosition = eval;
                    nearestSafeDistance = eval.DistanceToPosition;
                }
            }
            
            // Find flanking positions (max 3)
            _flankingPositions.Clear();
            foreach (var eval in evaluations)
            {
                if (eval.IsFlankingPosition && eval.TacticalAdvantageScore > 0.6f && _flankingPositions.Count < 3)
                {
                    _flankingPositions.Add(eval);
                }
            }
        }
        
        private TacticalPositionEvaluation EvaluatePosition(TacticalPosition position, AIContext context)
        {
            var evaluation = new TacticalPositionEvaluation
            {
                Position = position,
                DistanceToPosition = Vector3.Distance(position.Position, transform.position)
            };
            
            // Calculate safety score
            evaluation.SafetyScore = CalculateSafetyScore(position, context);
            
            // Calculate tactical advantage score
            evaluation.TacticalAdvantageScore = CalculateTacticalAdvantageScore(position, context);
            
            // Calculate accessibility score
            evaluation.AccessibilityScore = CalculateAccessibilityScore(position, context);
            
            // Determine if this is a flanking position
            evaluation.IsFlankingPosition = IsFlankingPosition(position, context);
            
            // Calculate composite score
            evaluation.CalculateCompositeScore();
            
            return evaluation;
        }
        
        private float CalculateSafetyScore(TacticalPosition position, AIContext context)
        {
            // If we took damage near this position recently, it's compromised
            if (context.WasDamagedNear(position.Position))
            {
                return 0.0f;
            }
            
            float safetyScore = 0.5f; // Base safety
            
            if (context.CurrentEnemy != null)
            {
                Vector3 enemyPosition = context.CurrentEnemy.transform.position;
                Vector3 positionToEnemy = enemyPosition - position.Position;
                
                // Check cover angle relative to enemy
                Vector3 coverDirection = position.mainCover.rotationToAlignWithCover * Vector3.forward;
                float coverAngle = Vector3.Angle(-positionToEnemy.normalized, coverDirection);
                
                // Better cover if we're facing away from enemy or at angle
                if (coverAngle < 45f) // Facing enemy - good cover
                {
                    safetyScore += 0.4f;
                }
                else if (coverAngle < 90f) // Side cover
                {
                    safetyScore += 0.2f;
                }
                
                // Penalize positions too close to enemy
                float distanceToEnemy = positionToEnemy.magnitude;
                if (distanceToEnemy < 10f)
                {
                    safetyScore -= 0.3f;
                }
                else if (distanceToEnemy < context.IdealEnemyRange)
                {
                    safetyScore += 0.1f;
                }
            }
            
            // Check for escape routes (simplified)
            if (HasEscapeRoute(position))
            {
                safetyScore += 0.1f;
            }
            
            return Mathf.Clamp01(safetyScore);
        }
        
        private float CalculateTacticalAdvantageScore(TacticalPosition position, AIContext context)
        {
            float tacticalScore = 0.3f; // Base tactical value
            
            if (context.CurrentEnemy != null)
            {
                Vector3 enemyPosition = context.CurrentEnemy.transform.position;
                Vector3 positionToEnemy = enemyPosition - position.Position;
                float distanceToEnemy = positionToEnemy.magnitude;
                
                // Ideal engagement range bonus
                float rangeOptimality = 1f - Mathf.Abs(distanceToEnemy - context.IdealEnemyRange) / context.IdealEnemyRange;
                tacticalScore += rangeOptimality * 0.3f;
                
                // Height advantage
                if (position.Position.y > enemyPosition.y + 1f)
                {
                    tacticalScore += 0.2f;
                }
                
                // Check for flanking opportunity
                if (IsFlankingPosition(position, context))
                {
                    tacticalScore += 0.2f;
                }
            }
            
            return Mathf.Clamp01(tacticalScore);
        }
        
        private float CalculateAccessibilityScore(TacticalPosition position, AIContext context)
        {
            float accessibilityScore = 0.5f; // Base accessibility
            var distance = Vector3.Distance(transform.position, position.Position);
            if (distance <= context.ThisController.StoppingDistance)
            {
                return 1f; // We're already there!
            }
            
            // NavMesh path check
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(transform.position, position.Position, NavMesh.AllAreas, path))
            {
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    accessibilityScore += 0.3f;
                    
                    // Shorter paths are better
                    float pathLength = CalculatePathLength(path);
                    float directDistance = Vector3.Distance(transform.position, position.Position);
                    float pathEfficiency = directDistance / pathLength;
                    accessibilityScore += pathEfficiency * 0.2f;
                }
                else if (path.status == NavMeshPathStatus.PathPartial)
                {
                    accessibilityScore += 0.1f;
                }
            }
            
            return Mathf.Clamp01(accessibilityScore);
        }
        
        private bool IsFlankingPosition(TacticalPosition position, AIContext context)
        {
            if (context.CurrentEnemy == null) return false;
            
            Vector3 enemyPosition = context.CurrentEnemy.transform.position;
            Vector3 enemyToPosition = position.Position - enemyPosition;
            Vector3 enemyToNPC = transform.position - enemyPosition;
            
            // Check if position would be to the side or behind enemy relative to current position
            float angle = Vector3.Angle(enemyToNPC.normalized, enemyToPosition.normalized);
            return angle > 60f && angle < 150f; // Side flanking positions
        }
        
        // ========================================================= WORLD STATE UPDATE
        
        private void UpdateWorldState(AIContext context)
        {
            // Update world state based on tactical analysis
            if (_bestPosition != null)
            {
                // Check if we have better cover available
                float currentPositionQuality = EvaluateCurrentPosition(context);
                bool hasBetterCover = _bestPosition.CompositeScore > currentPositionQuality + 0.2f; // Hysteresis
                context.SetState(AIWorldState.HasBetterCoverAvailable, hasBetterCover, EffectType.Permanent);
                
                // Set cover quality score (0-255 range)
                byte coverQuality = (byte)(Mathf.Clamp01(_bestPosition.CompositeScore) * 255);
                context.SetState(AIWorldState.CoverQualityScore, coverQuality, EffectType.Permanent);
                
                // Set distance to best cover (0-255 range, normalized by search radius)
                byte coverDistance = (byte)(Mathf.Clamp01(_bestPosition.DistanceToPosition / _searchRadius) * 255);
                context.SetState(AIWorldState.BestCoverDistance, coverDistance, EffectType.Permanent);
            }
            
            // Current position assessment
            float currentSafety = EvaluateCurrentPositionSafety(context);
            context.SetState(AIWorldState.CurrentPositionCompromised, currentSafety < 0.3f, EffectType.Permanent);
            context.SetState(AIWorldState.InEffectiveCoverPosition, currentSafety > 0.7f, EffectType.Permanent);
            context.SetState(AIWorldState.RequiresRepositioning, currentSafety <= 0.7f, EffectType.Permanent);
            
            // Flanking opportunities
            context.SetState(AIWorldState.FlankingOpportunityAvailable, _flankingPositions.Count > 0, EffectType.Permanent);
            
            // Update AIContext cached data
            context.BestCoverPosition = _bestPosition?.Position;
            context.NearestSafePosition = _nearestSafePosition?.Position;
            
            // Manual conversion instead of LINQ for performance
            context.FlankingPositions.Clear();
            foreach (var evaluation in _flankingPositions)
            {
                context.FlankingPositions.Add(evaluation.Position);
            }
        }
        
        // ========================================================= UTILITY METHODS
        
        private float EvaluateCurrentPosition(AIContext context)
        {
            // Create a dummy tactical position for current location
            var currentPos = new TacticalPosition
            {
                Position = transform.position,
                mainCover = new MainCover
                {
                    type = CoverType.Normal,
                    height = CoverHeight.NoCover,
                    rotationToAlignWithCover = transform.rotation
                }
            };
            
            var evaluation = EvaluatePosition(currentPos, context);
            return evaluation?.CompositeScore ?? 0.1f;
        }
        
        private float EvaluateCurrentPositionSafety(AIContext context)
        {
            var currentPos = new TacticalPosition
            {
                Position = transform.position,
                mainCover = new MainCover
                {
                    type = CoverType.Normal,
                    height = CoverHeight.NoCover,
                    rotationToAlignWithCover = transform.rotation
                }
            };
            
            return CalculateSafetyScore(currentPos, context);
        }
        
        private bool HasEscapeRoute(TacticalPosition position)
        {
            // Simple escape route check - can we path away from this position
            Vector3 escapeDirection = -transform.forward;
            Vector3 escapeTarget = position.Position + escapeDirection * 10f;
            
            NavMeshPath path = new NavMeshPath();
            return NavMesh.CalculatePath(position.Position, escapeTarget, NavMesh.AllAreas, path) 
                   && path.status != NavMeshPathStatus.PathInvalid;
        }
        
        private float CalculatePathLength(NavMeshPath path)
        {
            float length = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return length;
        }
        
        // ========================================================= DEBUG / GIZMOS
        
#if UNITY_EDITOR
        [CustomEditor(typeof(CoverPositionSensor))]
        public class CoverPositionSensorEditor : Editor
        {
            public void OnSceneGUI()
            {
                var sensor = target as CoverPositionSensor;
                if (!sensor.enabled || Application.isPlaying == false) return;
                
                // Get AIContext to access current state
                var npc = sensor.GetComponent<NPC>();
                if (npc?.Context == null) return;
                var context = npc.Context;
                
                DrawSearchRadius(sensor);
                DrawNearbyPositions(sensor);
                DrawBestPosition(sensor, context);
                DrawNearestSafePosition(sensor, context);
                DrawFlankingPositions(sensor, context);
                DrawCurrentPositionInfo(sensor, context);
                DrawEnemyInfo(sensor, context);
                DrawWorldStateInfo(sensor, context);
                DrawDamageHistory(sensor, context);
            }
            
            private void DrawSearchRadius(CoverPositionSensor sensor)
            {
                // Draw search radius
                Handles.color = new Color(0.5f, 0.5f, 1f, 0.1f);
                Handles.DrawSolidDisc(sensor.transform.position, Vector3.up, sensor._searchRadius);
                
                Handles.color = new Color(0.5f, 0.5f, 1f, 0.3f);
                Handles.DrawWireDisc(sensor.transform.position, Vector3.up, sensor._searchRadius);
            }
            
            private void DrawNearbyPositions(CoverPositionSensor sensor)
            {
                // Draw all nearby positions
                Handles.color = new Color(1f, 1f, 1f, 0.3f);
                foreach (var position in sensor._nearbyPositions)
                {
                    Handles.DrawWireCube(position.Position, Vector3.one * 0.5f);
                    
                    // Draw cover direction
                    Vector3 coverDirection = position.mainCover.rotationToAlignWithCover * Vector3.forward;
                    Handles.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
                    Handles.DrawLine(position.Position, position.Position + coverDirection * 2f);
                }
            }
            
            private void DrawBestPosition(CoverPositionSensor sensor, AIContext context)
            {
                if (sensor._bestPosition == null) return;
                
                // Draw best position with larger green cube
                Handles.color = new Color(0f, 1f, 0f, 0.8f);
                Handles.DrawWireCube(sensor._bestPosition.Position.Position, Vector3.one * 1.2f);
                
                // Draw line from NPC to best position
                Handles.color = new Color(0f, 1f, 0f, 0.6f);
                Handles.DrawLine(sensor.transform.position, sensor._bestPosition.Position.Position);
                
                // Label with score information
                var labelPos = sensor._bestPosition.Position.Position + Vector3.up * 2f;
                var labelContent = $"BEST\nScore: {sensor._bestPosition.CompositeScore:F2}\nSafety: {sensor._bestPosition.SafetyScore:F2}\nTactical: {sensor._bestPosition.TacticalAdvantageScore:F2}\nAccess: {sensor._bestPosition.AccessibilityScore:F2}\nDist: {sensor._bestPosition.DistanceToPosition:F1}m";
                
                Handles.Label(labelPos, labelContent);
            }
            
            private void DrawNearestSafePosition(CoverPositionSensor sensor, AIContext context)
            {
                if (sensor._nearestSafePosition == null || sensor._nearestSafePosition == sensor._bestPosition) return;
                
                // Draw nearest safe position with blue cube
                Handles.color = new Color(0f, 0.5f, 1f, 0.8f);
                Handles.DrawWireCube(sensor._nearestSafePosition.Position.Position, Vector3.one * 1f);
                
                // Draw line from NPC to nearest safe position
                Handles.color = new Color(0f, 0.5f, 1f, 0.4f);
                Handles.DrawLine(sensor.transform.position, sensor._nearestSafePosition.Position.Position);
                
                // Label
                var labelPos = sensor._nearestSafePosition.Position.Position + Vector3.up * 1.5f;
                var labelContent = $"SAFE\nSafety: {sensor._nearestSafePosition.SafetyScore:F2}\nDist: {sensor._nearestSafePosition.DistanceToPosition:F1}m";
                
                Handles.Label(labelPos, labelContent);
            }
            
            private void DrawFlankingPositions(CoverPositionSensor sensor, AIContext context)
            {
                int index = 0;
                foreach (var flankPos in sensor._flankingPositions)
                {
                    // Draw flanking positions with orange cubes
                    Handles.color = new Color(1f, 0.5f, 0f, 0.7f);
                    Handles.DrawWireCube(flankPos.Position.Position, Vector3.one * 0.8f);
                    
                    // Draw line from NPC to flanking position
                    Handles.color = new Color(1f, 0.5f, 0f, 0.3f);
                    Handles.DrawLine(sensor.transform.position, flankPos.Position.Position);
                    
                    // Label
                    var labelPos = flankPos.Position.Position + Vector3.up * (1f + index * 0.5f);
                    var labelContent = $"FLANK {index + 1}\nTactical: {flankPos.TacticalAdvantageScore:F2}\nDist: {flankPos.DistanceToPosition:F1}m";
                    
                    Handles.Label(labelPos, labelContent);
                    index++;
                }
            }
            
            private void DrawCurrentPositionInfo(CoverPositionSensor sensor, AIContext context)
            {
                // Draw current position assessment
                float currentSafety = sensor.EvaluateCurrentPositionSafety(context);
                float currentQuality = sensor.EvaluateCurrentPosition(context);
                
                Color currentPosColor = currentSafety > 0.7f ? Color.green : 
                                      currentSafety > 0.3f ? Color.yellow : Color.red;
                
                Handles.color = new Color(currentPosColor.r, currentPosColor.g, currentPosColor.b, 0.3f);
                Handles.DrawSolidDisc(sensor.transform.position, Vector3.up, 1.5f);
                
                // Current position info label
                var labelPos = sensor.transform.position + Vector3.up * 3f;
                var labelContent = $"CURRENT POS\nSafety: {currentSafety:F2}\nQuality: {currentQuality:F2}";
                
                Handles.Label(labelPos, labelContent);
            }
            
            private void DrawEnemyInfo(CoverPositionSensor sensor, AIContext context)
            {
                if (context.CurrentEnemy == null) return;
                
                // Draw line to current enemy
                Handles.color = new Color(1f, 0f, 0f, 0.8f);
                Handles.DrawLine(sensor.transform.position, context.CurrentEnemy.transform.position);
                
                // Enemy info
                float distanceToEnemy = Vector3.Distance(sensor.transform.position, context.CurrentEnemy.transform.position);
                var labelPos = context.CurrentEnemy.transform.position + Vector3.up * 2.5f;
                var labelContent = $"ENEMY\nDist: {distanceToEnemy:F1}m\nIdeal: {context.IdealEnemyRange:F1}m\nAwareness: {context.CurrentEnemyData?.awarenessOfThisTarget:F2}";
                
                Handles.Label(labelPos, labelContent);
            }
            
            private void DrawWorldStateInfo(CoverPositionSensor sensor, AIContext context)
            {
                // Draw world state information as text overlay
                var infoPos = sensor.transform.position + Vector3.right * 5f + Vector3.up * 2f;
                
                var worldStateInfo = "=== WORLD STATE ===\n";
                worldStateInfo += $"Has Better Cover: {context.HasState(AIWorldState.HasBetterCoverAvailable)}\n";
                worldStateInfo += $"Position Compromised: {context.HasState(AIWorldState.CurrentPositionCompromised)}\n";
                worldStateInfo += $"In Effective Cover: {context.HasState(AIWorldState.InEffectiveCoverPosition)}\n";
                worldStateInfo += $"Requires Reposition: {context.HasState(AIWorldState.RequiresRepositioning)}\n";
                worldStateInfo += $"Flanking Available: {context.HasState(AIWorldState.FlankingOpportunityAvailable)}\n";
                worldStateInfo += $"Cover Quality: {context.GetState(AIWorldState.CoverQualityScore)}/255\n";
                worldStateInfo += $"Cover Distance: {context.GetState(AIWorldState.BestCoverDistance)}/255\n";
                worldStateInfo += $"Nearby Positions: {sensor._nearbyPositions.Count}\n";
                
                Handles.Label(infoPos, worldStateInfo);
            }
            
            private void DrawDamageHistory(CoverPositionSensor sensor, AIContext context)
            {
                if (!context.HasRecentDamage) return;
                
                // Draw damage location with red pulsing circle
                float pulseIntensity = Mathf.PingPong(Time.time * 2f, 1f);
                Handles.color = new Color(1f, 0f, 0f, 0.3f + pulseIntensity * 0.4f);
                Handles.DrawSolidDisc(context.LastDamagePosition, Vector3.up, 3f);
                
                Handles.color = new Color(1f, 0f, 0f, 0.8f);
                Handles.DrawWireDisc(context.LastDamagePosition, Vector3.up, 3f);
                
                // Damage info
                float timeSinceDamage = Time.time - context.LastDamageTime;
                var labelPos = context.LastDamagePosition + Vector3.up * 2f;
                var labelContent = $"DAMAGE AREA\n{timeSinceDamage:F1}s ago\n3m radius";
                
                Handles.Label(labelPos, labelContent);
                
                // Line from current position to damage location
                Handles.color = new Color(1f, 0f, 0f, 0.5f);
                Handles.DrawLine(sensor.transform.position, context.LastDamagePosition);
            }
        }
#endif
    }
}