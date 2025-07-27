using UnityEngine;

[CreateAssetMenu(fileName = "New position settings", menuName = "FPSDemo/TacticalPositions/PositionSettings")]
public class TacticalPositionSettings : ScriptableObject
{
    public float RequiredProximityToNavMesh = 0.2f;
    public float geometryCheckYOffset = 3f;
    public float distanceToRemoveDuplicates = 1f;
    public float minHeightToConsiderHighCover = 1.85f;
    public float minHeightToConsiderLowCover = 0.95f;
}
