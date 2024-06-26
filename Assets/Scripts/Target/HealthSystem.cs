using System;
using UnityEngine;

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
			KillThisEntity();
		}
	}

	public Transform GetRootModelTransform()
	{
		return rootModelTransform;
	}

	void KillThisEntity()
	{
		if(!ThisTarget.IsPlayer)
		{
			characterCollider.enabled = false;
		}
		isDead = true;
		OnDeath?.Invoke();
	}
}
