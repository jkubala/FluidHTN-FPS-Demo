using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
	[CreateAssetMenu(fileName = "New tactical grid generation settings", menuName = "FPSDemo/TacticalPositionGrid/Settings")]
	public class TacticalGridGenerationSettings : ScriptableObject
	{
		public Vector3 StartPos;
		public Vector3 EndPos;
		public float DistanceBetweenPositions = 1f;
		public float DistanceOfRaycasts = 0.5f;
		[Range(4, 12)]
		public int NumberOfRays = 8;

		public LayerMask RaycastMask = 1 << 0;
	}
}