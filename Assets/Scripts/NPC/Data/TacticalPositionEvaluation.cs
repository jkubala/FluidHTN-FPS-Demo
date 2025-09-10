using UnityEngine;
using FPSDemo.NPC.Utilities;

namespace FPSDemo.NPC.Data
{
    public class TacticalPositionEvaluation
    {
        public TacticalPosition Position { get; set; }
        public float SafetyScore { get; set; }
        public float TacticalAdvantageScore { get; set; }
        public float AccessibilityScore { get; set; }
        public float CompositeScore { get; set; }
        public float DistanceToPosition { get; set; }
        public bool IsFlankingPosition { get; set; }
        
        public void CalculateCompositeScore()
        {
            CompositeScore = (SafetyScore * 0.5f) + (TacticalAdvantageScore * 0.3f) + (AccessibilityScore * 0.2f);
        }
    }
}