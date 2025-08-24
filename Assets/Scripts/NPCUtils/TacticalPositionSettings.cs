using UnityEngine;

[CreateAssetMenu(fileName = "New position settings", menuName = "FPSDemo/TacticalPositions/PositionSettings")]
public class TacticalPositionSettings : ScriptableObject
{
    public float RequiredProximityToNavMesh = 0.2f;
    public float geometryCheckYOffset = 3f;
    public float distanceToRemoveDuplicates = 1f;
    [Range(0f, 360f)]
    public float maxAngleDifferenceToRemoveDuplicates = 20f;

    [Range(0.05f, 0.2f)]
    public float verticalStepToCheckForCover = 0.1f;
    public float bottomRaycastBuffer = 0.1f; // Added to the ground so that it will not raycast through it
    [Range(0.5f, 2f)]
    public float distanceToCheckForCover = 1f;
}
