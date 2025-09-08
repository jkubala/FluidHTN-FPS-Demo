using FPSDemo.NPC.Utilities;
using UnityEngine;

public class PositionValidatorDebug : PositionValidator
{
    protected override Vector3? VerifyContinuousCoverOfCorner(Vector3 position, CornerDetectionInfo cornerInfo, float maxDistanceToAnalyse, float minimumWidth, TacticalPositionSettings posSettings, LayerMask raycastMask)
    {
        if (cornerInfo.debugData != null)
        {
            cornerInfo.debugData.verifyHorizStartPos = position;
            cornerInfo.debugData.verifyHorizEndPos = position - cornerInfo.outDirection.normalized * maxDistanceToAnalyse;
        }
        return base.VerifyContinuousCoverOfCorner(position, cornerInfo, maxDistanceToAnalyse, minimumWidth, posSettings, raycastMask);
    }

    protected override Vector3 CalculateSphereCastOrigin(CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings)
    {
        Vector3 sphereCastOrigin = base.CalculateSphereCastOrigin(cornerInfo, cornerSettings);
        if (cornerInfo.debugData != null)
        {
            cornerInfo.debugData.sphereCastOrigin = sphereCastOrigin;
            cornerInfo.debugData.sphereCastDirection = cornerInfo.positionFiringDirection;
            cornerInfo.debugData.sphereCastRadius = cornerSettings.sphereCastForFiringPositionCheckRadius;
        }
        return sphereCastOrigin;
    }

    protected override Vector3 GetVectorDownAlongWall(CornerDetectionInfo cornerInfo)
    {
        Vector3 downAlongWall = base.GetVectorDownAlongWall(cornerInfo);
        if (cornerInfo.debugData != null)
        {
            cornerInfo.debugData.yAxisStandSphereCastOrigin = cornerInfo.position;
            cornerInfo.debugData.yAxisStandSphereCastDirection = downAlongWall;
        }
        return downAlongWall;
    }

    protected override Vector3? FindGroundLevelAtCorner(CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask)
    {
        Vector3? standardizedPos = base.FindGroundLevelAtCorner(cornerInfo, cornerSettings, raycastMask);
        if (cornerInfo.debugData != null)
        {
            cornerInfo.debugData.yAxisStandSphereCastRadius = cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer;
        }
        return standardizedPos;
    }

    protected override Vector3? ValidateAndAdjustHeight(Vector3 pos, CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings)
    {
        if (cornerInfo.debugData != null)
        {
            cornerInfo.debugData.yAxisStandSphereCastHit = pos;
        }
        return base.ValidateAndAdjustHeight(pos, cornerInfo, cornerSettings);
    }

    protected override Vector3? FindHoleInCoverVertically(Vector3 bottomStart, CornerDetectionInfo cornerInfo, LayerMask raycastMask, TacticalPositionSettings posSettings)
    {
        Vector3? holePos = base.FindHoleInCoverVertically(bottomStart, cornerInfo, raycastMask, posSettings);
        if (cornerInfo.debugData != null && holePos.HasValue)
        {
            cornerInfo.debugData.verifyFailurePos = holePos.Value;
        }
        return holePos;
    }

}
