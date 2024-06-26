using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class HumanTarget : MonoBehaviour
{
	public List<VisibleBodyPart> bodyPartsToRaycast = new();
	public bool IsCrouching { get; set; } = false;
	public bool IsDead { get; private set; } = false;
	public float LastTimeFired { get; set; } = Mathf.NegativeInfinity;
	public enum Team { BLUFOR, OPFOR };

	public Team targetTeam;
	public bool IsPlayer { get; set; } = false;
	public Transform eyes;
	public Rigidbody mainRigidbody;
	[HideInInspector]
	public HealthSystem healthSystem;

	void Awake()
	{
		healthSystem = GetComponent<HealthSystem>();
		PopulateVisibleBodyParts();
		TargetRegister.RegisterSelf(this);
	}

	public void SetAsPlayer()
	{
		IsPlayer = true;
		mainRigidbody = GetComponent<Rigidbody>();
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
					gameObject.name + "! Modifier: " + bodyPart.visibilityModifier + " Previous modifier:" + maxModifier);
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