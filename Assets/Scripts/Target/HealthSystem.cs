using System;
using UnityEngine;

namespace FPSDemo.Target
{
    public class HealthSystem : MonoBehaviour
    {
        [SerializeField] bool godMode = false;
        [SerializeField] Transform rootModelTransform;

        public string ActionDescription
        {
            get { return "Perform takedown"; }
        }

        bool isDead = false;
        public event Action OnDeath;
        public event Action<Vector3> OnDamageTaken;
        public HumanTarget ThisTarget { get; private set; }
        CapsuleCollider characterCollider;

        void Awake()
        {
            ThisTarget = GetComponent<HumanTarget>();
            characterCollider = GetComponent<CapsuleCollider>();
        }

        public void WasShot(HumanTarget shotBy)
        {
            if (!isDead && shotBy != ThisTarget && !godMode)
            {
                // Notify about damage taken at this position
                OnDamageTaken?.Invoke(transform.position);
                
                KillThisEntity();
            }
        }

        public Transform GetRootModelTransform()
        {
            return rootModelTransform;
        }

        void KillThisEntity()
        {
            if (!ThisTarget.IsPlayer)
            {
                characterCollider.enabled = false;
            }

            isDead = true;
            OnDeath?.Invoke();
        }
    }
}
