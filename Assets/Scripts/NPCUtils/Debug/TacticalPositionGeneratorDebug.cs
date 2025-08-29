using System.Collections.Generic;
using FPSDemo.NPC.Utilities;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(TacticalPositionGenerator))]
public class TacticalPositionGeneratorDebug : MonoBehaviour
{
    [SerializeField] private TacticalPositionGenerator _generator;

    [SerializeField] private GameObject _debugParentAdded;
    [SerializeField] private GameObject _debugParentRemoved;
    [SerializeField] private GameObject _debugParentModified;
    [SerializeField] private GameObject _debugGameObjectPosChangePrefab;

    [SerializeField] private GameObject _debugGizmoGOPrefab;
    [SerializeField] private GameObject _debugGizmoGOParent;
    [SerializeField] private Transform _gizmo3DCursor;

    [SerializeField] List<BasePositionClassifier> _classifiers = new();
    private List<BasePositionClassifier> Classifiers
    {
        get
        {
            if (_classifiers == null || _classifiers.Count == 0)
            {
                _classifiers = new List<BasePositionClassifier>
                    {
                        new AddedPositionClassifier(_debugParentAdded, _maxDistanceToConsiderSamePosition, _maxDegreesDifferenceToConsiderSamePosition),
                        new RemovedPositionClassifier(_debugParentRemoved, _maxDistanceToConsiderSamePosition, _maxDegreesDifferenceToConsiderSamePosition),
                        new ModifiedPositionClassifier(_debugParentModified, _maxDistanceToConsiderSamePosition, _maxDegreesDifferenceToConsiderSamePosition)
                    };
            }
            return _classifiers;
        }
    }

    [Header("Limits for difference when comparing after regeneration")]
    [Range(0.01f, 0.25f)][SerializeField] private float _maxDistanceToConsiderSamePosition = 0.05f;
    [Range(1f, 3f)][SerializeField] private float _maxDegreesDifferenceToConsiderSamePosition = 1f;

    private void OnEnable()
    {
        if (_generator != null)
        {
            _generator.OnContextUpdated += HandleContextUpdated;
            _generator.OnNewPotentialPositionCreated += HandleNewPotentialPosition;
            _generator.OnGizmoViewModeChange += GizmoViewChanged;
        }
    }

    private void OnDisable()
    {
        if (_generator != null)
        {
            _generator.OnContextUpdated -= HandleContextUpdated;
            _generator.OnNewPotentialPositionCreated -= HandleNewPotentialPosition;
            _generator.OnGizmoViewModeChange -= GizmoViewChanged;
        }
    }

    private void ComputeDifferences(List<TacticalPosition> oldPositions, List<TacticalPosition> newPositions)
    {
        if (!_generator.CreatePosChangesDebugObjects)
        {
            return;
        }
        GameObject debugGO = null;

        if (_generator.CreatePosChangesDebugObjects)
        {
            debugGO = _debugGameObjectPosChangePrefab;
        }

        foreach (var classifier in Classifiers)
        {
            classifier.Classify(oldPositions, newPositions, debugGO);
        }
    }

    private void HandleNewPotentialPosition(Vector3 position, TacticalDebugData debugData, TacticalPositionGenerator.GizmoViewMode gizmoViewMode)
    {
        if (Vector3.Distance(position, _gizmo3DCursor.position) < _generator.DistanceToCreateGizmos)
        {
            TacticalPosDebugGizmoGO debugGO = Instantiate(_debugGizmoGOPrefab, _debugGizmoGOParent.transform).GetComponent<TacticalPosDebugGizmoGO>();
            debugGO.transform.position = position;
            debugGO.TacticalDebugData = debugData;
            HandleChildVisibility(gizmoViewMode, debugGO.gameObject);
            EditorUtility.SetDirty(_debugGizmoGOParent);
        }
    }

