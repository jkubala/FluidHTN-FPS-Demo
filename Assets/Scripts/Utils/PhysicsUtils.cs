using System.Collections.Generic;
using UnityEngine;

namespace FPSDemo.Utils
{
    public static class PhysicsUtils
    {
        public static RaycastHit[] RaycastTrulyAll(Vector3 initialXZCoordToCheck, Vector3 direction, LayerMask layerMask, float offsetAfterHit, float maxLength)
        {
            List<RaycastHit> raycasts = new();

            Vector3 thisRayOrigin = initialXZCoordToCheck;

            // TODO: Ensure this can't go infinite loop. I'd prefer a maxIteration failsafe mechanic here just to make sure it can't.
            while (Physics.Raycast(thisRayOrigin, direction, out var hit, maxLength, layerMask) && (initialXZCoordToCheck - hit.point).magnitude < maxLength)
            {
                raycasts.Add(hit);
                thisRayOrigin = hit.point + direction * offsetAfterHit;
            }

            return raycasts.ToArray();
        }
    }
}