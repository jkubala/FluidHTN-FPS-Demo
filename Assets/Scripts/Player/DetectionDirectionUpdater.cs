
using System.Collections.Generic;
using FPSDemo.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace FPSDemo.Player
{
    public class DetectionDirectionUpdater : MonoBehaviour
    {
        // ========================================================= INSPECTOR FIELDS

        [SerializeField] private Player _player;
        [SerializeField] private GameObjectPooler _detectionDirectionGUIPooler;


        // ========================================================= PRIVATE FIELDS

        private Dictionary<GameObject, DetectionDirectionFiller> _targetsWatching = new();
        

        // ========================================================= UNITY METHODS

        private void OnValidate()
        {
            if (_detectionDirectionGUIPooler == null)
            {
                _detectionDirectionGUIPooler = GetComponent<GameObjectPooler>();
            }
        }

        private void Update()
        {
            UpdateDetectionGUIRotation();
        }


        // ========================================================= PUBLIC METHODS

        public void RegisterNewTargetWatching(GameObject target)
        {
            if (!_targetsWatching.ContainsKey(target))
            {
                GameObject directionGUI = _detectionDirectionGUIPooler.GetPooledGO();
                _targetsWatching.Add(target, directionGUI.GetComponent<DetectionDirectionFiller>());
            }
        }

        public void UpdateGUIFill(GameObject target, float newScale)
        {
            if (_targetsWatching.TryGetValue(target, out DetectionDirectionFiller directionGUI))
            {
                directionGUI.UpdateFiller(newScale);
            }
        }

        public void UnregisterNewTargetWatching(GameObject target)
        {
            if (_targetsWatching.TryGetValue(target, out DetectionDirectionFiller directionGUI))
            {
                directionGUI.gameObject.SetActive(false);
                _targetsWatching.Remove(target);
            }
        }


        // ========================================================= PRIVATE METHODS

        private void UpdateDetectionGUIRotation()
        {
            foreach (GameObject target in _targetsWatching.Keys)
            {
                Vector3 directionFromPlayer = target.transform.position - _player.transform.position;
                Vector3 playerForward = _player.transform.forward;
                directionFromPlayer.y = 0f;
                playerForward.y = 0f;
                if (_targetsWatching.TryGetValue(target, out DetectionDirectionFiller guiToUpdate))
                {
                    guiToUpdate.transform.rotation = Quaternion.Euler(0f, 0f,
                        -Vector3.SignedAngle(playerForward, directionFromPlayer, Vector3.up));
                }
            }
        }
    }
}
