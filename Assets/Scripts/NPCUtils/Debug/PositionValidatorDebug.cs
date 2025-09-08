using FPSDemo.NPC.Utilities;
using UnityEngine;

public class PositionValidatorDebug : PositionValidator
{
    protected override Vector3? ValidateCoverContinuity(Vector3 position, CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings, TacticalPositionSettings posSettings, LayerMask raycastMask)
    {
        if (cornerInfo.debugData != null)
        {
            cornerInfo.debugData.verifyHorizStartPos = position;
            cornerInfo.debugData.verifyHorizEndPos = position - cornerInfo.outDirection.normalized * cornerSettings.maxCoverAnalysisDistance;
        }
        return base.ValidateCoverContinuity(position, cornerInfo, cornerSettings, posSettings, raycastMask);
    }

    protected override Vector3 GetFiringCheckOrigin(CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings)
    {
        Vector3 sphereCastOrigin = base.GetFiringCheckOrigin(cornerInfo, cornerSettings);
        if (cornerInfo.debugData != null)
        {
            cornerInfo.debugData.sphereCastOrigin = sphereCastOrigin;
            cornerInfo.debugData.sphereCastDirection = cornerInfo.positionFiringDirection;
            cornerInfo.debugData.sphereCastRadius = cornerSettings.sphereCastForFiringPositionCheckRadius;
        }
        return sphereCastOrigin;
    }

    protected override Vector3 CalculateWallAlignedDownVector(CornerDetectionInfo cornerInfo)
    {
        Vector3 downAlongWall = base.CalculateWallAlignedDownVector(cornerInfo);
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

    protected override Vector3? AdjustToStandardHeight(Vector3 pos, CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings)
    {
        if (cornerInfo.debugData != null)
        {
            cornerInfo.debugData.yAxisStandSphereCastHit = pos;
        }
        return base.AdjustToStandardHeight(pos, cornerInfo, cornerSettings);
    }

    protected override Vector3? ScanForCoverGaps(Vector3 bottomStart, CornerDetectionInfo cornerInfo, LayerMask raycastMask, TacticalPositionSettings posSettings)
    {
        Vector3? holePos = base.ScanForCoverGaps(bottomStart, cornerInfo, raycastMask, posSettings);
        if (cornerInfo.debugData != null && holePos.HasValue)
        {
            cornerInfo.debugData.verifyFailurePos = holePos.Value;
        }
        return holePos;
    }

}
