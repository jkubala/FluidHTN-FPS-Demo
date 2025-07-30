using System.Collections.Generic;
using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
	[CreateAssetMenu(fileName = "TacticalPositionData", menuName = "FPSDemo/TacticalPositions/Data")]
	public class TacticalPositionData : ScriptableObject
	{
		public List<TacticalPosition> Positions;
	}

	[System.Serializable]
	public struct TacticalPosition
	{
		public Vector3 Position;
		public MainCover mainCover;
		public CoverHeight[] CoverDirections;
		public bool isOutside;
	}



	public enum CoverHeight
	{
		LowCover, HighCover, NoCover
	}

	public enum MainCoverType
	{
		LeftCorner, RightCorner, Normal // Normal is for low cover with no corners, like over a stone fence
	}

	[System.Serializable]
	public struct MainCover
	{
		public MainCoverType type;
		public CoverHeight height;
		public Quaternion rotationToAlignWithCover;
	}
}