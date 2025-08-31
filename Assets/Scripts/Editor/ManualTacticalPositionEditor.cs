using UnityEditor;
using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
    [CustomEditor(typeof(ManualPosition))]
    public class ManualTacticalPositionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ManualPosition manualPosGO = (ManualPosition)target;

            if (GUILayout.Button("Apply position and rotation"))
            {
                manualPosGO.tacticalPosition.Position = manualPosGO.transform.position;
                manualPosGO.tacticalPosition.mainCover.rotationToAlignWithCover = manualPosGO.transform.rotation;
            }

            base.OnInspectorGUI();
        }
    }
}
