using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MiscType))]
public class MiscTypeEditor : Editor
{
	//[SerializeField]
	//[HideInInspector]

	private SerializedProperty _colorProperty;

	public override void OnInspectorGUI()
	{
		// Explicit positioning:
		//  - GUI
		//  - EditorGUI
		// Implicit positioning:
		//  - GUILayout
		//  - EditorGUILayout

		MiscType mt = (MiscType)target;

		EditorGUILayout.LabelField("Welkom");
		EditorGUILayout.LabelField($"Count: {mt.Properties.Count}");

		base.OnInspectorGUI();

		if (mt.Properties.Count == 0)
		{
			AddErrorLabel($"No properties available.");

			if (GUILayout.Button("Populate"))
			{
				mt.Fill();
			}
		}

		foreach (MiscType.MiscProperty property in mt.Properties.Values)
		{
			using (new GUILayout.VerticalScope("VerticalScope", EditorStyles.helpBox))
			{
				GUILayout.Space(10);
				using (var @scope = new GUILayout.HorizontalScope())
				{
					GUILayout.Label(property.Name, GUILayout.Width(EditorGUIUtility.labelWidth));

					if (property.Value is float floatValue)
					{
						GUILayout.HorizontalSlider(floatValue, -1f, 1f);
					}
					else
					{
						AddErrorLabel($"Type '{property.Type.Name}' not supported.");
					}
				}

				EditorGUILayout.ObjectField("Object field: ", null, typeof(ExplosiveBarrel), true);
			}
		}
	}

	private void AddErrorLabel(string message)
	{
		Color previousContentColor = GUI.contentColor;
		GUI.contentColor = Color.red;
		// GUI.skin.box
		// EditorStyles.boldLabel
		GUILayout.Label(message, GUI.skin.box);
		GUI.contentColor = previousContentColor;
	}
}