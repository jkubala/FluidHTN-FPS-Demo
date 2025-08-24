using System.Collections.Generic;
using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
    [CreateAssetMenu(fileName = "TacticalPositionData", menuName = "FPSDemo/TacticalPositions/Data")]
    public class TacticalPositionData : ScriptableObject
    {
        public List<TacticalPosition> HighCornerPositions;
        public List<TacticalPosition> LowCornerPositions;
        public List<TacticalPosition> LowCoverPositions;
    }

    [System.Serializable]
    public class TacticalPosition
    {
        public Vector3 Position;
        public MainCover mainCover;
        public CoverHeight[] CoverDirections;
        public bool isOutside;

        public override string ToString()
        {
            string coverDirectionsStr = CoverDirections != null
                ? string.Join(", ", CoverDirections)
                : "null";

            return $"Position: {Position}\n" +
                   $"MainCover: {mainCover}\n" +
                   $"CoverDirections: [{coverDirectionsStr}]\n" +
                   $"IsOutside: {isOutside}";
        }
    }

    public enum CoverHeight
    {
        LowCover, HighCover, NoCover
    }

    public enum CoverType
    {
        LeftCorner, RightCorner, Normal // Normal is for low cover with no corners, like over a stone fence
    }

    [System.Serializable]
    public struct MainCover
    {
        public CoverType type;
        public CoverHeight height;
        public Quaternion rotationToAlignWithCover;

        public override string ToString()
        {
            return $"Type: {type}, Height: {height}, Rotation: {rotationToAlignWithCover.eulerAngles}";
        }
    }
}