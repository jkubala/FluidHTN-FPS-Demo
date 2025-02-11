using System.Collections.Generic;
using FluidHTN;
using NUnit.Framework;
using UnityEngine;

namespace FPSDemo.NPC.Domains
{
    [CreateAssetMenu(fileName = "New AIDomain", menuName = "FPSDemo/AI/Domain")]
    public class AIDomainDefinition : ScriptableObject
    {
        public string DomainName;
        public List<AICompoundTaskDefinition> OrderedTasks;

        public Domain<AIContext> Create()
        {
            var domainBuilder = new AIDomainBuilder(DomainName);
            foreach (var task in OrderedTasks)
            {
                task.Add(domainBuilder);
            }
            return domainBuilder.Build();
        }
    }
}