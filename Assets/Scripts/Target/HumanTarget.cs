using System.Collections.Generic;
using UnityEngine;

namespace FPSDemo.Target
{
    // TODO: We shouldn't need to set execution order. It adds hidden execution direction from editor perspective. Let's instead take control in other ways.
    [DefaultExecutionOrder(-10), RequireComponent(typeof(HealthSystem))]
    public class HumanTarget : MonoBehaviour
    {
        public List<VisibleBodyPart> bodyPartsToRaycast = new();
        public bool IsCrouching { get; set; } = false;
        public bool IsDead { get; private set; } = false;
        public float LastTimeFired { get; set; } = Mathf.NegativeInfinity;

        public enum Team
        {
            BLUFOR,
            OPFOR
        };

        public Team targetTeam;
        public bool IsPlayer { get; set; } = false;
        public Transform eyes;
        [HideInInspector] public HealthSystem healthSystem;

        void Awake()
        {
            healthSystem = GetComponent<HealthSystem>();
            PopulateVisibleBodyParts();
            TargetRegister.RegisterSelf(this);
        }

        public void SetAsPlayer()
        {
            IsPlayer = true;
        }

        void PopulateVisibleBodyParts()
        {
            if (bodyPartsToRaycast.Count != 4)
            {
                Debug.LogError("Some body part to raycast against is wrong on " + transform.name);
                return;
            }

            float overallModifier = 0;
            float maxModifier = Mathf.Infinity;
            foreach (VisibleBodyPart bodyPart in bodyPartsToRaycast)
            {
                bodyPart.owner = this;
                overallModifier += bodyPart.visibilityModifier;
                if (bodyPart.visibilityModifier > maxModifier)
                {
                    Debug.LogError("Raycast modifiers for head, chest and legs are not in order on gameObject " +
                                   gameObject.name + "! Modifier: " + bodyPart.visibilityModifier +
                                   " Previous modifier:" + maxModifier);
                }

                maxModifier = bodyPart.visibilityModifier;
            }

            if (overallModifier != 100)
            {
                Debug.LogError("Raycast modifiers for head, chest and legs are not making up to 100!");
            }
        }

        public void OnDeath()
        {
            IsDead = true;
            TargetRegister.UnregisterSelf(this);
        }

        void OnEnable()
        {
            healthSystem.OnDeath += OnDeath;
        }

        void OnDisable()
        {
            healthSystem.OnDeath -= OnDeath;
        }
    }
}