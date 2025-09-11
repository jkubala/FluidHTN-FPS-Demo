using System;
using System.Collections.Generic;
using System.Linq;
using FPSDemo.NPC.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TacticalPositionGeneratorEditorWindow : EditorWindow
{
    private enum GizmoViewMode { All, Finished, Unfinished }

    [SerializeField] private TacticalPositionGenerator.CoverGenerationMode _currentCoverGenMode = TacticalPositionGenerator.CoverGenerationMode.LowCover;
    private TacticalPositionGenerator.CoverGenerationMode _lastCoverGenMode;

    [SerializeField] private TacticalPositionGenerator _generator;
    [SerializeField] private TacticalGeneratorSettings _settings;
    [SerializeField] private bool _showPositions = false;
    [SerializeField, HideInInspector] private int _currentPositionsCount = 0;

    [SerializeField, HideInInspector] private int _totalPositionsCount = 0;

    [SerializeField] private bool _showSpawners = false;

    [SerializeField] private bool _createGizmoDebugObjects = false;
    [SerializeField] private GizmoViewMode _currentGizmoViewMode;
    private GizmoViewMode _lastGizmoViewMode;
    [SerializeField] private float _distanceToCreateGizmos = 3f;

    [SerializeField] private bool _createPosChangesDebugObjects = false;
    [SerializeField] private float _maxDistanceToConsiderSamePosition = 0.05f;
    [SerializeField] private float _maxDegreesDifferenceToConsiderSamePosition = 1f;

    [SerializeField] private GameObject _debugParentAdded;
    [SerializeField] private GameObject _debugParentRemoved;
    [SerializeField] private GameObject _debugParentModified;
    [SerializeField] private GameObject _debugGizmoGOParent;
    [SerializeField] private GameObject _manualPositionsParent;
    [SerializeField] private Transform _gizmo3DCursor;

    [SerializeField] private GameObject _debugGizmoGOPrefab;
    [SerializeField] private GameObject _debugGameObjectPosChangePrefab;
    [SerializeField] private GameObject _manualPositionPrefab;

    private CornerFinder _cornerFinder;
    private CornerFinderDebug _cornerFinderDebug;
    private PositionValidator _positionValidator;
    private PositionValidatorDebug _positionValidatorDebug;

    private Vector2 _scrollPosition;
    private bool _sceneRefsFoldout = true;
    private bool _prefabsFoldout = true;
    private bool _gizmosFoldout = true;
    private bool _posChangesFoldout = true;

    private TacticalPositionGenerator Generator
    {
        get
        {
            if (GetSettings != null)
            {
                if (_generator == null)
                {
                    _generator = new(GetSettings, GetCornerFinder, GetPositionValidator);
                }
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

    public TacticalGeneratorSettings GetSettings
    {
        get
        {
            if (_settings != null)
            {
                return _settings;
            }

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string path = $"Assets/Content/NPCUtils/TacticalGrid/{sceneName}/{sceneName}TacticalGeneratorSettings.asset";
            _settings = AssetDatabase.LoadAssetAtPath<TacticalGeneratorSettings>(path);

            if (_settings == null)
            {
                string folderPath = $"Assets/Content/NPCUtils/TacticalGrid/{sceneName}";
                if (!AssetDatabase.IsValidFolder(folderPath))
                    AssetDatabase.CreateFolder("Assets/Content/NPCUtils/TacticalGrid", sceneName);

                _settings = ScriptableObject.CreateInstance<TacticalGeneratorSettings>();
                AssetDatabase.CreateAsset(_settings, $"{folderPath}/{sceneName}TacticalGeneratorSettings.asset");
                AssetDatabase.SaveAssets();
                Debug.Log($"Created default TacticalGeneratorSettings for scene {sceneName}");
            }

            return _settings;
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

    public GameObject GetManualPositionPrefab
    {
        get
        {
            if (_manualPositionPrefab != null)
            {
                return _manualPositionPrefab;
            }

            string path = "Assets/Content/NPCUtils/TacticalGrid/Prefabs/ManualTacticalPosition.prefab";
            _manualPositionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (_manualPositionPrefab == null)
            {
                CheckPrefabFolderExistence();
                Debug.LogError($"No ManualTacticalPosition prefab at {path}!");
            }

            return _manualPositionPrefab;
        }

        set { _manualPositionPrefab = value; }
    }

    public TacticalPositionGenerator.CoverGenerationMode CurCoverGenMode
    {
        get { return _currentCoverGenMode; }
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

    private static bool IsWindowOpen => HasOpenInstances<TacticalPositionGeneratorEditorWindow>();

    [MenuItem("Tools/Tactical Position Generator")]
    public static void ShowWindow()
    {
        TacticalPositionGeneratorEditorWindow window = GetWindow<TacticalPositionGeneratorEditorWindow>();
        window.titleContent = new GUIContent("Tactical Position Generator");
        window.Show();
    }

    private void OnEnable()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;

        _lastGizmoViewMode = _currentGizmoViewMode;
        _lastCoverGenMode = _currentCoverGenMode;

        if (Generator != null)
        {
            Generator.OnContextUpdated += HandleContextUpdated;
        }

        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;

        if (Generator != null)
        {
            Generator.OnContextUpdated -= HandleContextUpdated;
        }

        SceneView.duringSceneGui -= OnSceneGUI;
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

        DrawSceneReferencesSection();
        EditorGUILayout.Space();

        DrawPrefabsSection();
        EditorGUILayout.Space();

        DrawActionButtons();

        EditorGUILayout.EndScrollView();

        HandleUIChanges();
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
            CoverGenTypeChanged(_currentCoverGenMode);
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
            _currentGizmoViewMode = (GizmoViewMode)EditorGUILayout.EnumPopup("Gizmo View Mode", _currentGizmoViewMode);
            if (EditorGUI.EndChangeCheck())
            {
                GizmoViewChanged(_currentGizmoViewMode);
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

    private void DrawSceneReferencesSection()
    {
        if (_gizmo3DCursor == null)
        {
            EditorGUILayout.HelpBox("Gizmo 3D Cursor (Transform) is not assigned!", MessageType.Error);
        }

        _sceneRefsFoldout = EditorGUILayout.Foldout(_sceneRefsFoldout, "Scene References", true);
        if (_sceneRefsFoldout)
        {
            EditorGUI.indentLevel++;

            _debugParentAdded = EditorGUILayout.ObjectField("Debug Parent Added", _debugParentAdded, typeof(GameObject), true) as GameObject;
            _debugParentRemoved = EditorGUILayout.ObjectField("Debug Parent Removed", _debugParentRemoved, typeof(GameObject), true) as GameObject;
            _debugParentModified = EditorGUILayout.ObjectField("Debug Parent Modified", _debugParentModified, typeof(GameObject), true) as GameObject;
            _debugGizmoGOParent = EditorGUILayout.ObjectField("Debug Gizmo GO Parent", _debugGizmoGOParent, typeof(GameObject), true) as GameObject;
            _manualPositionsParent = EditorGUILayout.ObjectField("Manual Positions Parent", _manualPositionsParent, typeof(GameObject), true) as GameObject;
            _gizmo3DCursor = EditorGUILayout.ObjectField("Gizmo 3D Cursor", _gizmo3DCursor, typeof(Transform), true) as Transform;
            EditorGUILayout.LabelField("Tactical Position Generator Debug", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Tip: Hold SHIFT and Left-Click in the Scene View to move the 3D cursor.", MessageType.Info);

            EditorGUI.indentLevel--;
        }
    }

    private void DrawPrefabsSection()
    {
        _prefabsFoldout = EditorGUILayout.Foldout(_prefabsFoldout, "Prefabs", true);
        if (_prefabsFoldout)
        {
            EditorGUI.indentLevel++;

            GetDebugGizmoGOPrefab = EditorGUILayout.ObjectField("Debug Gizmo GO Prefab", _debugGizmoGOPrefab, typeof(GameObject), false) as GameObject;
            GetDebugGameObjectPosChangePrefab = EditorGUILayout.ObjectField("Debug GameObject Pos Change Prefab", _debugGameObjectPosChangePrefab, typeof(GameObject), false) as GameObject;
            GetManualPositionPrefab = EditorGUILayout.ObjectField("Manual Position Prefab", _manualPositionPrefab, typeof(GameObject), false) as GameObject;

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

        int totalCount = 0;
        if (Generator != null)
        {
            foreach (var activeContext in Generator.GetActiveCoverGenContexts(_currentCoverGenMode))
            {
                totalCount += DisplayPositions(activeContext.positionData.Positions);
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

        Draw3DCursor();
        Handle3DCursorInput();

        if (totalCount > 0)
        {
            Handles.BeginGUI();
            GUI.Label(new Rect(10, 30, 200, 20), $"Displaying {totalCount} positions");
            Handles.EndGUI();
        }
    }

    private void HandleUIChanges()
    {
        if (_currentGizmoViewMode != _lastGizmoViewMode)
        {
            GizmoViewChanged(_currentGizmoViewMode);
            _lastGizmoViewMode = _currentGizmoViewMode;
            UpdateVisibleCount();
        }

        if (_currentCoverGenMode != _lastCoverGenMode)
        {
            CoverGenTypeChanged(_currentCoverGenMode);
            _lastCoverGenMode = _currentCoverGenMode;
            UpdateVisibleCount();
        }

        Generator?.UpdateCornerFinder(GetCornerFinder);
    }

    private void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        Debug.Log($"Scene opened: {scene.name}. Resetting scene references.");
        ResetReferences();
        InitializeSceneParents();
        InitializePrefabObjects();
        Repaint();
    }

    private void ResetReferences()
    {
        _settings = null;
        _debugParentAdded = null;
        _debugParentRemoved = null;
        _debugParentModified = null;
        _debugGizmoGOParent = null;
        _manualPositionsParent = null;
        _gizmo3DCursor = null;
        _generator = null;
        _cornerFinder = null;
        _cornerFinderDebug = null;
        _positionValidator = null;
        _positionValidator = null;
        _positionValidatorDebug = null;
    }

    private void InitializePrefabObjects()
    {
        _ = GetDebugGizmoGOPrefab;
        _ = GetDebugGameObjectPosChangePrefab;
        _ = GetManualPositionPrefab;
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
        _manualPositionsParent = GetOrCreateChild(debugRoot.transform, "ManualPositions");
        _gizmo3DCursor = GetOrCreateChild(debugRoot.transform, "Gizmo3DCursor").transform;
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


    private void ComputeDifferences(List<TacticalPosition> oldPositions, List<TacticalPosition> newPositions)
    {
        if (!_createPosChangesDebugObjects)
            return;

        GameObject debugGO = null;
        if (_createPosChangesDebugObjects)
        {
            debugGO = GetDebugGameObjectPosChangePrefab;
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
        if (_debugGizmoGOParent == null) return;

        for (int i = 0; i < _debugGizmoGOParent.transform.childCount; i++)
        {
            GameObject childGO = _debugGizmoGOParent.transform.GetChild(i).gameObject;
            UpdateChildVisibility(viewMode, _currentCoverGenMode, childGO);
        }
    }

    private void CoverGenTypeChanged(TacticalPositionGenerator.CoverGenerationMode genMode)
    {
        if (_debugGizmoGOParent == null) return;

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
        UpdateVisibleCount();

        if (context != null)
        {
            if (context.cornerSettings.genMode == TacticalPositionGenerator.CoverGenerationMode.Manual)
                return;

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

        if (Generator != null && _distanceToCreateGizmos > 0)
        {
            Handles.color = new Color(1, 1, 0, 0.1f);
            Handles.DrawWireDisc(Gizmo3DCursor.position, Vector3.up, _distanceToCreateGizmos);
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

    private void Handle3DCursorInput()
    {
        if (_gizmo3DCursor == null || !IsWindowOpen)
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
            Repaint();
        }
    }
}