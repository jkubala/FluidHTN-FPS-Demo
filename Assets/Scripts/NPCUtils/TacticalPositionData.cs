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
		public CoverType[] CoverDirections;
		public SpecialCover? specialCover;
	}



	public enum CoverType
	{
		LowCover, HighCover, NoCover
	}

	public enum SpecialCoverType
	{
		LeftCorner, RightCorner, Window
	}

	[System.Serializable]
	public struct SpecialCover
	{
		public SpecialCoverType type;
		public Quaternion rotationToAlignWithCover;
	}
}