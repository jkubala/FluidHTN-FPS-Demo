using UnityEditor;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
	[CustomEditor(typeof(TacticalPositionGenerator))]
	public class TacticalPositionGeneratorEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			TacticalPositionGenerator posGenerator = (TacticalPositionGenerator)target;

			if (GUILayout.Button("Generate tactical positions for AI"))
			{
				if (posGenerator.TacticalPositionData == null)
				{
					Debug.LogError("TacticalPositionData is missing!");
					return;
				}

				posGenerator.GenerateTacticalPositions();
				EditorUtility.SetDirty(posGenerator.TacticalPositionData);
				AssetDatabase.SaveAssetIfDirty(posGenerator.TacticalPositionData);
			}

			base.OnInspectorGUI();
		}
	}
}