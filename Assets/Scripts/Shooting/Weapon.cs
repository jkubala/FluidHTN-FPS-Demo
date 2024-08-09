using System.Collections;
using FPSDemo.Target;
using FPSDemo.Utils;
using UnityEngine;

namespace FPSDemo.Weapons
{
    public class Weapon : MonoBehaviour
    {
        public float gunShotDistanceToAlert;
        [SerializeField] Transform muzzleFlashPos;
        [SerializeField] float muzzleFlashDuration = 0.15f;
        [SerializeField] AudioSource weaponAudioSource;

        public WeaponCollisionDetector weaponCollisionDetector;

        public bool isAutomatic = false;
        public int currentAvailableAmmo = 0;
        [Header("Reload & Ammo")] public int maxAmmoInMagazine = 30;
        [SerializeField] public int startMagazineCount = 3;
        [SerializeField] public float reloadTime = 3.0f;

        public float fireRate = 3f;
        public float maxRange = 1000f;
        public float angleSpreadPerShot = 10f;
        public float angleSpreadPerShotADS = 3f;
        public float defaultHipFireAngleSpread = 1f;
        public float maxAngleSpreadWhenShooting = 10f;
        public float maxAngleSpreadWhenShootingADS = 3f;
        public float maxAngleSpreadWhenMoving = 10f;
        public AnimationCurve spreadStabilityGain;
        [SerializeField] AudioClip[] shotSFX;
        [SerializeField] GameObjectPooler muzzleFlashVFX;
        [SerializeField] GameObjectPooler bloodHitPooler;
        [SerializeField] GameObjectPooler bulletHoleVFX;

        [Header("Positioning")] public Vector3 normalPos;
        public Vector3 normalRot;
        [Space(10)] public Vector3 ADSPos;
        public Vector3 ADSRot;
        [Space(10)] public Vector3 runPos;
        public Vector3 runRot;
        [Space(10)] public Vector3 awayPos;
        public Vector3 awayRot;

        [Header("Recoil")] public Vector3 recoil;
        public Vector3 adsRecoil;
        public float blowbackForce;
        public float recoilRecoverSpeed;

        void Awake()
        {
            currentAvailableAmmo = maxAmmoInMagazine;
        }

        public void Fire(HumanTarget target, Transform bulletStart, LayerMask shotLayerMask, int ragdollBodyLayerIndex)
        {
            currentAvailableAmmo--;
            weaponAudioSource.PlayOneShot(shotSFX[Random.Range(0, shotSFX.Length)]);
            MakeMuzzleFlash();

            if (Physics.Raycast(bulletStart.position, bulletStart.forward, out RaycastHit hit, maxRange, shotLayerMask))
            {
                MakeImpactVFX(hit);
            }
        }

        void MakeMuzzleFlash()
        {
            GameObject flash = muzzleFlashVFX.GetPooledGO();
            StartCoroutine(MaintainMuzzleFlashPositionAndRotation(flash));
        }

        IEnumerator MaintainMuzzleFlashPositionAndRotation(GameObject flash)
        {
            float timeLeft = muzzleFlashDuration;
            while (timeLeft > 0)
            {
                timeLeft -= Time.deltaTime;
                flash.transform.position = muzzleFlashPos.position;
                flash.transform.rotation = muzzleFlashPos.rotation;
                yield return null;
            }

            yield return new WaitForEndOfFrame(); // TODO needed?
            flash.SetActive(false);
        }

        void MakeImpactVFX(RaycastHit hit)
        {
            GameObject impactVFXGO;
            switch (hit.transform.gameObject.layer)
            {
                case LayerManager.ragdollBodyLayer:
                    impactVFXGO = bloodHitPooler.GetPooledGO();
                    break;
                default:
                    impactVFXGO = bulletHoleVFX.GetPooledGO();
                    break;
            }

            impactVFXGO.transform.position = hit.point;
            impactVFXGO.transform.rotation = Quaternion.LookRotation(hit.normal);
        }
    }
}
