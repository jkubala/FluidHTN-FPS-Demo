using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
    public class TacticalPositionDebugManager
    {
        public enum GizmoViewMode { All, Finished, Unfinished }
        private readonly TacticalPositionGeneratorEditorWindow _editorWindow;

        [SerializeField] private GameObject _debugParentAdded;
        [SerializeField] private GameObject _debugParentRemoved;
        [SerializeField] private GameObject _debugParentModified;
        [SerializeField] private GameObject _debugGizmoGOParent;
        [SerializeField] private Transform _gizmo3DCursor;

        [SerializeField] private GameObject _debugGizmoGOPrefab;
        [SerializeField] private GameObject _debugGameObjectPosChangePrefab;

        public GameObject GetDebugGameObjectPosChangePrefab
        {
            get
            {
                if (_debugGameObjectPosChangePrefab == null)
                {
                    _debugGameObjectPosChangePrefab = AssetLoaderHelper.GetPrefab("Prefabs/TacticalPosChangeDebugGO.prefab", "TacticalPosChangeDebugGO");
                }
                return _debugGameObjectPosChangePrefab;
            }
        }

        public GameObject GetDebugGizmoGOPrefab
        {
            get
            {
                if (_debugGizmoGOPrefab == null)
                {
                    _debugGizmoGOPrefab = AssetLoaderHelper.GetPrefab("Prefabs/TacticalPosGizmoDebugGO.prefab", "TacticalPosGizmoDebugGO");
                }
                return _debugGizmoGOPrefab;
            }
        }

        [SerializeField] List<BasePositionClassifier> _classifiers = new();

        public Transform Gizmo3DCursor
        {
            get { return _gizmo3DCursor; }
        }

        public TacticalPositionDebugManager(TacticalPositionGeneratorEditorWindow editorWindow)
        {
            _editorWindow = editorWindow;
        }

        public void StartListening()
        {
            _editorWindow.OnGizmoViewModeChanged += GizmoViewChanged;
            _editorWindow.OnCoverGenTypeChanged += CoverGenTypeChanged;
            _editorWindow.OnSceneLoad += OnSceneLoad;
            _editorWindow.OnOnSceneGUI += Handle3DCursor;
            _editorWindow.OnContextUpdated += HandleContextUpdated;
            _editorWindow.OnPosChangeDistanceChanged += UpdateClassifierDistances;
            _editorWindow.OnPosChangeDegreeDifferenceChanged += UpdateClassifierDegreeDifferences;
            InitializeSceneReferences();
        }

        public void StopListening()
        {
            _editorWindow.OnGizmoViewModeChanged -= GizmoViewChanged;
            _editorWindow.OnCoverGenTypeChanged -= CoverGenTypeChanged;
            _editorWindow.OnSceneLoad -= OnSceneLoad;
            _editorWindow.OnOnSceneGUI -= Handle3DCursor;
            _editorWindow.OnContextUpdated -= HandleContextUpdated;
            _editorWindow.OnPosChangeDistanceChanged -= UpdateClassifierDistances;
            _editorWindow.OnPosChangeDegreeDifferenceChanged -= UpdateClassifierDegreeDifferences;
        }

        private void OnSceneLoad()
        {
            InitializeSceneReferences();
        }

        private void InitializeSceneReferences()
        {
            _debugGizmoGOParent = AssetLoaderHelper.GetOrCreateSceneObject("TacticalPositionDebugParent", "Gizmos");
            _debugParentAdded = AssetLoaderHelper.GetOrCreateSceneObject("TacticalPositionDebugParent", "AddedPositions");
            _debugParentRemoved = AssetLoaderHelper.GetOrCreateSceneObject("TacticalPositionDebugParent", "RemovedPositions");
            _debugParentModified = AssetLoaderHelper.GetOrCreateSceneObject("TacticalPositionDebugParent", "ModifiedPositions");
            _gizmo3DCursor = AssetLoaderHelper.GetOrCreateSceneObject("TacticalPositionDebugParent", "Gizmo3DCursor").transform;
            ReinitiliazeClassifiers();
        }

        private void ReinitiliazeClassifiers()
        {
            _classifiers.Clear();
            _classifiers.Add(new AddedPositionClassifier(_debugParentAdded, _editorWindow.MaxDistanceToConsiderSamePosition, _editorWindow.MaxDegreesDifferenceToConsiderSamePosition));
            _classifiers.Add(new RemovedPositionClassifier(_debugParentRemoved, _editorWindow.MaxDistanceToConsiderSamePosition, _editorWindow.MaxDegreesDifferenceToConsiderSamePosition));
            _classifiers.Add(new ModifiedPositionClassifier(_debugParentModified, _editorWindow.MaxDistanceToConsiderSamePosition, _editorWindow.MaxDegreesDifferenceToConsiderSamePosition));
        }

        private void UpdateClassifierDistances(float maxDistance)
        {
            foreach (BasePositionClassifier classifier in _classifiers)
            {
                classifier.UpdateMaxDistance(maxDistance);
            }
        }

        private void UpdateClassifierDegreeDifferences(float maxAngleDifference)
        {
            foreach (BasePositionClassifier classifier in _classifiers)
            {
                classifier.UpdateMaxDegreesDifference(maxAngleDifference);
            }
        }

        private void ComputeDifferences(List<TacticalPosition> oldPositions, List<TacticalPosition> newPositions)
        {
            if (!_editorWindow.CreatePosChangesDebugObjects)
            {
                return;
            }

            foreach (var classifier in _classifiers)
            {
                classifier.Classify(oldPositions, newPositions, GetDebugGameObjectPosChangePrefab);
            }
        }

        public bool PositionGizmoInRange(Vector3 position)
        {
            return Vector3.Distance(position, _gizmo3DCursor.position) < _editorWindow.DistanceToCreateGizmos;
        }

        public void HandleNewPotentialPosition(Vector3 position, TacticalDebugData debugData)
        {
            if (PositionGizmoInRange(position))
            {
                Undo.RecordObject(_debugGizmoGOParent, "Add a new potential position");
                GameObject gizmoDebugGO = GameObject.Instantiate(GetDebugGizmoGOPrefab, _debugGizmoGOParent.transform);
                Undo.RegisterCreatedObjectUndo(gizmoDebugGO, "Added a gizmo debug gameobject");
                TacticalPosDebugGizmoGO gizmoDebug = gizmoDebugGO.GetComponent<TacticalPosDebugGizmoGO>();
                gizmoDebugGO.transform.position = position;
                gizmoDebug.TacticalDebugData = debugData;
                UpdateChildVisibility(_editorWindow.CurGizmoViewMode, _editorWindow.CurCoverGenMode, gizmoDebugGO);
                EditorUtility.SetDirty(_debugGizmoGOParent);
            }
        }

        private void GizmoViewChanged(GizmoViewMode viewMode)
        {
            if (_debugGizmoGOParent == null) return;

            for (int i = 0; i < _debugGizmoGOParent.transform.childCount; i++)
            {
                GameObject childGO = _debugGizmoGOParent.transform.GetChild(i).gameObject;
                UpdateChildVisibility(viewMode, _editorWindow.CurCoverGenMode, childGO);
            }
        }

        private void CoverGenTypeChanged(TacticalPositionGenerator.CoverGenerationMode genMode)
        {
            if (_debugGizmoGOParent == null) return;

            for (int i = 0; i < _debugGizmoGOParent.transform.childCount; i++)
            {
                GameObject childGO = _debugGizmoGOParent.transform.GetChild(i).gameObject;
                UpdateChildVisibility(_editorWindow.CurGizmoViewMode, genMode, childGO);
            }
        }

        private void UpdateChildVisibility(GizmoViewMode viewMode, TacticalPositionGenerator.CoverGenerationMode coverGenMode, GameObject childGO)
        {
            if (childGO.TryGetComponent(out TacticalPosDebugGizmoGO childDebugGO))
            {
                bool viewModeOK = viewMode == GizmoViewMode.All ||
                    (childDebugGO.TacticalDebugData.Finished && viewMode == GizmoViewMode.Finished) ||
                    (!childDebugGO.TacticalDebugData.Finished && viewMode == GizmoViewMode.Unfinished);

                bool coverTypeOK = coverGenMode == TacticalPositionGenerator.CoverGenerationMode.All ||
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
            if (childGO != null)
                childGO.SetActive(shouldShow);
        }

        private void HandleContextUpdated(TacticalPositionData oldPositions, CoverGenerationContext context)
        {
            if (context != null)
            {
                if (context.cornerSettings.genMode == TacticalPositionGenerator.CoverGenerationMode.Manual)
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
            foreach (BasePositionClassifier classifier in _classifiers)
            {
                classifier.ClearDebugGOs();
            }
        }

        private void ClearDebugGizmosGOs(CoverGenerationContext context)
        {
            if (_debugGizmoGOParent == null)
            {
                return;
            }

            Undo.RecordObject(_debugGizmoGOParent, "Clear debug gizmo GOs");
            for (int i = _debugGizmoGOParent.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = _debugGizmoGOParent.transform.GetChild(i).gameObject;
                if (child.TryGetComponent(out TacticalPosDebugGizmoGO debugGO))
                {
                    if (debugGO.TacticalDebugData.genMode == context.cornerSettings.genMode)
                    {
                        Undo.DestroyObjectImmediate(child);
                    }
                }
                else
                {
                    Debug.LogError($"{child.name} did not have TacticalPosDebugGO script attached!");
                }
            }
        }

        private void Handle3DCursor()
        {
            Process3DCursorInput();
            Draw3DCursor();
        }

        private void Process3DCursorInput()
        {
            if (_gizmo3DCursor == null)
            {
                return;
            }

            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

                Vector3 newPosition;
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    newPosition = hit.point;
                }
                else
                {
                    newPosition = ray.origin + ray.direction * 10f;
                }

                Undo.RecordObject(_gizmo3DCursor, "Move 3D Cursor");
                _gizmo3DCursor.position = newPosition;
                EditorUtility.SetDirty(_gizmo3DCursor);

                e.Use();
            }
        }

        private void Draw3DCursor()
        {
            if (Gizmo3DCursor == null)
            {
                return;
            }

            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, Gizmo3DCursor.position, Quaternion.identity, 0.2f, EventType.Repaint);

            if (_editorWindow.DistanceToCreateGizmos > 0)
            {
                Handles.color = new Color(1, 1, 0, 0.1f);
                Handles.DrawWireDisc(Gizmo3DCursor.position, Vector3.up, _editorWindow.DistanceToCreateGizmos);
            }
        }
    }
}
