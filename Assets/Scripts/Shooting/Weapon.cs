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

	public int currentAvailableAmmo = 0;
	public int maxAmmoInMagazine = 5;
	public float fireRate = 3f;
	public float angleSpreadPerShot = 10f;
	public float defaultHipFireAngleSpread = 1f;
	public float maxAngleSpreadWhenShooting = 10f;
	public float maxAngleSpreadWhenMoving = 10f;
	public AnimationCurve spreadStabilityGain;

	[SerializeField] AudioClip shotSFX;

	void Awake()
	{
		currentAvailableAmmo = maxAmmoInMagazine;
	}

	public void Fire(HumanTarget target, Transform bulletStart)
	{
		currentAvailableAmmo--;
		weaponAudioSource.PlayOneShot(shotSFX);
		// Instantiate muzzle flash
		// flash.transform.position = muzzleFlashPos.position;
		// flash.transform.rotation = muzzleFlashPos.rotation;
		// flash.SetActive(true);
		// Fire some raycast
	}
}
