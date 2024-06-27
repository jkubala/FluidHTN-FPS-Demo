using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
	public float gunShotDistanceToAlert;
	[SerializeField] Transform muzzleFlashPos;
	public Transform otherHandTransform;
	public Transform AimTransform;
	[SerializeField] AudioSource weaponAudioSource;
	[SerializeField] LayerMask shotLayerMask;

	public WeaponCollisionDetector weaponCollisionDetector;

	public bool isAutomatic = false;
	public int currentAvailableAmmo = 0;
	public int maxAmmoInMagazine = 5;
	public float fireRate = 3f;
	public float maxRange = 1000f;
	public float angleSpreadPerShot = 10f;
	public float defaultHipFireAngleSpread = 1f;
	public float maxAngleSpreadWhenShooting = 10f;
	public float maxAngleSpreadWhenMoving = 10f;
	public AnimationCurve spreadStabilityGain;
	[SerializeField] AudioClip shotSFX;

	[Header("Positioning")]
	public Vector3 normalPos;
	public Vector3 normalRot;
	[Space(10)]
	public Vector3 ADSPos;
	public Vector3 ADSRot;
	[Space(10)]
	public Vector3 runPos;
	public Vector3 runRot;
	[Space(10)]
	public Vector3 awayPos;
	public Vector3 awayRot;

	[Header("Recoil")]
	public Vector3 recoil;
	public Vector3 adsRecoil;
	public float blowbackForce;
	public float recoilRecoverSpeed;

	void Awake()
	{
		currentAvailableAmmo = maxAmmoInMagazine;
	}

	public void Fire(HumanTarget target, Transform bulletStart)
	{
		currentAvailableAmmo--;
		if (shotSFX != null)
		{
			weaponAudioSource.PlayOneShot(shotSFX);
		}
		// Instantiate muzzle flash
		// flash.transform.position = muzzleFlashPos.position;
		// flash.transform.rotation = muzzleFlashPos.rotation;
		// flash.SetActive(true);
		// Fire some raycast
		if (Physics.Raycast(bulletStart.position, bulletStart.forward, out RaycastHit hit, maxRange, shotLayerMask))
		{
			Debug.Log("Gameobject hit: " + hit.collider.name + " at position: " + hit.point);
		}
	}
}
