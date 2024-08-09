using System;
using UnityEngine;

namespace FPSDemo.Weapons
{
    public class WeaponCollisionDetector : MonoBehaviour
    {
        public Action CollisionEntered;
        public Action CollisionExited;
        int nOfCollisions = 0;

        private void OnTriggerEnter(Collider other)
        {
            CollisionEntered?.Invoke();
            nOfCollisions++;
        }

        private void OnTriggerExit(Collider other)
        {
            nOfCollisions--;
            if (nOfCollisions <= 0)
            {
                CollisionExited?.Invoke();
            }
        }
    }
}
