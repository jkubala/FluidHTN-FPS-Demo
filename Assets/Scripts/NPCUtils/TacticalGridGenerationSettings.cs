using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
	[CreateAssetMenu(fileName = "New tactical grid generation settings", menuName = "FPSDemo/TacticalPositionGrid/Settings")]
	public class TacticalGridGenerationSettings : ScriptableObject
	{
		[Header("Grid generation settings")]
		public Vector3 StartPos;
		public Vector3 EndPos;
		public float DistanceBetweenPositions = 3f;
		public float DistanceOfRaycasts = 0.5f;
		public bool OffsetEveryOtherRow = false;
		[Range(4, 12)]
		public int NumberOfRaysSpawner = 8;
		[Range(4, 12)]
		public int NumberOfRaysPosition = 8;
		public float RequiredProximityToNavMesh = 0.1f;
		public float geometryCheckYOffset = 1.0f;
		public float distanceToRemoveDuplicates = 0.2f;
		public LayerMask RaycastMask = 1 << 0;

		[Header("Corner detection settings")]
		public float cornerCheckRayWallOffset = 0.1f; // how far from the wall should the raycasts be fired from
		public float cornerCheckRayStep = 0.01f; // how much distance between the individual raycasts when scanning the wall
		public float cornerCheckRaySequenceDistance = 2f; // how far to the side should the raycasts go when scanning the wall
		public float cornerCheckPositionOffset = 0.25f; // how far to offset the position from the found corner
		public float minWidthToConsiderAValidPosition = 0.7f;
		public float minAngleToConsiderCorner = 20f;
		[Range(0.001f, 1f)]
		public float rayLengthBeyondWall = 0.001f; // To avoid situations of no hit, because the ray is 0.000001f short due to float imprecision
		public Vector2 sphereCastForFiringPositionCheckOffset = Vector2.one;
		public float sphereCastForFiringPositionCheckRadius = 1f;
		public float sphereCastForFiringPositionCheckDistance = 0.25f;
		public int nOfHitsOfDifferentNormalToConsiderCorner = 3;

		[Header("Tactical position settings")]
		public float minHeightToConsiderHighCover = 1.25f;
		public float minHeightToConsiderLowCover = 0.75f;
	}
}