using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class SnapperToolWindow : EditorWindow
{
	public enum GridType
	{
		Cartesian,
		Polar
	}

	const float TAU = Mathf.PI * 2;
	const string prefsPrefix = "YO_SNAPPER_TOOL_";

	[MenuItem("Tools/Snapper tool")]
	public static void OpenWindow()
	{
		GetWindow<SnapperToolWindow>("Snapper");
	}

	[SerializeField]
	[HideInInspector]
	private bool _enableCustomizeGrid;

	[SerializeField]
	[HideInInspector]
	private float _currentGridScale = 1f;

	[SerializeField]
	[HideInInspector]
	private int _currentAngularDivision = 24;

	[SerializeField]
	[HideInInspector]
	private bool _showGrid;

	[SerializeField]
	[HideInInspector]
	private float _gridDrawExtend = 16f;

	[SerializeField]
	[HideInInspector]
	private GridType _gridType;

	[SerializeField]
	[HideInInspector]
	private Vector3 _handlePoint;

	private SerializedObject _serializedObject;
	private SerializedProperty _enableCustomizeGridProperty;
	private SerializedProperty _currentGridScaleProperty;
	private SerializedProperty _currentAngularDivisionProperty;
	private SerializedProperty _showGridProperty;
	private SerializedProperty _gridDrawExtendProperty;
	private SerializedProperty _gridTypeProperty;
	private SerializedProperty _handlePointProperty;

	private void OnEnable()
	{
		SetupSerialization();

		LoadPrefs();

		Selection.selectionChanged += Repaint;
		// Update-loop of editor: EditorApplication.update;
		SceneView.duringSceneGui += DuringSceneGUI;
	}

	private void LoadPrefs()
	{
		_enableCustomizeGrid = EditorPrefs.GetBool($"{prefsPrefix}{nameof(_enableCustomizeGrid)}");
		_currentGridScale = EditorPrefs.GetFloat($"{prefsPrefix}{nameof(_currentGridScale)}");
		_currentAngularDivision = EditorPrefs.GetInt($"{prefsPrefix}{nameof(_currentAngularDivision)}");
		_showGrid = EditorPrefs.GetBool($"{prefsPrefix}{nameof(_showGrid)}");
		_gridDrawExtend = EditorPrefs.GetFloat($"{prefsPrefix}{nameof(_gridDrawExtend)}");
		_gridType = (GridType)EditorPrefs.GetInt($"{prefsPrefix}{nameof(_gridType)}");
		_handlePoint = EditorPrefs_GetVector3($"{prefsPrefix}{nameof(_handlePoint)}");
	}

	private Vector3 EditorPrefs_GetVector3(string key)
	{
		if(!EditorPrefs.HasKey($"{key}_{nameof(Vector3.x)}"))
		{
			return Vector3.zero;
		}

		return new Vector3(
			EditorPrefs.GetFloat($"{key}_{nameof(Vector3.x)}"),
			EditorPrefs.GetFloat($"{key}_{nameof(Vector3.y)}"),
			EditorPrefs.GetFloat($"{key}_{nameof(Vector3.z)}")
		);
	}

	private void SavePrefs()
	{
		EditorPrefs.SetBool($"{prefsPrefix}{nameof(_enableCustomizeGrid)}", _enableCustomizeGrid);
		EditorPrefs.SetFloat($"{prefsPrefix}{nameof(_currentGridScale)}", _currentGridScale);
		EditorPrefs.SetInt($"{prefsPrefix}{nameof(_currentAngularDivision)}", _currentAngularDivision);
		EditorPrefs.SetBool($"{prefsPrefix}{nameof(_showGrid)}", _showGrid);
		EditorPrefs.SetFloat($"{prefsPrefix}{nameof(_gridDrawExtend)}", _gridDrawExtend);
		EditorPrefs.SetInt($"{prefsPrefix}{nameof(_gridType)}", (int)_gridType);
		EditorPrefs_SetVector3($"{prefsPrefix}{nameof(_handlePoint)}", _handlePoint);
	}

	private void EditorPrefs_SetVector3(string key, Vector3 v)
	{
		EditorPrefs.SetFloat($"{key}_{nameof(Vector3.x)}", v.x);
		EditorPrefs.SetFloat($"{key}_{nameof(Vector3.y)}", v.y);
		EditorPrefs.SetFloat($"{key}_{nameof(Vector3.z)}", v.z);
	}

	private void SetupSerialization()
	{
		_serializedObject = new SerializedObject(this);
		_enableCustomizeGridProperty = _serializedObject.FindProperty(nameof(_enableCustomizeGrid));
		_currentGridScaleProperty = _serializedObject.FindProperty(nameof(_currentGridScale));
		_currentAngularDivisionProperty = _serializedObject.FindProperty(nameof(_currentAngularDivision));
		_showGridProperty = _serializedObject.FindProperty(nameof(_showGrid));
		_gridDrawExtendProperty = _serializedObject.FindProperty(nameof(_gridDrawExtend));
		_gridTypeProperty = _serializedObject.FindProperty(nameof(_gridType));
		_handlePointProperty = _serializedObject.FindProperty(nameof(_handlePoint));
	}

	//[SuppressMessage("ReSharper", "DelegateSubtraction")]
	private void OnDisable()
	{
		SavePrefs();

		Selection.selectionChanged -= Repaint;
		SceneView.duringSceneGui -= DuringSceneGUI;
	}

	private void DuringSceneGUI(SceneView scene)
	{
		_serializedObject.Update();
		_handlePointProperty.vector3Value = Handles.PositionHandle(_handlePointProperty.vector3Value, Quaternion.identity);
		_serializedObject.ApplyModifiedProperties();

		Color previousColor = Handles.color;
		CompareFunction previousZTest = Handles.zTest;

		try
		{
			if (Event.current.type == EventType.Repaint)
			{
				DrawGrid();
			}
		}
		finally
		{
			Handles.zTest = previousZTest;
			Handles.color = previousColor;
		}
	}

	private void DrawGrid()
	{
		if (_gridType == GridType.Cartesian)
		{
			DrawCartesianGrid();
		}
		else if (_gridType == GridType.Polar)
		{
			DrawPolarGrid();
		}
		else
		{
			// Not supported yet.
		}
	}

	private void DrawPolarGrid()
	{
		if (!_showGrid)
		{
			return;
		}

		Handles.zTest = CompareFunction.LessEqual;

		int ringCount = Mathf.RoundToInt(_gridDrawExtend / _currentGridScale);
		float gridSize = _enableCustomizeGrid ? _currentGridScale : 1f;
		float halfLength = ringCount * gridSize + gridSize / 2;

		for (int ringIndex = 1; ringIndex <= ringCount; ringIndex++)
		{
			Handles.DrawWireDisc(Vector3.zero, Vector3.up, ringIndex * gridSize);
		}

		// X pos = cos angle, Y = sin angle
		for (int angleIndex = 0; angleIndex < _currentAngularDivision; angleIndex++)
		{
			float t = angleIndex / (float) _currentAngularDivision;
			float angleRadians = t * TAU;
			float x = Mathf.Cos(angleRadians);
			float y = Mathf.Sin(angleRadians);
			Vector3 direction = new Vector3(x, 0, y);
			Handles.DrawAAPolyLine(Vector3.zero, direction * halfLength);
		}
	}

	private void DrawCartesianGrid()
	{
		if (!_showGrid)
		{
			return;
		}

		Handles.zTest = CompareFunction.LessEqual;

		int extendLineCount = Mathf.RoundToInt(_gridDrawExtend / _currentGridScale);
		float gridSize = _enableCustomizeGrid ? _currentGridScale : 1f;
		float halfLength = extendLineCount * gridSize + gridSize / 2;

		// Draw center cross.
		Handles.DrawAAPolyLine(new Vector3(-halfLength, 0, 0), new Vector3(+halfLength, 0, 0));
		Handles.DrawAAPolyLine(new Vector3(0, 0, -halfLength), new Vector3(0, 0, +halfLength));

		for (int extendIndex = 1; extendIndex <= extendLineCount; extendIndex++)
		{
			float offset = extendIndex * gridSize;
			// X-axis
			Handles.DrawAAPolyLine(new Vector3(offset, 0, -halfLength), new Vector3(offset, 0, +halfLength));
			Handles.DrawAAPolyLine(new Vector3(-offset, 0, -halfLength), new Vector3(-offset, 0, +halfLength));

			// Z-axis
			Handles.DrawAAPolyLine(new Vector3(-halfLength, 0, offset), new Vector3(+halfLength, 0, offset));
			Handles.DrawAAPolyLine(new Vector3(-halfLength, 0, -offset), new Vector3(+halfLength, 0, -offset));
		}
	}

	private void OnGUI()
	{
		_serializedObject.Update();
		EditorGUILayout.PropertyField(_enableCustomizeGridProperty, new GUIContent("Use customized property"));

		using (new EditorGUI.DisabledScope(!_enableCustomizeGrid))
		{
			EditorGUILayout.PropertyField(_gridTypeProperty, new GUIContent("Grid type"));

			if (_gridType == GridType.Cartesian)
			{
				EditorGUILayout.PropertyField(_currentGridScaleProperty, new GUIContent("Grid scale"));
				_currentGridScaleProperty.floatValue = Mathf.Max(.02f, _currentGridScaleProperty.floatValue);
				//EditorGUILayout.Slider(_currentGridScaleProperty, .01f, 100f, new GUIContent("Grid scale"));
			}
			else
			{
				EditorGUILayout.PropertyField(_currentGridScaleProperty, new GUIContent("Radial distance"));
				_currentGridScaleProperty.floatValue = Mathf.Max(.02f, _currentGridScaleProperty.floatValue);
				EditorGUILayout.PropertyField(_currentAngularDivisionProperty, new GUIContent("Radial segment count"));
				_currentAngularDivisionProperty.intValue = Mathf.Max(4, _currentAngularDivisionProperty.intValue);
			}
		}

		EditorGUILayout.PropertyField(_showGridProperty, new GUIContent("Show grid"));
		EditorGUILayout.PropertyField(_gridDrawExtendProperty, new GUIContent("Draw extend"));

		if (_serializedObject.ApplyModifiedProperties())
		{
			// Modified.
			SceneView.RepaintAll();
			//SceneView.currentDrawingSceneView.Repaint();
			//Repaint();
		}

		AddButton("Snap selection", CanSnapSelection, SnapSelection);
	}

	private bool CanSnapSelection()
	{
		return Selection.gameObjects.Any();
	}

	private void SnapSelection()
	{
		GameObject[] selection = Selection.gameObjects;

		foreach (GameObject go in selection)
		{
			Undo.RecordObject(go.transform, "Snapping Selection To Grid");
			Vector3 snappedPosition = GetSnappedPosition(go.transform.position);
			go.transform.position = snappedPosition;
		}
	}

	private Vector3 GetSnappedPosition(Vector3 originalPosition)
	{
		if (_gridType == GridType.Cartesian)
		{
			return GetCartesianSnapPosition(originalPosition);
		}
		
		if (_gridType == GridType.Polar)
		{
			return GetPolarSnapPosition(originalPosition);
		}

		// Add error handling
		return originalPosition;
	}

	private Vector3 GetPolarSnapPosition(Vector3 originalPosition)
	{
		Vector2 center = Vector3.zero;
		Vector2 plainPosition = new Vector2(originalPosition.x, originalPosition.z);
		float gridSize = _enableCustomizeGrid ? _currentGridScale : 1f;
		float distanceFromCenter = (plainPosition - center).magnitude;
		//float currentAngleDegree = Vector2.Angle(Vector2.right, plainPosition);
		float currentAngleRadian = Mathf.Atan2(plainPosition.y, plainPosition.x);
		float angleInTurns = currentAngleRadian / TAU;

		float snappedAngleInTurns = RoundTo(angleInTurns, 1f / _currentAngularDivision);
		float snappedAngleInRadians = snappedAngleInTurns * TAU;
		float newDistance = RoundTo(distanceFromCenter, gridSize);

		Vector2 snappedDirection = new Vector2(Mathf.Cos(snappedAngleInRadians), Mathf.Sin(snappedAngleInRadians));
		Vector2 newPlainPosition = snappedDirection * newDistance;

		return new Vector3(newPlainPosition.x, originalPosition.y, newPlainPosition.y);
	}

	private Vector3 GetCartesianSnapPosition(Vector3 originalPosition)
	{
		float gridSize = _enableCustomizeGrid ? _currentGridScale : 1f;
		return RoundTo(originalPosition, gridSize);
	}

	public static Vector3 RoundTo(Vector3 v, float step)
	{
		return new Vector3(
			RoundTo(v.x, step),
			RoundTo(v.y, step),
			RoundTo(v.z, step)
		);
	}

	private static float RoundTo(float value, float step)
	{
		return Mathf.Round(value / step) * step;
	}

	private bool AddButton(string label, Action action)
	{
		return AddButton(label, () => true, action);
	}

	private bool AddButton(string label, Func<bool> isEnabled, Action action)
	{
		using (new EditorGUI.DisabledScope(!isEnabled()))
		{
			bool isPressed = GUILayout.Button(label);

			if (isPressed)
			{
				action();
			}

			return isPressed;
		}
	}
}