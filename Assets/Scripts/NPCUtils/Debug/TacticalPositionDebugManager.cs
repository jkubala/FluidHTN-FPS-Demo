using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
    public class TacticalPositionDebugManager
    {
        public enum GizmoViewMode { All, Finished, Unfinished }

        public TacticalPositionDebugManager(TacticalPositionGeneratorEditorWindow editorWindow)
        {
            _editorWindow = editorWindow;
            _editorWindow.OnGizmoViewModeChanged += GizmoViewChanged;
            _editorWindow.OnCoverGenTypeChanged += CoverGenTypeChanged;
            _editorWindow.OnSceneLoad += InitializeSceneParents;
            _editorWindow.OnOnSceneGUI += Handle3DCursor;
            _editorWindow.Generator.OnContextUpdated += HandleContextUpdated;

        }

        ~TacticalPositionDebugManager()
        {
            _editorWindow.OnGizmoViewModeChanged -= GizmoViewChanged;
            _editorWindow.OnCoverGenTypeChanged -= CoverGenTypeChanged;
            _editorWindow.OnSceneLoad -= InitializeSceneParents;
            _editorWindow.OnOnSceneGUI -= Handle3DCursor;
            _editorWindow.Generator.OnContextUpdated -= HandleContextUpdated;
        }

        private void Handle3DCursor()
        {
            Process3DCursorInput();
            Draw3DCursor();
        }

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
                if (_debugGameObjectPosChangePrefab != null)
                {
                    return _debugGameObjectPosChangePrefab;
                }

                string path = "Assets/Content/NPCUtils/TacticalGrid/Prefabs/TacticalPosChangeDebugGO.prefab";
                _debugGameObjectPosChangePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (_debugGameObjectPosChangePrefab == null)
                {
                    CheckPrefabFolderExistence();
                    Debug.LogError($"No debugChangePos prefab at {path}!");
                }

                return _debugGameObjectPosChangePrefab;
            }
            set
            {
                _debugGameObjectPosChangePrefab = value;
            }
        }

        public GameObject GetDebugGizmoGOPrefab
        {
            get
            {
                if (_debugGizmoGOPrefab != null)
                {
                    return _debugGizmoGOPrefab;
                }

                string path = "Assets/Content/NPCUtils/TacticalGrid/Prefabs/TacticalPosGizmoDebugGO.prefab";
                _debugGizmoGOPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (_debugGizmoGOPrefab == null)
                {
                    CheckPrefabFolderExistence();
                    Debug.LogError($"No debugGizmo prefab at {path}!");
                }

                return _debugGizmoGOPrefab;
            }
            set { _debugGizmoGOPrefab = value; }
        }

        private static void CheckPrefabFolderExistence()
        {
            string folderPath = "Assets/Content/NPCUtils/TacticalGrid/Prefabs";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogError("Did now find the prefab folder of Tactical Grid! Making it now.");
                AssetDatabase.CreateFolder("Assets/Content/NPCUtils/TacticalGrid", "Prefabs");
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
                    new AddedPositionClassifier(_debugParentAdded, _editorWindow.MaxDistanceToConsiderSamePosition, _editorWindow.MaxDegreesDifferenceToConsiderSamePosition),
                    new RemovedPositionClassifier(_debugParentRemoved, _editorWindow.MaxDistanceToConsiderSamePosition, _editorWindow.MaxDegreesDifferenceToConsiderSamePosition),
                    new ModifiedPositionClassifier(_debugParentModified, _editorWindow.MaxDistanceToConsiderSamePosition, _editorWindow.MaxDegreesDifferenceToConsiderSamePosition)
                };
                }
                return _classifiers;
            }
        }

        public Transform Gizmo3DCursor
        {
            get { return _gizmo3DCursor; }
        }

        private void InitializeSceneParents()
        {
            var debugRoot = GameObject.Find("TacticalPositionDebugParent");
            if (debugRoot == null)
            {
                debugRoot = new GameObject("TacticalPositionDebugParent");
                Undo.RegisterCreatedObjectUndo(debugRoot, "Create DebugParent for tactical positions");
            }

            _debugGizmoGOParent = GetOrCreateChild(debugRoot.transform, "Gizmos");
            _debugParentAdded = GetOrCreateChild(debugRoot.transform, "AddedPositions");
            _debugParentRemoved = GetOrCreateChild(debugRoot.transform, "RemovedPositions");
            _debugParentModified = GetOrCreateChild(debugRoot.transform, "ModifiedPositions");
            _gizmo3DCursor = GetOrCreateChild(debugRoot.transform, "Gizmo3DCursor").transform;
        }

        private void ComputeDifferences(List<TacticalPosition> oldPositions, List<TacticalPosition> newPositions)
        {
            if (!_editorWindow.CreatePosChangesDebugObjects)
            {
                return;
            }

            foreach (var classifier in Classifiers)
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

        private GameObject GetOrCreateChild(Transform parent, string childName)
        {
            var childTransform = parent.Find(childName);
            if (childTransform == null)
            {
                childTransform = new GameObject(childName).transform;
                Undo.RegisterCreatedObjectUndo(childTransform.gameObject, $"Create {childName}");
                childTransform.SetParent(parent);
            }
            return childTransform.gameObject;
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
            if (_debugGizmoGOParent == null) return;

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

        private void Draw3DCursor()
        {
            if (Gizmo3DCursor == null)
                return;

            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, Gizmo3DCursor.position, Quaternion.identity, 0.2f, EventType.Repaint);

            if (_editorWindow.Generator != null && _editorWindow.DistanceToCreateGizmos > 0)
            {
                Handles.color = new Color(1, 1, 0, 0.1f);
                Handles.DrawWireDisc(Gizmo3DCursor.position, Vector3.up, _editorWindow.DistanceToCreateGizmos);
            }
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
    }
}
