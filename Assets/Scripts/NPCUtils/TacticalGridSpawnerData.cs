using System.Collections.Generic;
using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
    [CreateAssetMenu(fileName = "TacticalGridSpawnerData", menuName = "FPSDemo/TacticalGrid/SpawnerData")]
    public class TacticalGridSpawnerData : ScriptableObject
    {
        public List<Vector3> Positions;
    }
}