using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoadGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

//		AddButton("Add circle", AddCircle);
	}

//	override onsc
//
//	private void AddCircle()
//	{
//		// Handles.CircleHandleCap();
//		HandleUtility.GUIPointToWorldRay(Event.current.mousePosition.x, )
//	}

	private bool AddButton(string label, Action action)
	{
		bool isClicked = GUILayout.Button(label);

		if (isClicked)
		{
			action();
		}

		return isClicked;
	}
}