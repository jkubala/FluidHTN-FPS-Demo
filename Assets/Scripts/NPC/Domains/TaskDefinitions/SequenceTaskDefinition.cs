using FPSDemo.NPC.Domains.ConditionDefinitions;
using System.Collections.Generic;
using UnityEngine;

namespace FPSDemo.NPC.Domains.TaskDefinitions
{
    [CreateAssetMenu(fileName = "New AISequenceTask", menuName = "FPSDemo/AI/SequenceTask")]
    public class SequenceTaskDefinition : AICompoundTaskDefinition
    {
        public string TaskName;
        public List<AICompoundTaskDefinition> SequenceOfTasks;
        public List<HasIntState> Conditions;

        public override void Add(AIDomainBuilder builder)
        {
            builder.Sequence(TaskName);
            {
                foreach (var condition in Conditions)
                {
                    builder.HasState(condition.State, (byte)condition.Value);
                }

                foreach (var task in SequenceOfTasks)
                {
                    task.Add(builder);
                }
            }
            builder.End();
        }
    }
}