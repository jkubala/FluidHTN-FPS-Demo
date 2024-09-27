
using UnityEngine;
using UnityEditor;
using FPSDemo.NPC;


[CustomEditor(typeof(ThirdPersonController))]
public class ThirdPersonControllerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		ThirdPersonController npcController = (ThirdPersonController)target;
		if (GUILayout.Button("Navigate to current player pos"))
		{
			npcController.NavigateToPlayer();
		}
		if (GUILayout.Button("Clear destination"))
		{
			npcController.SetDestination(null);
		}
		if (GUILayout.Button("Keep rotating to player"))
		{
			npcController.SetPlayerAsRotatePoint();
		}
		if (GUILayout.Button("Clear rotate to point"))
		{
			npcController.ClearRotateToPoint();
		}
		if (GUILayout.Button("Start shooting"))
		{
			npcController.StartShooting();
		}
		if (GUILayout.Button("Stop shooting"))
		{
			npcController.StopShooting();
		}
        if (GUILayout.Button("Reload"))
        {
            npcController.Reload();
        }
        if (GUILayout.Button("Run"))
		{
			npcController.SetRunSpeed();
		}
		if (GUILayout.Button("Walk"))
		{
			npcController.SetWalkSpeed();
		}
		if(GUILayout.Button("Crouch"))
		{
			npcController.Crouch();
		}
		if (GUILayout.Button("Uncrouch"))
		{
			npcController.Uncrouch();
		}
		DrawDefaultInspector();
	}
}
