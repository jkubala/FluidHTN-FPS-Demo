using System.Collections.Generic;
using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
    [CreateAssetMenu(fileName = "TacticalGeneratorDebugSettings", menuName = "FPSDemo/TacticalGrid/TacticalGeneratorDebugProfile")]
    public class TacticalGeneratorDebugProfile : ScriptableObject
    {
        [SerializeField] List<CoverGenerationContext> generationContexts;
        public TacticalGridGenerationSettings gridSettings;
        public TacticalPositionSettings positionSettings;
        public TacticalGridSpawnerData gridSpawnerData;
        public LayerMask raycastMask = 1 << 0;


        public List<CoverGenerationContext> GetContextsFor(TacticalPositionGenerator.CoverGenerationMode genMode)
        {
            List<CoverGenerationContext> contexts = new();
            if (genMode == TacticalPositionGenerator.CoverGenerationMode.all)
            {
                contexts.AddRange(generationContexts);
            }
            else
            {
                foreach (var ctx in generationContexts)
                {
                    if (ctx.genMode == genMode)
                    {
                        contexts.Add(ctx);
                        break;
                    }
                }
            }
            return contexts;
        }
    }
}
