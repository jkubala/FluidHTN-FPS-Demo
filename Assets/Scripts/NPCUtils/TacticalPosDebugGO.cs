using System.Collections.Generic;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
	public class TacticalPosDebugGO : MonoBehaviour
	{
		enum DebugMode { Corner, Non90DegreeCorner, Obstacle }
		[SerializeField] DebugMode debugMode;

		public SpecialCover specialCover;
		public Vector3 origCornerRayPos;
		public TacticalGridGenerationSettings gridSettings;

		public Vector3 offsetPosition, leftDirection, finalCornerPos;
		public Vector3 sphereCastAnchor, sphereCastOrigin, sphereCastDirection, sphereCastNormal, cornerNormal, cornerFiringNormal;
		public float distanceToObstacleLeft, distanceToObstacleRight, maxDistLeft, maxDistRight;
		public float distLeft2, distRight2;
		public Vector3? leftCornerPos, rightCornerPos;
		public List<Vector3> hitPositions;
		public Vector3? firstNormal, secondNormal, secondNormalHit;

		private void DrawSphere(Vector3 position, float radius, Color color)
		{
			Color curColor = Gizmos.color;
			Gizmos.color = color;
			Gizmos.DrawSphere(position, radius);
			Gizmos.color = curColor;
		}

		private void DrawRay(Vector3 position, Vector3 direction, Color color)
		{
			Color curColor = Gizmos.color;
			Gizmos.color = color;
			Gizmos.DrawRay(position, direction);
			Gizmos.color = curColor;
		}

		private void GetHighPosAdjustedToCornerDebug()
		{
			// Looking for left and right corners
			DrawRay(offsetPosition, leftDirection * distanceToObstacleLeft, Color.black);
			DrawRay(offsetPosition, -leftDirection * distanceToObstacleRight, Color.black);
			DrawSphere(offsetPosition, 0.1f, Color.black);
			DrawSphere(offsetPosition + leftDirection * maxDistLeft, 0.025f, Color.black);
			DrawSphere(offsetPosition - leftDirection * maxDistRight, 0.025f, Color.black);

			if (leftCornerPos.HasValue)
			{
				DrawSphere(leftCornerPos.Value, 0.1f, Color.yellow);
			}

			if (rightCornerPos.HasValue)
			{
				DrawSphere(rightCornerPos.Value, 0.1f, Color.cyan);
			}

			foreach (Vector3 pos in hitPositions)
			{
				DrawSphere(pos, 0.01f, Color.red);
			}

			DrawRay(finalCornerPos, specialCover.rotationToAlignWithCover.eulerAngles, Color.green);
		}

		private void ObstacleInFiringPositionDebug()
		{
			DrawSphere(sphereCastOrigin, 0.1f, Color.black);
			DrawRay(sphereCastOrigin, sphereCastDirection, Color.blue);
			DrawRay(finalCornerPos, cornerNormal, Color.cyan);
			DrawRay(finalCornerPos, cornerFiringNormal, Color.black);
		}

		private void Non90DegreeCornerDebug()
		{
			if (firstNormal.HasValue)
			{
				Debug.Log("HEHEE");
				DrawRay(finalCornerPos, firstNormal.Value, Color.black);
			}
			if (secondNormal.HasValue)
			{
				DrawRay(secondNormalHit.Value, secondNormal.Value, Color.yellow);
			}
		}


		void OnDrawGizmosSelected()
		{
			switch (debugMode)
			{
				case DebugMode.Corner:
					GetHighPosAdjustedToCornerDebug();
					break;
				case DebugMode.Obstacle:
					ObstacleInFiringPositionDebug();
					break;
				case DebugMode.Non90DegreeCorner:
					Non90DegreeCornerDebug();
					break;
			}
		}
	}
}