using FPSDemo.FPSController;
using System.Collections.Generic;
using UnityEngine;

public class DetectionDirectionUpdater : MonoBehaviour
{
	Dictionary<GameObject, DetectionDirectionFiller> targetsWatching = new();
	[SerializeField] Player player;
	GameObjectPooler detectionDirectionGUIPooler;

	void Awake()
	{
		detectionDirectionGUIPooler = GetComponent<GameObjectPooler>();
	}

	void Update()
	{
		UpdateDetectionGUIRotation();
	}

	public void RegisterNewTargetWatching(GameObject target)
	{
		if (!targetsWatching.ContainsKey(target))
		{
			GameObject directionGUI = detectionDirectionGUIPooler.GetPooledGO();
			targetsWatching.Add(target, directionGUI.GetComponent<DetectionDirectionFiller>());
		}
	}

	public void UpdateGUIFill(GameObject target, float newScale)
	{
		if (targetsWatching.TryGetValue(target, out DetectionDirectionFiller directionGUI))
		{
			directionGUI.UpdateFiller(newScale);
		}
	}

	public void UnregisterNewTargetWatching(GameObject target)
	{
		if (targetsWatching.TryGetValue(target, out DetectionDirectionFiller directionGUI))
		{
			directionGUI.gameObject.SetActive(false);
			targetsWatching.Remove(target);
		}
	}

	void UpdateDetectionGUIRotation()
	{
		foreach (GameObject target in targetsWatching.Keys)
		{
			Vector3 directionFromPlayer = target.transform.position - player.transform.position;
			Vector3 playerForward = player.transform.forward;
			directionFromPlayer.y = 0f;
			playerForward.y = 0f;
			if (targetsWatching.TryGetValue(target, out DetectionDirectionFiller guiToUpdate))
			{
				guiToUpdate.transform.rotation = Quaternion.Euler(0f, 0f, -Vector3.SignedAngle(playerForward, directionFromPlayer, Vector3.up));
			}
		}
	}
}
