using System.Collections.Generic;
using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
	[CreateAssetMenu(fileName = "TacticalPositionData", menuName = "FPSDemo/TacticalPositionGrid/Data")]
	public class TacticalPositionData : ScriptableObject
	{
		public List<TacticalPosition> Positions;
	}

	[System.Serializable]
	public struct TacticalPosition
	{
		public Vector3 Position;
		public CoverStatus[] CoverDirections;
	}

	[System.Serializable]
	public struct CoverStatus
	{
		public bool ProvidesCover;

		// TODO additional raycasting around the position has to be done to detect these properties
		public bool LowCover;
		public bool LeftCorner;
		public bool RightCorner;
	}
}