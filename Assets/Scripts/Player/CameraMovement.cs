using System.Collections;
using UnityEngine;

namespace FPSDemo.FPSController
{
	public class CameraMovement : MonoBehaviour
	{
		public bool CanRotateCamera { get; set; } = true;
		[SerializeField] float standingCamHeight = 1.65f;
		[SerializeField] float targetCamYPos = 1.65f;
		[Tooltip("Mouse look sensitivity/camera move speed.")]
		[SerializeField] float mouseSensitivity = 0.2f;
		[Tooltip("Mouse look sensitivity/camera move speed in ADS.")]
		[SerializeField] float mouseSensitivityADS = 0.1f;
		[Tooltip("Maximum pitch of camera for mouselook.")]
		[SerializeField] float defaultYAngleLimit = 89f;
		[Tooltip("Maximum yaw of camera for mouselook.")]
		[SerializeField] float defaultXAngleLimit = 360f;
		[Tooltip("Smooth speed of camera angles for mouse look.")]
		[SerializeField] float smoothSpeed = 50f;
		[SerializeField] float lookTowardsSpeed = 50f;
		[SerializeField] float camLeanMoveTime = 0.075f;
		[SerializeField] float camDampSpeed = 0.1f;
		[SerializeField] float tiltHeadForward = 0.1f;
		[SerializeField] float tiltHeadBack = -0.15f;

		private Coroutine rotateTowardsRoutine = null;
		public bool RotateTowardsFinished { get; private set; } = true;
		float currentMaxXAngle = 360f;
		float rotationX = 0f, rotationY = 0f, rotationZ = 0f;
		Quaternion originalRotation;
		new Camera camera;
		[SerializeField] float cameraDefaultFOV = 65f;
		public float NormalFOV { get { return cameraDefaultFOV; } private set { cameraDefaultFOV = value; } }
		public Transform CameraBase { get; private set; }
		Transform cameraOffsetPoint;
		Vector3 currentCamXZPos;
		Vector3 camXVelocity;
		float currentCamYPos;
		float camYVelocity;
		float targetFOV;
		float targetMouseSensitivity;

		public Vector2 cameraMovementThisFrame { get; private set; } = Vector2.zero;
		Vector2 cameraMovementLastFrame = Vector2.zero;
		public float CurrentFOV { get; set; } = 45f;

		Player player;
		PlayerLeaning playerLeaning;

		void Awake()
		{
			player = GetComponentInParent<Player>();
			playerLeaning = GetComponentInParent<PlayerLeaning>();
			camera = GetComponentInChildren<Camera>();
			CameraBase = camera.transform.parent;
			cameraOffsetPoint = CameraBase.transform.parent;
			originalRotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0f);
			targetCamYPos = standingCamHeight;
			currentCamXZPos = transform.position;
			CurrentFOV = NormalFOV;
			camera.fieldOfView = CurrentFOV;
			targetMouseSensitivity = mouseSensitivity;
			currentCamYPos = targetCamYPos + player.transform.position.y;
		}

		void Start()
		{
			transform.SetParent(null);
			transform.position = new Vector3(transform.position.x, currentCamYPos, transform.position.z);
		}

		void OnEnable()
		{
			player.OnBeforeMove += UpdateCameraPosition;
			player.OnAfterMove += UpdateCamera;
			player.OnTeleport += TeleportYCamPosUpdate;
			player.OnTeleportRotate += UpdateRotation;
		}
		void OnDisable()
		{
			player.OnBeforeMove -= UpdateCameraPosition;
			player.OnAfterMove -= UpdateCamera;
			player.OnTeleport -= TeleportYCamPosUpdate;
			player.OnTeleportRotate -= UpdateRotation;
		}

		public void UpdateCamera()
		{
			if (CanRotateCamera)
			{
				RotateCamera();
			}
			MoveCameraLean();
		}

		void UpdateCameraPosition()
		{
			if(player.ThisTarget.IsDead)
			{
				// Set it to the middle of the collider
				targetCamYPos = player.CurrentFloatingColliderHeight / 2 + player.transform.position.y;
			}
			else
			{
			targetCamYPos = standingCamHeight - (player.standingFloatingColliderHeight - player.CurrentFloatingColliderHeight) + player.transform.position.y;
			player.rigidbody.MoveRotation(Quaternion.Euler(Vector3.up * transform.eulerAngles.y));
			}
		}

