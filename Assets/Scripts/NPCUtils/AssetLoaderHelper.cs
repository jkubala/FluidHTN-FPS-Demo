using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPSDemo.NPC.Utilities
{
    public static class AssetLoaderHelper
    {
        private const string BASE_PATH = "Assets/Content/NPCUtils/TacticalGrid";

        /// <summary>
        /// Gets a prefab from the specified path, creating the folder structure if needed
        /// </summary>
        public static GameObject GetPrefab(string relativePath, string prefabName)
        {
            string fullPath = $"{BASE_PATH}/{relativePath}";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);

            if (prefab == null)
            {
                EnsureFolderExists(fullPath);
                Debug.LogError($"No {prefabName} prefab found at {fullPath}!");
            }

            return prefab;
        }

        /// <summary>
        /// Gets or creates a GameObject in the scene with the specified hierarchy
        /// </summary>
        public static GameObject GetOrCreateSceneObject(string rootName, params string[] childPath)
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null)
            {
                root = new(rootName);
                Undo.RegisterCreatedObjectUndo(root, $"Create {rootName}");
            }

            GameObject current = root;

            foreach (string childName in childPath)
            {
                Transform childTransform = current.transform.Find(childName);
                if (childTransform == null)
                {
                    GameObject child = new(childName);
                    Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
                    child.transform.SetParent(current.transform);
                    current = child;
                }
                else
                {
                    current = childTransform.gameObject;
                }
            }

            return current;
        }

        /// <summary>
        /// Creates settings for the current scene if they don't exist
        /// </summary>
        public static TacticalGeneratorSettings GetOrCreateSettings()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            string settingsPath = $"{BASE_PATH}/{sceneName}/{sceneName}TacticalGeneratorSettings.asset";

            var settings = AssetDatabase.LoadAssetAtPath<TacticalGeneratorSettings>(settingsPath);

            if (settings == null)
            {
                EnsureFolderExists($"{BASE_PATH}/{sceneName}");

                settings = ScriptableObject.CreateInstance<TacticalGeneratorSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
                AssetDatabase.SaveAssets();

                Debug.Log($"Created default TacticalGeneratorSettings for scene {sceneName}");
            }

            return settings;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string[] pathParts = folderPath.Split('/');
                string currentPath = pathParts[0];

                for (int i = 1; i < pathParts.Length; i++)
                {
                    string nextPath = $"{currentPath}/{pathParts[i]}";
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, pathParts[i]);
                    }
                    currentPath = nextPath;
                }
            }
        }
    }
}