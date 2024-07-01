using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AIContext
{
	public class TargetData
	{
		public float awarenessOfThisTarget = 0;
		public List<VisibleBodyPart> visibleBodyParts = new();
	}

	public Dictionary<HumanTarget, TargetData> enemiesSpecificData = new();
	public Dictionary<HumanTarget, TargetData> alliesSpecificData = new();

	public float alertAwarenessThreshold = 2f;
	float awarenessDeterioration = 0.1f;

	public HumanTarget ThisTarget { get; }
	public HumanTarget CurrentEnemy { get; private set; }
	public TargetData CurrentEnemyData { get; private set; }

	public AIContext(HumanTarget aTarget)
	{
		ThisTarget = aTarget;
	}

	public void Init()
	{
		AddTargets();
	}

	public void UpdateCurrentEnemy(HumanTarget target)
	{
		CurrentEnemy = target;
		if (CurrentEnemy != null)
		{
			CurrentEnemyData = enemiesSpecificData[CurrentEnemy];
		}
		else
		{
			CurrentEnemyData = null;
		}
	}

	void AddTargets()
	{
		foreach (HumanTarget target in TargetRegister.instance.ListOfActiveTargetsBLUFORTeam)
		{
			if (target != ThisTarget)
			{
				if (ThisTarget.targetTeam == HumanTarget.Team.BLUFOR)
				{
					alliesSpecificData.Add(target, new());
				}
				else
				{
					enemiesSpecificData.Add(target, new());
				}
			}
		}
		foreach (HumanTarget target in TargetRegister.instance.ListOfActiveTargetsOPFORTeam)
		{
			if (target != ThisTarget)
			{
				if (ThisTarget.targetTeam == HumanTarget.Team.OPFOR)
				{
					Debug.Log("ADDED SOMEONE");
					alliesSpecificData.Add(target, new());
				}
				else
				{
					Debug.Log("ADDED SOMEONE");
					enemiesSpecificData.Add(target, new());
				}
			}
		}
	}

	public void SetAwarenessOfThisEnemy(HumanTarget target, float newAwareness)
	{
		TargetData currentTargetData;
		currentTargetData = enemiesSpecificData[target];

		// Increase awareness of the target
		if (currentTargetData.awarenessOfThisTarget < newAwareness)
		{
			currentTargetData.awarenessOfThisTarget = Mathf.Min(alertAwarenessThreshold, newAwareness);
		}
	}

	public void AwarenessDecrease(TargetData targetData)
	{
		targetData.awarenessOfThisTarget = Mathf.Max(0f, targetData.awarenessOfThisTarget - awarenessDeterioration * Time.deltaTime);
	}
}