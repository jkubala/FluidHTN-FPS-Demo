using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
    [CreateAssetMenu(fileName = "New corner settings", menuName = "FPSDemo/TacticalPositions/CornerSettings")]
    public class TacticalCornerSettings : ScriptableObject
    {
        [Header("Corner detection settings")]
        public float cornerCheckRayWallOffset = 0.1f; // how far from the wall should the raycasts be fired from
        public float cornerCheckRayStep = 0.01f; // how much distance between the individual raycasts when scanning the wall
        public float cornerCheckRaySequenceDistance = 2f; // how far to the side should the raycasts go when scanning the wall
        public float cornerCheckPositionOffset = 0.25f; // how far to offset the position from the found corner
        public float minWidthToConsiderAValidPosition = 0.7f;
        public float minAngleToConsiderCorner = 15f;
        [Range(0.001f, 1f)]
        public float rayLengthBeyondWall = 0.25f;
        public float floatPrecisionBuffer = 0.01f; // To avoid situations of no hit, because the ray is 0.000001f short due to float imprecision
        public Vector2 sphereCastForFiringPositionCheckOffset = new(0.1f, 0f);
        public float sphereCastForFiringPositionCheckRadius = 0.15f;
        public float sphereCastForFiringPositionCheckDistance = 1f;
        public int nOfHitsOfDifferentNormalToConsiderCorner = 3;
        public float heightOfHighFiringPosition = 1.6f;
        public float heightOfLowFiringPosition = 0.5f;
    }
}