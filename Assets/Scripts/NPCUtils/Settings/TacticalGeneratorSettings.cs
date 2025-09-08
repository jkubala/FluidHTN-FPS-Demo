using System;
using System.Collections.Generic;
using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
    [CreateAssetMenu(fileName = "TacticalGeneratorSettings", menuName = "FPSDemo/TacticalGrid/TacticalGeneratorSettings")]
    public class TacticalGeneratorSettings : ScriptableObject
    {
        [SerializeField] List<CoverGenerationContext> generationContexts;
        [SerializeField] private TacticalPositionData manualPositionData;
        public TacticalGridGenerationSettings gridSettings;
        public TacticalGridSpawnerData gridSpawnerData;
        public TacticalPositionSettings positionSettings;
        public LayerMask raycastMask = 1 << 0;


        public List<CoverGenerationContext> GetContextsFor(TacticalPositionGenerator.CoverGenerationMode genMode)
        {
            List<CoverGenerationContext> contexts = new();
            if (genMode == TacticalPositionGenerator.CoverGenerationMode.manual)
            {
                return contexts;
            }

            if (genMode == TacticalPositionGenerator.CoverGenerationMode.all)
            {
                contexts.AddRange(generationContexts);
            }
            else
            {
                foreach (var ctx in generationContexts)
                {
                    if (ctx.cornerSettings.genMode == genMode)
                    {
                        contexts.Add(ctx);
                        break;
                    }
                }
            }
            return contexts;
        }

        public TacticalPositionData GetManualPositionData()
        {
            return manualPositionData;
        }
    }

    [Serializable]
    public class CoverGenerationContext
    {
        public TacticalPositionData positionData;
        public TacticalPositionScanSettings cornerSettings;
    }
}
