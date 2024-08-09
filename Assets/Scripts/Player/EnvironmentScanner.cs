using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FPSDemo.Player
{
	public class EnvironmentScanner : MonoBehaviour
	{
		Player player;
		PlayerCrouching playerCrouching;
		[SerializeField] float forwardRaysYPosStart = 1f;
		[SerializeField] float forwardRaysForwardOffset = 0.3f;
		[SerializeField] float forwardRayLength = 0.8f;
		[SerializeField] float heightRayLength = 3.25f;
		[SerializeField] float heightRayLengthUngrounded = 2f;
		[SerializeField] float minDistanceForUniqueXZPoint = 0.01f;
		float forwardRayEveryXDistance = 0.2f;
		float actualRayLengthFromGround;
		float actualRayLengthFromGroundUngrounded;
		float actualForwardRayLength;
		[SerializeField] LayerMask obstacleLayer;
		readonly List<Vector3> forwardRayOrigins = new();
		List<RaycastHit> forwardHitPositions = new();

		void Awake()
		{
			actualRayLengthFromGround = heightRayLength - forwardRaysYPosStart;
			actualRayLengthFromGroundUngrounded = heightRayLengthUngrounded - forwardRaysYPosStart;
			actualForwardRayLength = forwardRayLength - forwardRaysForwardOffset;

			player = GetComponent<Player>();
			playerCrouching = GetComponent<PlayerCrouching>();
			InitRaycastColumn();
		}

		void InitRaycastColumn()
		{
			int nOfRaycasts = Mathf.CeilToInt(actualRayLengthFromGround / forwardRayEveryXDistance);
			Vector3 newRayCastOriginPoint = Vector3.up * forwardRaysYPosStart;
			for (int i = 0; i < nOfRaycasts; i++)
			{
				forwardRayOrigins.Add(newRayCastOriginPoint);
				newRayCastOriginPoint = newRayCastOriginPoint + Vector3.up * forwardRayEveryXDistance;
			}
		}
		RaycastHit[] raycasts = null;
		public List<ValidHit> ObstacleCheck()
		{
			List<ValidHit> validHits = new();
			float heightRayLengthToUse = player.IsGrounded ? heightRayLength : heightRayLengthUngrounded;
			FindOutUniquePointsOnXZPlane();

			Vector3 offsetIntoWall = transform.forward * 0.01f;
			foreach (RaycastHit forwardHit in forwardHitPositions)
			{
				Vector3 coordXZToCheck = forwardHit.point + offsetIntoWall;
				coordXZToCheck.y = transform.position.y + heightRayLengthToUse;
				raycasts = RaycastTrulyAll(coordXZToCheck, Vector3.down, obstacleLayer, 0.01f);
				if (raycasts.Length > 0)
				{
					foreach (RaycastHit hit in raycasts)
					{
						float heightSpace = ValidateHit(hit.point);
						if (heightSpace > 0)
						{
							validHits.Add(new ValidHit(hit.point, Quaternion.LookRotation(-forwardHit.normal), heightSpace));
						}
					}
				}
			}
			return validHits;
		}

		void OnDrawGizmos()
		{
			if (raycasts != null)
			{
				foreach (RaycastHit hit in raycasts)
				{
					Gizmos.color = Color.blue;
					Gizmos.DrawWireSphere(hit.point, 0.25f);
				}
			}

			if (forwardHitPositions != null)
			{
				foreach (RaycastHit hit in forwardHitPositions)
				{
					Gizmos.color = Color.yellow;
					Gizmos.DrawWireSphere(hit.point, 0.5f);
				}
			}
		}

		private void FindOutUniquePointsOnXZPlane()
		{
			forwardHitPositions.Clear();

			float maxHeight = player.IsGrounded ? heightRayLength : heightRayLengthUngrounded;
			foreach (Vector3 origin in forwardRayOrigins)
			{
				Vector3 offsetOrigin = origin + transform.forward * forwardRaysForwardOffset;
				if (origin.y <= maxHeight && Physics.Raycast(transform.position + offsetOrigin, transform.forward,
				out RaycastHit forwardHit, actualForwardRayLength, obstacleLayer))
				{
					if (forwardHitPositions.Count == 0 || !forwardHitPositions.Any(v => Mathf.Abs(v.point.x - forwardHit.point.x) < minDistanceForUniqueXZPoint && Mathf.Abs(v.point.z - forwardHit.point.z) < minDistanceForUniqueXZPoint))
					{
						forwardHitPositions.Add(forwardHit);
					}
				}
			}
		}

		float ValidateHit(Vector3 posToValidate)
		{
			Vector3 origin = posToValidate + new Vector3(0, player.Radius + 0.01f, 0);
			float crouchingDistance = player.crouchFloatingColliderHeight + player.DistanceToFloat - player.Radius * 2;

			if (Physics.CheckSphere(origin, player.Radius, obstacleLayer) || !RaycastAccessibilityCheck(posToValidate))
			{
				return -1;
			}

			if (!Physics.SphereCast(origin, player.Radius, Vector3.up, out RaycastHit hit, crouchingDistance, obstacleLayer))
			{
				float standingDistance = player.characterHeight - player.Radius * 2;
				if (Physics.SphereCast(origin, player.Radius, Vector3.up, out RaycastHit standingHit, standingDistance, obstacleLayer))
				{
					if (standingHit.point.y - posToValidate.y >= playerCrouching.CrouchColliderHeight)
					{
						return standingHit.point.y - posToValidate.y;
					}
				}
				else
				{
					return player.characterHeight;
				}
			}
			return -1;
		}

		bool RaycastAccessibilityCheck(Vector3 posToCheck)
		{
			Vector3 origin = transform.position + Vector3.up * (player.CurrentFloatingColliderHeight + player.DistanceToFloat);
			Vector3 target = posToCheck + Vector3.up * playerCrouching.CrouchColliderHeight;
			return !Physics.Raycast(origin, target - origin, Vector3.Distance(target, origin), obstacleLayer);
		}

		public RaycastHit[] RaycastTrulyAll(Vector3 initialXZCoordToCheck, Vector3 direction, LayerMask layerMask, float offsetAfterHit)
		{
			List<RaycastHit> raycastHits = new();
			Vector3 thisRayOrigin = initialXZCoordToCheck;
			// If something is hit within the max length
			float maxLength = player.IsGrounded ? actualRayLengthFromGround : actualRayLengthFromGroundUngrounded;
			while (Physics.Raycast(thisRayOrigin, direction, out RaycastHit hit, maxLength, layerMask) && (initialXZCoordToCheck - hit.point).magnitude < maxLength)
			{
				raycastHits.Add(hit);
				thisRayOrigin = hit.point + direction * offsetAfterHit;
			}
			return raycastHits.ToArray();
		}
	}

	public struct ValidHit
	{
		public Vector3 destination;
		public Quaternion rotation;
		public float heightSpace;
		public ValidHit(Vector3 dest, Quaternion rot, float space)
		{
			destination = dest;
			rotation = rot;
			heightSpace = space;
		}
	}
}