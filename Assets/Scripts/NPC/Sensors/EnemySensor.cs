using FluidHTN;
using UnityEngine;
using FPSDemo.Core;
using FPSDemo.Player;
using FPSDemo.Target;

namespace FPSDemo.NPC.Sensors
{
    [RequireComponent(typeof(NPC))]
    public class EnemySensor : MonoBehaviour, ISensor
    {
        // ========================================================= INSPECTOR FIELDS
        // TODO: Add tooltips to all serialized fields

        [SerializeField] private NPC _npc;


        // ========================================================= PRIVATE FIELDS



        // ========================================================= PROPERTIES

        public float TickRate => Game.AISettings != null ? Game.AISettings.EnemySensorTickRate : 0f;
        public float NextTickTime { get; set; }


        // ========================================================= UNITY METHODS

        private void OnValidate()
        {
            if (_npc == null)
            {
                _npc = GetComponent<NPC>();
            }
        }

        private void Start()
        {
            
        }

        private void OnEnable()
        {

        }

        private void OnDisable()
        {

        }


        // ========================================================= VALIDATION METHODS



        // ========================================================= TICK

        public void Tick(AIContext context)
        {
            // Reset to default world state for enemy awareness and line of sight
            context.SetState(AIWorldState.AwareOfEnemy, false, EffectType.Permanent);
            context.SetState(AIWorldState.HasEnemyInSight, false, EffectType.Permanent);
            context.UpdateCurrentEnemy(null);

            var bestAwareness = 0.0f;

            // Update to current enemy awareness and line of sight world state
            foreach (var kvp in context.EnemiesSpecificData)
            {
                var currentTarget = kvp.Key;
                var currentTargetData = kvp.Value;

                // If we are above our alert awareness threshold
                if (currentTargetData.awarenessOfThisTarget >= context.AlertAwarenessThreshold)
                {
                    // And we are currently not aware of enemy
                    if (context.HasState(AIWorldState.AwareOfEnemy, false))
                    {
                        // Set a permanent change to our "here and now" world state, we are aware of an enemy!
                        context.SetState(AIWorldState.AwareOfEnemy, true, EffectType.Permanent);

                        // If we currently don't have an enemy in sight, but this enemy is visible to us
                        if (context.HasState(AIWorldState.HasEnemyInSight, false) && 
                            currentTargetData.visibleBodyParts.Count > 0)
                        {
                            // Set a permanent change to our "here and now" world state, we have an enemy in sight!!!
                            context.SetState(AIWorldState.HasEnemyInSight, true, EffectType.Permanent);
                        }
                    }

                    // Set best awareness score as current enemy target
                    if (currentTargetData.awarenessOfThisTarget > bestAwareness)
                    {
                        bestAwareness = currentTargetData.awarenessOfThisTarget;
                        context.UpdateCurrentEnemy(currentTarget, currentTargetData);
                    }
                }

                if (context.CurrentEnemy != null)
                {
                    var distance = Vector3.Distance(context.CurrentEnemy.transform.position, context.ThisNPC.transform.position);
                    Debug.Log($"Current Enemy: {context.CurrentEnemy.gameObject.name} at distance: {distance}");
                }
            }
        }


        // ========================================================= ON DEATH



        // ========================================================= GETTERS



        // ========================================================= CALCULATIONS



        // ========================================================= EDITOR / DEBUG


    }
}