		void RotateCamera()
		{
			targetMouseSensitivity = player.IsAiming ? mouseSensitivityADS : mouseSensitivity;
			rotationX += player.inputManager.GetLookInput().x * targetMouseSensitivity;
			rotationY -= player.inputManager.GetLookInput().y * targetMouseSensitivity;

			rotationX = Mathf.Clamp(rotationX %= 360f, -currentMaxXAngle, currentMaxXAngle);
			rotationY = Mathf.Clamp(rotationY %= 360f, -defaultYAngleLimit, defaultYAngleLimit);
			rotationZ = -playerLeaning.LeanPos * playerLeaning.rotationLeanAmt;
			cameraMovementThisFrame = new Vector2(rotationX - cameraMovementLastFrame.x, rotationY - cameraMovementLastFrame.y);
			transform.rotation = Quaternion.Lerp(transform.rotation, originalRotation * Quaternion.AngleAxis(rotationX, Vector3.up), smoothSpeed * Time.deltaTime);
			CameraBase.transform.rotation = Quaternion.Lerp(CameraBase.transform.rotation, transform.rotation * Quaternion.AngleAxis(rotationY, Vector3.right) * Quaternion.AngleAxis(rotationZ, Vector3.forward), smoothSpeed * Time.deltaTime);
			cameraMovementLastFrame.x = rotationX;
			cameraMovementLastFrame.y = rotationY;
		}

		void MoveCameraLean()
		{
			Vector3 targetPos = player.transform.position;
			currentCamXZPos = Vector3.SmoothDamp(currentCamXZPos, targetPos, ref camXVelocity, camLeanMoveTime);
			currentCamYPos = Mathf.SmoothDamp(currentCamYPos, targetCamYPos, ref camYVelocity, camDampSpeed);
			transform.position = new Vector3(currentCamXZPos.x, currentCamYPos, currentCamXZPos.z);
			float headTiltValue = Vector3.Dot(-transform.up, camera.transform.forward);
			Vector3 headTilt = transform.forward * Mathf.Lerp(tiltHeadBack, tiltHeadForward, (headTiltValue + 1f) / 2f);
			CameraBase.transform.position = cameraOffsetPoint.position + player.transform.right * playerLeaning.LeanPos + headTilt;
		}

		public void ResetXAngle()
		{
			currentMaxXAngle = defaultXAngleLimit;
		}

		public void ClampXAngle(float angle)
		{
			currentMaxXAngle = angle;
		}

		public float GetCameraPitch()
		{
			float angleToReturn = CameraBase.localRotation.eulerAngles.x;
			if (angleToReturn > 180)
			{
				angleToReturn -= 360;
			}
			return angleToReturn;
		}

		public void StartRotatingCameraTowards(Quaternion targetRotation, float targetCameraPitch)
		{
			if (rotateTowardsRoutine != null)
			{
				StopCoroutine(rotateTowardsRoutine);
			}
			CanRotateCamera = false;
			RotateTowardsFinished = false;
			Vector3 modifiedRotation = targetRotation.eulerAngles;
			modifiedRotation.x = 0;
			rotateTowardsRoutine = StartCoroutine(RotateCameraTowards(Quaternion.Euler(modifiedRotation), targetCameraPitch));
		}

		IEnumerator RotateCameraTowards(Quaternion targetRot, float targetCameraPitch)
		{
			while (targetRot != transform.rotation || !Mathf.Approximately(GetCameraPitch(), targetCameraPitch))
			{
				CameraBase.localRotation = Quaternion.Euler(new Vector3(Mathf.MoveTowards(GetCameraPitch(), targetCameraPitch, Time.deltaTime * lookTowardsSpeed), 0, 0));
				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, Time.deltaTime * lookTowardsSpeed);
				yield return null;
			}
			originalRotation = targetRot;
			UpdateRotation(transform.rotation);
			UpdateCameraPitch(GetCameraPitch());
			cameraMovementLastFrame = new Vector2(rotationX, rotationY);
			RotateTowardsFinished = true;
		}

		void UpdateRotation(Quaternion newRotation)
		{
			originalRotation = newRotation;
			rotationX = newRotation.x;
			rotationZ = newRotation.z;
		}

		void TeleportYCamPosUpdate(float newPosition)
		{
			currentCamYPos = standingCamHeight + newPosition;
			targetCamYPos = standingCamHeight - (player.standingFloatingColliderHeight - player.CurrentFloatingColliderHeight) + newPosition;
			camYVelocity = 0f;
			UpdateCameraPitch(0);
			currentCamXZPos = player.transform.position;
			transform.position = new Vector3(currentCamXZPos.x, currentCamYPos, currentCamXZPos.z);
		}

		void UpdateCameraPitch(float angle)
		{
			rotationY = angle;
		}
	}
}
