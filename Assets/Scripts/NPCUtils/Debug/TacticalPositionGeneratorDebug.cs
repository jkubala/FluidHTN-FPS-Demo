using System;
using System.Collections.Generic;
using FPSDemo.NPC.Utilities;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class TacticalPositionGeneratorDebug : MonoBehaviour
{
    private enum GizmoViewMode { all, finished, unfinished }
    [SerializeField] private TacticalPositionGenerator.CoverGenerationMode _currentCoverGenMode = TacticalPositionGenerator.CoverGenerationMode.lowCover;
    private TacticalPositionGenerator.CoverGenerationMode _lastCoverGenMode;
    [SerializeField] private TacticalPositionGenerator _generator;
    [SerializeField] private TacticalGeneratorSettings _settings;
    [SerializeField] private bool _showPositions = false;
    [SerializeField] private bool _showSpawners = false;

    [Header("Gizmos")]
    [SerializeField] private bool _createGizmoDebugObjects = false;
    [SerializeField] private GizmoViewMode _currentGizmoViewMode;
    private GizmoViewMode _lastGizmoViewMode;
    [Range(1f, 5f)] public float _distanceToCreateGizmos = 3f;

    [Header("Changed positions debug")]
    public bool _createPosChangesDebugObjects = false;
    [Range(0.01f, 0.25f)][SerializeField] private float _maxDistanceToConsiderSamePosition = 0.05f;
    [Range(1f, 3f)][SerializeField] private float _maxDegreesDifferenceToConsiderSamePosition = 1f;

    [Header("Scene references")]
    [SerializeField] private GameObject _debugParentAdded;
    [SerializeField] private GameObject _debugParentRemoved;
    [SerializeField] private GameObject _debugParentModified;
    [SerializeField] private GameObject _debugGizmoGOParent;
    [SerializeField] private GameObject _manualPositionsParent;
    [SerializeField] private Transform _gizmo3DCursor;

    [Header("Prefabs")]
    [SerializeField] private GameObject _debugGizmoGOPrefab;
    [SerializeField] private GameObject _debugGameObjectPosChangePrefab;
    [SerializeField] private GameObject _manualPositionPrefab;


    CornerFinder _cornerFinder;
    CornerFinderDebug _cornerFinderDebug;

    PositionValidator _positionValidator;
    PositionValidatorDebug _positionValidatorDebug;


    TacticalPositionGenerator Generator
    {
        get
        {
            if (_generator == null)
            {
                _generator = new(_settings, GetCornerFinder, GetPositionValidator);
            }
            return _generator;
        }
    }

    public CornerFinder GetCornerFinder
    {
        get
        {
            return _createGizmoDebugObjects ? (_cornerFinderDebug ??= new CornerFinderDebug(this))
                            : (_cornerFinder ??= new CornerFinder());
        }
    }

    public PositionValidator GetPositionValidator
    {
        get
        {
            return _createGizmoDebugObjects ? (_positionValidatorDebug ??= new PositionValidatorDebug())
                            : (_positionValidator ??= new PositionValidator());
        }
    }

    public TacticalPositionGenerator.CoverGenerationMode CurCoverGenMode
    {
        get
        {
            return _currentCoverGenMode;
        }
    }

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


    public Transform Gizmo3DCursor
    {
        get { return _gizmo3DCursor; }
    }

    private void OnEnable()
    {
        _lastGizmoViewMode = _currentGizmoViewMode;
        _lastCoverGenMode = _currentCoverGenMode;
        Generator.OnContextUpdated += HandleContextUpdated;
    }

    private void OnDisable()
    {
        Generator.OnContextUpdated -= HandleContextUpdated;
    }

    private void OnValidate()
    {
        if (_currentGizmoViewMode != _lastGizmoViewMode)
        {
            GizmoViewChanged(_currentGizmoViewMode);
            _lastGizmoViewMode = _currentGizmoViewMode;
        }
        if (_currentCoverGenMode != _lastCoverGenMode)
        {
            CoverGenTypeChanged(_currentCoverGenMode);
            _lastCoverGenMode = _currentCoverGenMode;
        }
        Generator.UpdateCornerFinder(GetCornerFinder);
    }

    private void ComputeDifferences(List<TacticalPosition> oldPositions, List<TacticalPosition> newPositions)
    {
        if (!_createPosChangesDebugObjects)
        {
            return;
        }
        GameObject debugGO = null;

        if (_createPosChangesDebugObjects)
        {
            debugGO = _debugGameObjectPosChangePrefab;
        }

        foreach (var classifier in Classifiers)
        {
            classifier.Classify(oldPositions, newPositions, debugGO);
        }
    }

    public bool PositionGizmoInRange(Vector3 position)
    {
        return Vector3.Distance(position, _gizmo3DCursor.position) < _distanceToCreateGizmos;
    }

    public void HandleNewPotentialPosition(Vector3 position, TacticalDebugData debugData)
    {
        if (PositionGizmoInRange(position))
        {
            Undo.RecordObject(_debugGizmoGOParent, "Add a new potential position");
            GameObject gizmoDebugGO = Instantiate(_debugGizmoGOPrefab, _debugGizmoGOParent.transform);
            Undo.RegisterCreatedObjectUndo(gizmoDebugGO, "Added a gizmo debug gameobject");
            TacticalPosDebugGizmoGO gizmoDebug = gizmoDebugGO.GetComponent<TacticalPosDebugGizmoGO>();
            gizmoDebugGO.transform.position = position;
            gizmoDebug.TacticalDebugData = debugData;
            UpdateChildVisibility(_currentGizmoViewMode, _currentCoverGenMode, gizmoDebugGO);
            EditorUtility.SetDirty(_debugGizmoGOParent);
        }
    }

    private void GizmoViewChanged(GizmoViewMode viewMode)
    {
        for (int i = 0; i < _debugGizmoGOParent.transform.childCount; i++)
        {
            GameObject childGO = _debugGizmoGOParent.transform.GetChild(i).gameObject;
            UpdateChildVisibility(viewMode, _currentCoverGenMode, childGO);
        }
    }

    private void CoverGenTypeChanged(TacticalPositionGenerator.CoverGenerationMode genMode)
    {
        for (int i = 0; i < _debugGizmoGOParent.transform.childCount; i++)
        {
            GameObject childGO = _debugGizmoGOParent.transform.GetChild(i).gameObject;
            UpdateChildVisibility(_currentGizmoViewMode, genMode, childGO);
        }
    }

    private void UpdateChildVisibility(GizmoViewMode viewMode, TacticalPositionGenerator.CoverGenerationMode coverGenMode, GameObject childGO)
    {
        if (childGO.TryGetComponent(out TacticalPosDebugGizmoGO childDebugGO))
        {
            bool viewModeOK = viewMode == GizmoViewMode.all ||
                (childDebugGO.TacticalDebugData.Finished && viewMode == GizmoViewMode.finished) ||
                (!childDebugGO.TacticalDebugData.Finished && viewMode == GizmoViewMode.unfinished);

            bool coverTypeOK = coverGenMode == TacticalPositionGenerator.CoverGenerationMode.all ||
                childDebugGO.TacticalDebugData.genMode == coverGenMode;


            SetChildVisibility(childGO, viewModeOK && coverTypeOK);
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
            {
                childGO.SetActive(shouldShow);
            }
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
            if (context.cornerSettings.genMode == TacticalPositionGenerator.CoverGenerationMode.manual)
            {
                return;
            }

            if (context.positionData.Positions.Count == 0 && oldPositions == null)
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
        Undo.RecordObject(_debugGizmoGOParent, "Clear debug gizmo GOs");
        for (int i = _debugGizmoGOParent.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = _debugGizmoGOParent.transform.GetChild(i).gameObject;
            if (child.TryGetComponent(out TacticalPosDebugGizmoGO debugGO))
            {
                if (debugGO.TacticalDebugData.genMode == context.cornerSettings.genMode)
                {
#if UNITY_EDITOR
                    Undo.DestroyObjectImmediate(child);
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
        if (!_showPositions)
        {
            return;
        }

        int totalCount = 0;
        foreach (var activeContext in Generator.GetActiveCoverGenContexts(_currentCoverGenMode))
        {
            totalCount += DisplayPositions(activeContext.positionData.Positions);
        }

        if (_showSpawners)
        {
            foreach (Vector3 pos in Generator.GetSpawnerData.Positions)
            {
                Gizmos.DrawSphere(pos, 0.1f);
            }
        }

        Debug.Log($"Currently displaying {totalCount} positions");

        Draw3DCursor();
    }

    private void Draw3DCursor()
    {
        if (Gizmo3DCursor == null)
            return;


        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(Gizmo3DCursor.position, 0.1f);

        if (Generator != null && _distanceToCreateGizmos > 0)
        {
            Gizmos.color = new Color(1, 1, 0, 0.1f);
            Gizmos.DrawWireSphere(Gizmo3DCursor.position, _distanceToCreateGizmos);
        }
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

    public void CreateSpawnersAlongTheGrid()
    {
        Generator.CreateSpawnersAlongTheGrid();
    }

    public void GenerateTacticalPositions()
    {
        Generator.GenerateTacticalPositions(_currentCoverGenMode);
    }

    public void ClearAllTacticalData()
    {
        Generator.ClearAllTacticalData(_currentCoverGenMode);
    }

    public void SaveManualPositions()
    {
        Generator.SaveManualPositions(_manualPositionsParent);
    }

    public void LoadManualPositions()
    {
        Generator.LoadManualPositions(_manualPositionPrefab, _manualPositionsParent);
    }
}
