using System;
using System.Collections.Generic;
using UnityEngine;

namespace FPSDemo.Target
{
    // TODO: We shouldn't need to set execution order. It adds hidden execution direction from editor perspective. Let's instead take control in other ways.
    [DefaultExecutionOrder(-500)]
    public class TargetRegister : MonoBehaviour
    {
        public static TargetRegister instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public List<HumanTarget> ListOfActiveTargetsBLUFORTeam { get; private set; } = new();
        public List<HumanTarget> ListOfActiveTargetsOPFORTeam { get; private set; } = new();
        public static event Action<HumanTarget> onTargetDeath;

        public static void RegisterSelf(HumanTarget target)
        {

            if (target.targetTeam == HumanTarget.Team.BLUFOR)
            {
                instance.ListOfActiveTargetsBLUFORTeam.Add(target);
            }
            else
            {
                instance.ListOfActiveTargetsOPFORTeam.Add(target);
            }
        }

        public static void UnregisterSelf(HumanTarget target)
        {
            if (target.targetTeam == HumanTarget.Team.BLUFOR)
            {
                instance.ListOfActiveTargetsBLUFORTeam.Remove(target);
            }
            else
            {
                instance.ListOfActiveTargetsOPFORTeam.Remove(target);
            }

            onTargetDeath(target);
        }
    }
}
