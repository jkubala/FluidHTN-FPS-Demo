using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
    [RequireComponent(typeof(SphereCollider))]
    public class TacticalProbe : MonoBehaviour
    {
        [SerializeField] private SphereCollider _collider;

        private void OnValidate()
        {
            if (_collider == null)
            {
                _collider = GetComponent<SphereCollider>();
            }
        }
    }
}