using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPSDemo.NPC.Utilities
{
    public class TacticalPositionGeneratorEditorWindow : EditorWindow
    {
        [SerializeField] private TacticalPositionGenerator.CoverGenerationMode _currentCoverGenMode = TacticalPositionGenerator.CoverGenerationMode.LowCover;
        private TacticalPositionGenerator.CoverGenerationMode _lastCoverGenMode;

        [SerializeField] private TacticalPositionGenerator _generator;
        [SerializeField] private TacticalGeneratorSettings _settings;
        [SerializeField] private GameObject _manualPositionsParent;
        [SerializeField] private GameObject _manualPositionPrefab;

        [SerializeField] private bool _showPositions = false;
        [SerializeField, HideInInspector] private int _currentPositionsCount = 0;

        [SerializeField, HideInInspector] private int _totalPositionsCount = 0;

        [SerializeField] private bool _showSpawners = false;

        [SerializeField] private bool _createGizmoDebugObjects = false;
        [SerializeField] private TacticalPositionDebugManager.GizmoViewMode _currentGizmoViewMode;
        private TacticalPositionDebugManager.GizmoViewMode _lastGizmoViewMode;
        [SerializeField] private float _distanceToCreateGizmos = 3f;

        [SerializeField] private bool _createPosChangesDebugObjects = false;
        [SerializeField] private float _maxDistanceToConsiderSamePosition = 0.05f;
        [SerializeField] private float _maxDegreesDifferenceToConsiderSamePosition = 1f;

        private CornerFinder _cornerFinder;
        private CornerFinderDebug _cornerFinderDebug;
        private PositionValidator _positionValidator;
        private PositionValidatorDebug _positionValidatorDebug;
        private TacticalPositionDebugManager _debugManager;

        private Vector2 _scrollPosition;
        private bool _gizmosFoldout = true;
        private bool _posChangesFoldout = true;

        private TacticalPositionGenerator Generator
        {
            get
            {
                if (GetSettings != null)
                {
                    _generator ??= new(GetSettings, GetCornerFinder, GetPositionValidator);

                    return _generator;
                }
                Debug.LogError("Settings is missing in the Tactical Position Editor Window!");
                return null;
            }
        }

        public CornerFinder GetCornerFinder
        {
            get
            {
                return _createGizmoDebugObjects ? (_cornerFinderDebug ??= new CornerFinderDebug(GetDebugManager))
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

        public TacticalGeneratorSettings GetSettings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = AssetLoaderHelper.GetOrCreateSettings();
                }
                return _settings;
            }
        }

        public GameObject GetManualPositionPrefab
        {
            get
            {
                if (_manualPositionPrefab == null)
                {
                    _manualPositionPrefab = AssetLoaderHelper.GetPrefab("Prefabs/ManualTacticalPosition.prefab", "ManualTacticalPosition");
                }
                return _manualPositionPrefab;
            }
        }

        public TacticalPositionDebugManager GetDebugManager
        {
            get
            {
                _debugManager ??= new TacticalPositionDebugManager(this);
                _debugManager.Init();
                return _debugManager;
            }
        }

        public TacticalPositionGenerator.CoverGenerationMode CurCoverGenMode
        {
            get { return _currentCoverGenMode; }
        }

        public TacticalPositionDebugManager.GizmoViewMode CurGizmoViewMode
        {
            get { return _currentGizmoViewMode; }
        }

        public float DistanceToCreateGizmos
        {
            get { return _distanceToCreateGizmos; }
        }

        public float MaxDistanceToConsiderSamePosition
        {
            get { return _maxDistanceToConsiderSamePosition; }
        }

        public float MaxDegreesDifferenceToConsiderSamePosition
        {
            get { return _maxDegreesDifferenceToConsiderSamePosition; }
        }

        public bool CreatePosChangesDebugObjects
        {
            get { return _createPosChangesDebugObjects; }
        }

        private static bool IsWindowOpen => HasOpenInstances<TacticalPositionGeneratorEditorWindow>();

        public Action<TacticalPositionGenerator.CoverGenerationMode> OnCoverGenTypeChanged;
        public Action<TacticalPositionDebugManager.GizmoViewMode> OnGizmoViewModeChanged;
        public Action OnOnSceneGUI;
        public Action OnSceneLoad;
        public Action<TacticalPositionData, CoverGenerationContext> OnContextUpdated;


        [MenuItem("Tools/Tactical Position Generator")]
        public static void ShowWindow()
        {
            TacticalPositionGeneratorEditorWindow window = GetWindow<TacticalPositionGeneratorEditorWindow>();
            window.titleContent = new GUIContent("Tactical Position Generator");
            window.Show();
        }

        private void Awake()
        {
            _lastGizmoViewMode = _currentGizmoViewMode;
            _lastCoverGenMode = _currentCoverGenMode;
        }

        private void OnEnable()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            Generator.OnContextUpdated += PropagateOnContextUpdatedEvent;
            SceneView.duringSceneGui += OnSceneGUI;
            OnSceneLoad?.Invoke();
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            Generator.OnContextUpdated -= PropagateOnContextUpdatedEvent;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnDestroy()
        {
            GetDebugManager.Dispose();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawMainSettings();
            EditorGUILayout.Space();

            DrawGizmosSection();
            EditorGUILayout.Space();

            DrawPositionChangesSection();
            EditorGUILayout.Space();

            DrawActionButtons();

            EditorGUILayout.EndScrollView();

            HandleUIChanges();
        }

        private void PropagateOnContextUpdatedEvent(TacticalPositionData oldPositions, CoverGenerationContext context)
        {
            OnContextUpdated?.Invoke(oldPositions, context);
        }

        private void DrawMainSettings()
        {
            EditorGUILayout.LabelField("Main Settings", EditorStyles.boldLabel);

            if (GetSettings == null)
            {
                EditorGUILayout.HelpBox("Tactical Generator Settings is not assigned!", MessageType.Error);
            }

            _settings = EditorGUILayout.ObjectField("Settings", _settings, typeof(TacticalGeneratorSettings), false) as TacticalGeneratorSettings;

            EditorGUI.BeginChangeCheck();
            _currentCoverGenMode = (TacticalPositionGenerator.CoverGenerationMode)EditorGUILayout.EnumPopup("Cover Generation Mode", _currentCoverGenMode);
            if (EditorGUI.EndChangeCheck())
            {
                OnCoverGenTypeChanged?.Invoke(_currentCoverGenMode);
                _lastCoverGenMode = _currentCoverGenMode;
                UpdateVisibleCount();
            }

            EditorGUI.BeginChangeCheck();
            _showPositions = EditorGUILayout.Toggle("Show Positions", _showPositions);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateVisibleCount();
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            _showSpawners = EditorGUILayout.Toggle("Show Spawners", _showSpawners);
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        private void DrawGizmosSection()
        {
            _gizmosFoldout = EditorGUILayout.Foldout(_gizmosFoldout, "Gizmos", true);
            if (_gizmosFoldout)
            {
                EditorGUI.indentLevel++;

                _createGizmoDebugObjects = EditorGUILayout.Toggle("Create Gizmo Debug Objects", _createGizmoDebugObjects);

                EditorGUI.BeginChangeCheck();
                _currentGizmoViewMode = (TacticalPositionDebugManager.GizmoViewMode)EditorGUILayout.EnumPopup("Gizmo View Mode", _currentGizmoViewMode);
                if (EditorGUI.EndChangeCheck())
                {
                    OnGizmoViewModeChanged?.Invoke(_currentGizmoViewMode);
                    _lastGizmoViewMode = _currentGizmoViewMode;
                }

                _distanceToCreateGizmos = EditorGUILayout.Slider("Distance to Create Gizmos", _distanceToCreateGizmos, 1f, 5f);

                EditorGUI.indentLevel--;
            }
        }

        private void DrawPositionChangesSection()
        {
            _posChangesFoldout = EditorGUILayout.Foldout(_posChangesFoldout, "Position Changes Debug", true);
            if (_posChangesFoldout)
            {
                EditorGUI.indentLevel++;

                _createPosChangesDebugObjects = EditorGUILayout.Toggle("Create Position Changes Debug Objects", _createPosChangesDebugObjects);
                _maxDistanceToConsiderSamePosition = EditorGUILayout.Slider("Max Distance Same Position", _maxDistanceToConsiderSamePosition, 0.01f, 0.25f);
                _maxDegreesDifferenceToConsiderSamePosition = EditorGUILayout.Slider("Max Degrees Difference Same Position", _maxDegreesDifferenceToConsiderSamePosition, 1f, 3f);

                EditorGUI.indentLevel--;
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Spawners Along Grid"))
            {
                CreateSpawnersAlongTheGrid();
            }
            if (GUILayout.Button("Generate Tactical Positions"))
            {
                GenerateTacticalPositions();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All Tactical Data"))
            {
                ClearAllTacticalData();
            }
            if (GUILayout.Button("Save Manual Positions"))
            {
                SaveManualPositions();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Load Manual Positions"))
            {
                LoadManualPositions();
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_showPositions || GetSettings == null)
            {
                return;
            }

            _totalPositionsCount = 0;
            if (Generator != null)
            {
                foreach (var activeContext in Generator.GetActiveCoverGenContexts(_currentCoverGenMode))
                {
                    _totalPositionsCount += DisplayPositions(activeContext.positionData.Positions);
                }

                if (_showSpawners)
                {
                    foreach (Vector3 pos in Generator.GetSpawnerData.Positions)
                    {
                        Handles.color = Color.green;
                        Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.2f, EventType.Repaint);
                    }
                }
            }

            OnOnSceneGUI?.Invoke();
        }

        private void HandleUIChanges()
        {
            if (_currentGizmoViewMode != _lastGizmoViewMode)
            {
                OnGizmoViewModeChanged?.Invoke(_currentGizmoViewMode);
                _lastGizmoViewMode = _currentGizmoViewMode;
                UpdateVisibleCount();
            }

            if (_currentCoverGenMode != _lastCoverGenMode)
            {
                OnCoverGenTypeChanged?.Invoke(_currentCoverGenMode);
                _lastCoverGenMode = _currentCoverGenMode;
                UpdateVisibleCount();
            }

            Generator?.UpdateCornerFinder(GetCornerFinder);
        }

        private void UpdateVisibleCount()
        {
            _currentPositionsCount = 0;
            _totalPositionsCount = 0;

            if (Generator == null)
            {
                return;
            }

            List<CoverGenerationContext> allContexts = Generator.GetActiveCoverGenContexts(TacticalPositionGenerator.CoverGenerationMode.All);
            foreach (var context in allContexts)
            {
                _totalPositionsCount += context.positionData.Positions.Count;
            }

            List<CoverGenerationContext> currentContexts = Generator.GetActiveCoverGenContexts(CurCoverGenMode);
            foreach (var context in currentContexts)
            {
                _currentPositionsCount += context.positionData.Positions.Count;
            }
        }

        private int DisplayPositions(List<TacticalPosition> positions)
        {
            if (positions != null)
            {
                foreach (TacticalPosition position in positions)
                {
                    Color gizmoColor;
                    if (position.mainCover.type == CoverType.LeftCorner)
                    {
                        gizmoColor = Color.red;
                    }
                    else if (position.mainCover.type == CoverType.RightCorner)
                    {
                        gizmoColor = Color.blue;
                    }
                    else
                    {
                        gizmoColor = Color.white;
                    }

                    Handles.color = gizmoColor;

                    if (position.isOutside)
                    {
                        Handles.SphereHandleCap(0, position.Position, Quaternion.identity, 0.2f, EventType.Repaint);
                    }
                    else
                    {
                        Handles.DrawWireDisc(position.Position, Vector3.up, 0.1f);
                    }

                    Vector3 forward = position.mainCover.rotationToAlignWithCover * Vector3.forward;
                    Handles.DrawLine(position.Position, position.Position + forward);
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
            Generator.LoadManualPositions(GetManualPositionPrefab, _manualPositionsParent);
        }

        public void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            Debug.Log($"Scene opened: {scene.name}. Resetting scene references.");
            ResetReferences();
            OnSceneLoad?.Invoke();
            Repaint();
        }

        private void ResetReferences()
        {
            _generator = null;
            _settings = null;
            _manualPositionPrefab = null;
            _manualPositionsParent = null;

            GetDebugManager.Dispose();
            _debugManager = null;
        }
    }
}
