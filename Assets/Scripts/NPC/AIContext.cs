using UnityEngine;
using System.Collections.Generic;
using FluidHTN.Contexts;
using FPSDemo.Target;

namespace FPSDemo.NPC
{
    public partial class AIContext : BaseContext
    {
        // ========================================================= PRIVATE FIELDS

        private NPCSettings _settings;


        // ========================================================= PUBLIC PROPERTIES

        public Dictionary<HumanTarget, TargetData> EnemiesSpecificData { get; } = new();
        public Dictionary<HumanTarget, TargetData> AlliesSpecificData { get; } = new();

        public float AlertAwarenessThreshold => _settings != null ? _settings.AlertAwarenessThreshold : 2f;
        public float AwarenessDeterioration => _settings != null ? _settings.AwarenessDeterioration : 0.1f;

        public NPC ThisNPC { get; }
        public HumanTarget ThisTarget { get; }
        public ThirdPersonController ThisController => ThisNPC?.Controller;

        public HumanTarget CurrentEnemy { get; private set; }
        public TargetData CurrentEnemyData { get; private set; }


        // ========================================================= INIT

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

        void AddTargets()
        {
            foreach (HumanTarget target in TargetRegister.instance.ListOfActiveTargetsBLUFORTeam)
            {
                if (target != ThisTarget)
                {
                    if (ThisTarget.targetTeam == HumanTarget.Team.BLUFOR)
                    {
                        AlliesSpecificData.Add(target, new());
                    }
                    else
                    {
                        EnemiesSpecificData.Add(target, new());
                    }
                }
            }

            foreach (HumanTarget target in TargetRegister.instance.ListOfActiveTargetsOPFORTeam)
            {
                if (target != ThisTarget)
                {
                    if (ThisTarget.targetTeam == HumanTarget.Team.OPFOR)
                    {
                        AlliesSpecificData.Add(target, new());
                    }
                    else
                    {
                        EnemiesSpecificData.Add(target, new());
                    }
                }
            }
        }


        // ========================================================= CURRENT ENEMIES

        public void UpdateCurrentEnemy(HumanTarget target)
        {
            CurrentEnemy = target;
            if (CurrentEnemy != null)
            {
                if (EnemiesSpecificData.TryGetValue(CurrentEnemy, out var data))
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


        // ========================================================= AWARENESS

        public void SetAwarenessOfThisEnemy(HumanTarget target, float newAwareness)
        {
            if (EnemiesSpecificData.TryGetValue(target, out var currentTargetData))
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
