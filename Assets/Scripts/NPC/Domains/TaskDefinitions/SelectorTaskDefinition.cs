using System.Collections.Generic;
using FPSDemo.NPC.Conditions;
using FPSDemo.NPC.Domains.ConditionDefinitions;
using UnityEngine;

namespace FPSDemo.NPC.Domains.TaskDefinitions
{
    [CreateAssetMenu(fileName = "New AISelectorTask", menuName = "FPSDemo/AI/SelectorTask")]
    public class SelectorTaskDefinition : AICompoundTaskDefinition
    {
        public string TaskName;
        public List<AICompoundTaskDefinition> SelectionOfTasks;
        public List<HasIntState> Conditions;

        public override void Add(AIDomainBuilder builder)
        {
            builder.Select(TaskName);
            {
                foreach (var condition in Conditions)
                {
                    builder.HasState(condition.State, (byte)condition.Value);
                }

                foreach (var task in SelectionOfTasks)
                {
                    task.Add(builder);
                }
            }
            builder.End();
        }
    }
}