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
        [SerializeField, HideInInspector] private int _currentVisiblePositionsCount = 0;
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

        private TacticalPositionGenerator Generator
        {
            get
            {
                if (_settings != null)
                {
                    _generator ??= new(_settings, GetCornerFinder, GetPositionValidator);

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
                return _createGizmoDebugObjects ? (_cornerFinderDebug ??= new CornerFinderDebug(_debugManager))
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

        public Action<TacticalPositionGenerator.CoverGenerationMode> OnCoverGenTypeChanged;
        public Action<TacticalPositionDebugManager.GizmoViewMode> OnGizmoViewModeChanged;
        public Action OnOnSceneGUI;
        public Action OnSceneLoad;
        public Action<float> OnPosChangeDistanceChanged;
        public Action<float> OnPosChangeDegreeDifferenceChanged;
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
            _debugManager ??= new(this);
            _manualPositionPrefab = AssetLoaderHelper.GetPrefab("Prefabs/ManualTacticalPosition.prefab", "ManualTacticalPosition");
            _settings = AssetLoaderHelper.GetOrCreateSettings();
            _generator ??= new(_settings, GetCornerFinder, GetPositionValidator);
            LoadSceneReferences();

            EditorSceneManager.sceneOpened += OnSceneOpened;
            Generator.OnContextUpdated += PropagateOnContextUpdatedEvent;
            SceneView.duringSceneGui += OnSceneGUI;

            _debugManager.StartListening();
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
            _debugManager.StopListening();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawMainSettings();
            DrawGizmosSection();
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

            if (_settings == null)
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
            if (_showPositions)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Position Count:", $"{_currentVisiblePositionsCount}/{_totalPositionsCount}", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }
            if (EditorGUI.EndChangeCheck())
            {
                UpdateVisibleCount();
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            _showSpawners = EditorGUILayout.Toggle("Show Spawners", _showSpawners);
            if (_showSpawners)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Spawner Count:", $"{Generator.GetSpawnerData.Positions.Count}", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        private void DrawGizmosSection()
        {
            EditorGUI.BeginChangeCheck();
            _currentGizmoViewMode = (TacticalPositionDebugManager.GizmoViewMode)EditorGUILayout.EnumPopup("Gizmo View Mode", _currentGizmoViewMode);
            if (EditorGUI.EndChangeCheck())
            {
                OnGizmoViewModeChanged?.Invoke(_currentGizmoViewMode);
                _lastGizmoViewMode = _currentGizmoViewMode;
            }

            _createGizmoDebugObjects = EditorGUILayout.Toggle("Create Gizmo Debug Objects", _createGizmoDebugObjects);
            if (_createGizmoDebugObjects)
            {
                _distanceToCreateGizmos = EditorGUILayout.Slider("Distance to Create Gizmos", _distanceToCreateGizmos, 1f, 5f);
            }
        }

        private void DrawPositionChangesSection()
        {
            _createPosChangesDebugObjects = EditorGUILayout.Toggle("Create Position Changes Debug Objects", _createPosChangesDebugObjects);
            if (_createPosChangesDebugObjects)
            {
                EditorGUI.BeginChangeCheck();
                _maxDistanceToConsiderSamePosition = EditorGUILayout.Slider("Max Distance Same Position", _maxDistanceToConsiderSamePosition, 0.01f, 0.25f);
                if (EditorGUI.EndChangeCheck())
                {
                    OnPosChangeDistanceChanged?.Invoke(_maxDistanceToConsiderSamePosition);
                }

                EditorGUI.BeginChangeCheck();
                _maxDegreesDifferenceToConsiderSamePosition = EditorGUILayout.Slider("Max Degrees Difference Same Position", _maxDegreesDifferenceToConsiderSamePosition, 1f, 3f);
                if (EditorGUI.EndChangeCheck())
                {
                    OnPosChangeDegreeDifferenceChanged?.Invoke(_maxDegreesDifferenceToConsiderSamePosition);
                }
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
            if (Generator != null)
            {
                if (_showPositions)
                {
                    foreach (var activeContext in Generator.GetActiveCoverGenContexts(_currentCoverGenMode))
                    {
                        DisplayPositions(activeContext.positionData.Positions);
                    }
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
            _currentVisiblePositionsCount = 0;
            _totalPositionsCount = 0;

            if (Generator == null)
            {
                return;
            }

            foreach (CoverGenerationContext context in Generator.GetActiveCoverGenContexts(TacticalPositionGenerator.CoverGenerationMode.All))
            {
                _totalPositionsCount += context.positionData.Positions.Count;
            }

            foreach (CoverGenerationContext context in Generator.GetActiveCoverGenContexts(CurCoverGenMode))
            {
                _currentVisiblePositionsCount += context.positionData.Positions.Count;
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
            Generator.LoadManualPositions(_manualPositionPrefab, _manualPositionsParent);
        }

        public void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            Debug.Log($"Scene opened: {scene.name}. Resetting scene references.");
            LoadSceneReferences();
            OnSceneLoad?.Invoke();
            Repaint();
        }

        private void LoadSceneReferences()
        {
            _settings = AssetLoaderHelper.GetOrCreateSettings();
            _generator.UpdateSettings(_settings);
            _manualPositionsParent = AssetLoaderHelper.GetOrCreateSceneObject("TacticalPositionDebugParent", "ManualPositions");
        }
    }
}
