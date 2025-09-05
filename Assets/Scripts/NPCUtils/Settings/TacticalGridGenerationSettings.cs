using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
	[CreateAssetMenu(fileName = "New tactical grid generation settings", menuName = "FPSDemo/TacticalGrid/GridSettings")]
	public class TacticalGridGenerationSettings : ScriptableObject
	{
		[Header("Grid generation settings")]
		public Vector3 StartPos;
		public Vector3 EndPos;
		public float DistanceBetweenPositions = 3f;
		public bool OffsetEveryOtherRow = false;
		[Range(4, 12)]
		public int NumberOfRaysSpawner = 8;
		public float floatPrecisionBuffer = 0.01f;
	}
}