using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(BarrelType))]
public class BarrelTypeEditor : Editor
{
	//[SerializeField]
	//[HideInInspector]

	private SerializedProperty _radiusProperty;
	private SerializedProperty _damageProperty;
	private SerializedProperty _colorProperty;

	private void OnEnable()
	{
		_radiusProperty = serializedObject.FindProperty(nameof(BarrelType.Radius));
		_damageProperty = serializedObject.FindProperty(nameof(BarrelType.Damage));
		_colorProperty = serializedObject.FindProperty(nameof(BarrelType.ExplosionColor));
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(_radiusProperty);
		EditorGUILayout.PropertyField(_damageProperty);
		EditorGUILayout.PropertyField(_colorProperty);

		if (serializedObject.ApplyModifiedProperties())
		{
			// Something has changed.
			foreach (ExplosiveBarrel barrel in FindObjectsOfType<ExplosiveBarrel>())
			{
				barrel.TryApplyColor();
			}
		}
	}

	public /*override*/ void OnInspectorGUI_previous()
	{
		BarrelType barrel = (BarrelType) target;

		// Use 'targets' when [CanEditMultipleObjects]

		// Direct editing without Undo.
		barrel.Radius = EditorGUILayout.FloatField("Radius", barrel.Radius);
		
		// Using the undo system, is a better way.
		float newDamage = EditorGUILayout.FloatField("Damage", barrel.Damage);
		if (Math.Abs(newDamage - barrel.Damage) > .001f)
		{
			Undo.RecordObject(barrel, "Change barrel radius");
			barrel.Damage = newDamage;
		}

		barrel.ExplosionColor = EditorGUILayout.ColorField("Color", barrel.ExplosionColor);
	}
}