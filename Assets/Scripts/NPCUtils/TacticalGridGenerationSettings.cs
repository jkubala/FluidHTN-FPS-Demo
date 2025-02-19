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
		public int NumberOfRays = 8;
		public float RequiredProximityToNavMesh = 0.1f;
		public float geometryCheckYOffset = 1.0f;
		public LayerMask RaycastMask = 1 << 0;

		[Header("Tactical position settings")]
		public float minHeightToConsiderHighCover = 1.25f;
		public float minHeightToConsiderLowCover = 0.75f;
	}
}