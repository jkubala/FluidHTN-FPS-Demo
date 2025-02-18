using System.Collections.Generic;
using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
	[CreateAssetMenu(fileName = "TacticalPositionData", menuName = "FPSDemo/TacticalPositionGrid/Data")]
	public class TacticalPositionData : ScriptableObject
	{
		public List<TacticalPosition> _positions = new();
	}

	[System.Serializable]
	public struct TacticalPosition
	{
		public Vector3 position;
		public CoverStatus[] coverDirections;
	}

	[System.Serializable]
	public struct CoverStatus
	{
		public bool providesCover;

		// TODO additional raycasting around the position has to be done to detect these properties
		public bool lowCover;
		public bool leftCorner;
		public bool rightCorner;
	}
}