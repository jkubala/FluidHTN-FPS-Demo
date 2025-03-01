using Codice.CM.Common;
using FPSDemo.NPC.Utilities;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
	public class TacticalPosDebugGO : MonoBehaviour
	{
		public SpecialCover specialCover;
		public Vector3 origCornerRayPos;
		public TacticalGridGenerationSettings gridSettings;

		public Vector3 offsetPosition, leftDirection, finalCornerPos;
		public float distanceToObstacleLeft, distanceToObstacleRight, maxDistLeft, maxDistRight;
		public Vector3? leftCornerPos, rightCornerPos;

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

			DrawRay(finalCornerPos, specialCover.rotationToAlignWithCover.eulerAngles, Color.green);
		}


		void OnDrawGizmosSelected()
		{
			GetHighPosAdjustedToCornerDebug();
		}
	}
}