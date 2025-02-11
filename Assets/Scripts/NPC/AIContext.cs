using UnityEngine;
using System.Collections.Generic;
using FluidHTN.Contexts;
using FPSDemo.Target;

namespace FPSDemo.NPC
{
    public partial class AIContext : BaseContext
    {
        public Dictionary<HumanTarget, TargetData> enemiesSpecificData = new();
        public Dictionary<HumanTarget, TargetData> alliesSpecificData = new();

        private NPCSettings _settings;

        public float AlertAwarenessThreshold => _settings != null ? _settings.AlertAwarenessThreshold : 2f;
        public float AwarenessDeterioration => _settings != null ? _settings.AwarenessDeterioration : 0.1f;

        public NPC ThisNPC { get; }
        public HumanTarget ThisTarget { get; }
        public HumanTarget CurrentEnemy { get; private set; }
        public TargetData CurrentEnemyData { get; private set; }

        public AIContext(NPC npc, HumanTarget aTarget)
        {
            ThisNPC = npc;
            ThisTarget = aTarget;
        }

        public void Init(NPCSettings settings)
        {
            _settings = settings;
            AddTargets();

            Init();
        }

        public void UpdateCurrentEnemy(HumanTarget target)
        {
            CurrentEnemy = target;
            if (CurrentEnemy != null)
            {
                if (enemiesSpecificData.TryGetValue(CurrentEnemy, out var data))
                {
                    CurrentEnemyData = data;
                }
                else
                {
                    CurrentEnemyData = null;
                }
            }
            else
            {
                CurrentEnemyData = null;
            }
        }

        public void UpdateCurrentEnemy(HumanTarget target, TargetData targetData)
        {
            CurrentEnemy = target;
            if (CurrentEnemy != null)
            {
                CurrentEnemyData = targetData;
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
                        alliesSpecificData.Add(target, new());
                    }
                    else
                    {
                        enemiesSpecificData.Add(target, new());
                    }
                }
            }
        }

        public void SetAwarenessOfThisEnemy(HumanTarget target, float newAwareness)
        {
            if (enemiesSpecificData.TryGetValue(target, out var currentTargetData))
            {
                // Increase awareness of the target
                if (currentTargetData.awarenessOfThisTarget < newAwareness)
                {
                    currentTargetData.awarenessOfThisTarget = Mathf.Min(AlertAwarenessThreshold, newAwareness);
                }
            }
        }

        public void AwarenessDecrease(TargetData targetData)
        {
            targetData.awarenessOfThisTarget = Mathf.Max(0f,
                targetData.awarenessOfThisTarget - AwarenessDeterioration * Time.deltaTime);
        }
    }
}
