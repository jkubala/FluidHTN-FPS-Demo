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

	public enum CoverType
	{
		LowCover, HighCover, NoCover
	}

	[System.Serializable]
	public struct CoverStatus
	{
		// TODO additional raycasting around the position has to be done to detect these properties
		public CoverType coverType;
		public bool LeftCorner;
		public bool RightCorner;
	}
}