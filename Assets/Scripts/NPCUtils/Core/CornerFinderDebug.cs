using System.Collections.Generic;
using FPSDemo.NPC.Utilities;
using UnityEngine;

public class CornerFinderDebug : CornerFinder
{
    public CornerFinderDebug(TacticalPositionGeneratorDebug debugGen)
    {
        _debugGen = debugGen;
    }
    private readonly TacticalPositionGeneratorDebug _debugGen;
    private TacticalDebugData _debugData;

    public override List<CornerDetectionInfo> FindCorners(RaycastHit hit, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask)
    {
        if (_debugGen.PositionGizmoInRange(hit.point))
        {
            _debugData = new()
            {
                genMode = _debugGen.CurCoverGenMode
            };
        }
        else
        {
            _debugData = null;
        }
        var result = base.FindCorners(hit, cornerSettings, raycastMask);
        _debugGen.HandleNewPotentialPosition(hit.point, _debugData);
        return result;
    }

    protected override Vector3 CalculateOffsetPosition(RaycastHit hit, TacticalPositionScanSettings cornerSettings)
    {
        Vector3 offsetPosition = base.CalculateOffsetPosition(hit, cornerSettings);
        if (_debugData != null)
        {
            _debugData.offsetPosition = offsetPosition;
        }
        return offsetPosition;
    }

    protected override CornerDetectionInfo? FindCorner(Vector3 position, Vector3 hitNormal, Vector3 axis, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask)
    {
        CornerDebugData cornerDebugData = null;
        if (_debugData != null)
        {
            cornerDebugData = new(_debugData)
            {
                hitPositions = new()
            };
            _debugData.corners.Add(cornerDebugData);
        }
        CornerDetectionInfo? cornerInfo = base.FindCorner(position, hitNormal, axis, cornerSettings, raycastMask);

        if (cornerDebugData != null && cornerInfo.HasValue)
        {
            var modifiedCornerInfo = cornerInfo.Value;
            modifiedCornerInfo.debugData = cornerDebugData;
            if (cornerInfo.Value.cornerType == CornerType.Convex)
            {
                cornerDebugData.cornerNormal = cornerInfo.Value.coverWallNormal;
                cornerDebugData.positionFiringDirection = cornerInfo.Value.positionFiringDirection;
            }
            return modifiedCornerInfo;
        }
        return cornerInfo;
    }

    protected override bool CornerBendDetected(Vector3 newProjectedNormal, float angleDifference, TacticalPositionScanSettings cornerSettings, ref int currentHitsOfDifferentNormal, ref Vector3? lastDifferentNormal, ref Vector3? lastAdjustedPosition, Vector3 adjustedPosition, RaycastHit newHit)
    {
        _debugData?.corners[^1].hitPositions.Add(newHit.point);
        return base.CornerBendDetected(newProjectedNormal, angleDifference, cornerSettings, ref currentHitsOfDifferentNormal, ref lastDifferentNormal, ref lastAdjustedPosition, adjustedPosition, newHit);
    }

}