    private void GizmoViewChanged(TacticalPositionGenerator.GizmoViewMode viewMode)
    {
        for (int i = 0; i < _debugGizmoGOParent.transform.childCount; i++)
        {
            GameObject childGO = _debugGizmoGOParent.transform.GetChild(i).gameObject;
            HandleChildVisibility(viewMode, childGO);
        }
    }

    private void HandleChildVisibility(TacticalPositionGenerator.GizmoViewMode viewMode, GameObject childGO)
    {
        if (childGO.TryGetComponent(out TacticalPosDebugGizmoGO childDebugGO))
        {
            bool shouldShow = viewMode == TacticalPositionGenerator.GizmoViewMode.all ||
                (childDebugGO.TacticalDebugData.Finished && viewMode == TacticalPositionGenerator.GizmoViewMode.finished) ||
                (!childDebugGO.TacticalDebugData.Finished && viewMode == TacticalPositionGenerator.GizmoViewMode.unfinished);

            SetChildVisibility(childGO, shouldShow);
        }
        else
        {
            Debug.LogError($"{childGO.name}, which is a child of debug gizmo gameobject parent does not have TacticalPosDebugGizmoGO script attached!");
        }
    }

    private void SetChildVisibility(GameObject childGO, bool shouldShow)
    {
#if UNITY_EDITOR
        EditorApplication.delayCall += () =>
        {
            if (childGO != null)
                childGO.SetActive(shouldShow);
        };
#else
    if (childGO != null)
        childGO.SetActive(shouldShow);
#endif
    }

    private void HandleContextUpdated(TacticalPositionData oldPositions, CoverGenerationContext context)
    {
        if (context != null)
        {
            if (context.positionData.Positions.Count == 0)
            {
                ClearDebugGizmosGOs(context);
            }
            if (oldPositions != null)
            {
                ComputeDifferences(oldPositions.Positions, context.positionData.Positions);
            }
            else
            {
                ClearAllPosChangeDebugGOs();
            }
        }
        else
        {
            Debug.LogError("Context tried to update with null new positions!");
        }

    }

    private void ClearAllPosChangeDebugGOs()
    {
        foreach (BasePositionClassifier classifier in Classifiers)
        {
            classifier.ClearDebugGOs();
        }
    }

    private void ClearDebugGizmosGOs(CoverGenerationContext context)
    {
        for (int i = _debugGizmoGOParent.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = _debugGizmoGOParent.transform.GetChild(i).gameObject;
            if (child.TryGetComponent(out TacticalPosDebugGizmoGO debugGO))
            {
                if (debugGO.TacticalDebugData.genMode == context.genMode)
                {
#if UNITY_EDITOR
                    DestroyImmediate(child);
#else
                    Destroy(child);
#endif
                }
            }
            else
            {
                Debug.LogError($"{child.name} did not have TacticalPosDebugGO script attached!");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!_generator.ShowPositions)
        {
            return;
        }

        int totalCount = 0;
        foreach (var activeContext in _generator.GetActiveCoverGenContexts())
        {
            totalCount += DisplayPositions(activeContext.positionData.Positions);
        }

        Debug.Log($"Currently displaying {totalCount} positions");
    }

    private int DisplayPositions(List<TacticalPosition> positions)
    {
        if (positions != null)
        {
            foreach (TacticalPosition position in positions)
            {
                if (position.mainCover.type == CoverType.LeftCorner)
                {
                    Gizmos.color = Color.red;
                }
                else if (position.mainCover.type == CoverType.RightCorner)
                {
                    Gizmos.color = Color.blue;
                }
                else
                {
                    Gizmos.color = Color.white;
                }

                if (position.isOutside)
                {
                    Gizmos.DrawSphere(position.Position, 0.1f);
                }
                else
                {
                    Gizmos.DrawWireSphere(position.Position, 0.1f);
                }

                Gizmos.DrawRay(position.Position, position.mainCover.rotationToAlignWithCover * Vector3.forward);
            }
            return positions.Count;
        }
        return 0;
    }
}
