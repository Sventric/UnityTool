using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class GrimmCannonWindow : EditorWindow
{
	const float TAU = Mathf.PI * 2;

	[MenuItem("Tools/Grimm Cannon")]
	public static void OpenWindow()
	{
		GetWindow<GrimmCannonWindow>("Grimm Cannon");
	}

	public bool ShowBrush = true;
	public float Radius = 2f;
	public int SpawnCount = 8;

	private SerializedObject _serializedObject;
	private SerializedProperty _serializedShowBrush;
	private SerializedProperty _serializedRadius;
	private SerializedProperty _serializedSpawnCount;

	private void SetupSerialization()
	{
		_serializedObject = new SerializedObject(this);
		_serializedShowBrush = _serializedObject.FindProperty(nameof(ShowBrush));
		_serializedRadius = _serializedObject.FindProperty(nameof(Radius));
		_serializedSpawnCount = _serializedObject.FindProperty(nameof(SpawnCount));
	}

	private Vector2[] _randomPoints = new Vector2[0];

	private void GenerateRandomPoints(int count, bool refresh)
	{
		if (!refresh && count <= _randomPoints.Length)
		{
			//Debug.Log($"Skip resize random points [{_randomPoints.Length} / {count}].");
			return;
		}

		//Debug.Log($"Create new set [{refresh}].");
		Vector2[] newSet = new Vector2[count];

		for (int idx = 0; idx < count; idx++)
		{
			if (refresh || idx >= _randomPoints.Length)
			{
				//Debug.Log($"Add new index: {idx}.");
				newSet[idx] = Random.insideUnitCircle;
			}
			else
			{
				//Debug.Log($"Copy old index: {idx}.");
				newSet[idx] = _randomPoints[idx];
			}
		}

		//Debug.Log("Apply new set.");
		_randomPoints = newSet;
	}

	private void OnEnable()
	{
		SetupSerialization();
		GenerateRandomPoints(SpawnCount, true);

		SceneView.duringSceneGui += DuringSceneGUI;
	}

	private void OnDisable()
	{
		SceneView.duringSceneGui -= DuringSceneGUI;
	}

	private void DuringSceneGUI(SceneView sceneView)
	{
		CompareFunction previousZTest = Handles.zTest;
		Color previousColor = Handles.color;

		try
		{
			ScatterPlacer(sceneView);
		}
		finally
		{
			Handles.color = previousColor;
			Handles.zTest = previousZTest;
		}
	}

	private void ScatterPlacer(SceneView sceneView)
	{
		if (!ShowBrush)
		{
			return;
		}

		Handles.zTest = CompareFunction.LessEqual;

		Transform cameraTransform = sceneView.camera.transform;

		if (Event.current.type == EventType.MouseMove)
		{
			// Repaint on mouse move.
			sceneView.Repaint();
		}

		// bool hasAlt = (Event.current.modifiers & EventModifiers.Alt) != 0

		if (Event.current.type == EventType.ScrollWheel && !Event.current.alt)
		{
			// How far did the mouse wheel moved.
			float scrollDirection = Event.current.delta.y; // +3 / -3
			// Debug.Log($"ScrollWheel: {scrollDirection}");

			_serializedObject.Update();
			_serializedRadius.floatValue *= 1 + -Mathf.Sign(scrollDirection) * .05f;

			if (_serializedObject.ApplyModifiedProperties())
			{
				Repaint();
			}

			// PreventPropagate
			Event.current.Use();
		}

		bool addPrefabs = Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Space;

		if (addPrefabs)
		{
			Debug.Log("Clicked... let's add.");
			// PreventPropagate
			Event.current.Use();
		}

		Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		//Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

		if (Physics.Raycast(ray, out RaycastHit hit))
		{
			//Handles.color = Color.magenta;
			//Handles.DrawAAPolyLine(5f, hit.point, hit.point + hit.normal);
			Handles.color = new Color(0, 0, 0, .5f);
			Handles.DrawSolidDisc(hit.point + hit.normal * .01f, hit.normal, Radius);

			// Determine local tangent space
			Vector3 hitNormal = hit.normal;
			Vector3 hitTangent = Vector3.Cross(hitNormal, cameraTransform.up).normalized;
			Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent)/*.normalized Not needed.*/;
			float topViewHeight = 5f;

			Ray GetTangentRay(Vector2 tangentSpacePosition)
			{
				Vector2 localPoint = tangentSpacePosition * Radius;
				Vector3 rayOrigin = hit.point + hitNormal * topViewHeight
				                              + hitTangent * localPoint.x
				                              + hitBitangent * localPoint.y;
				Vector3 radDirection = -hitNormal;
				return new Ray(rayOrigin, radDirection);
			}

			Handles.color = new Color(0, 0, 0, .5f);
			Handles.DrawSolidDisc(hit.point + topViewHeight * hit.normal, hit.normal, Radius);
			//DrawCircle(hit, Radius, Color.blue, 5f);
			Color baseColor = Color.yellow;
			Color tooSteep = Color.red;

			foreach (Vector2 point in _randomPoints.Take(SpawnCount))
			{
				/*
				Vector2 localPoint = point * Radius;
				Vector3 rayOrigin = hit.point + hitNormal * topViewHeight
				                        + hitTangent * localPoint.x
				                        + hitBitangent * localPoint.y;
				Vector3 radDirection = -hitNormal;
				//*/

				Ray placementRay = GetTangentRay(point);

				if (Physics.Raycast(placementRay, out RaycastHit placementHit))
				{
					// Skip when normal too much off.
					float angle = Vector3.Angle(hitNormal, placementHit.normal);
					Handles.color = Color.Lerp(baseColor, tooSteep, angle / 90);

					//Handles.color = new Color(1f, 1f, 0, .5f);
					Handles.SphereHandleCap(-1, placementHit.point, Quaternion.identity, .1f, EventType.Repaint);
					Handles.color = Color.black;
					Handles.DrawAAPolyLine(5f, placementHit.point, placementHit.point + placementHit.normal);

					if (angle > 30f)
					{
						// TOO Steep
						continue;
					}

					if (addPrefabs)
					{
						GameObject prefab = GetPrefab();
						prefab.name += $" ∠{angle}";
						//GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
						prefab.transform.position = placementHit.point;
						prefab.transform.up = placementHit.normal;
						prefab.transform.localRotation = Quaternion.Euler(new Vector3(0, Random.Range(0f, 360f), 0));
						// For tree, but not rocks:
						//   float size = 1 + Random.Range(-.1f, +.1f);
						//   float height = 1 + Random.Range(-.1f, +.1f);
						//   prefab.transform.localScale = new Vector3(size, height, size);
					}
				}
			}

			Handles.color = Handles.xAxisColor;
			Handles.DrawAAPolyLine(5f, hit.point, hit.point + hitTangent);

			Handles.color = Handles.zAxisColor;
			Handles.DrawAAPolyLine(5f, hit.point, hit.point + hitBitangent);

			Handles.color = Handles.yAxisColor;
			Handles.DrawAAPolyLine(5f, hit.point, hit.point + hitNormal);

			// Draw circle
			Handles.color = Color.blue;
			const int segmentCount = 256;
			Vector3[] circlePoints = new Vector3[segmentCount + 1];
			for (int i = 0; i < segmentCount; i++)
			{
				float t = i / (float)segmentCount;
				float radiusAngle = t * TAU;
				Vector2 direction = new Vector2(Mathf.Cos(radiusAngle), Mathf.Sin(radiusAngle));
				Ray circleRay = GetTangentRay(direction);

				if (Physics.Raycast(circleRay, out RaycastHit circleHit))
				{
					circlePoints[i] = circleHit.point + circleHit.normal * .01f;
				}
				else
				{
					Vector2 localPoint = direction * Radius;
					Vector3 circlePoint = hit.point
					                              + hitTangent * localPoint.x
					                              + hitBitangent * localPoint.y;
					circlePoints[i] = circlePoint;
				}
			}
			// Map the end to the beginning.
			circlePoints[segmentCount] = circlePoints[0];
			Handles.DrawAAPolyLine(6f, circlePoints);
		}
		else
		{
			// We are not above any surface.

			//Handles.color = Color.cyan;
			//float halfDistance = (cameraTransform.position / 2).magnitude;
			//Vector3 center = cameraTransform.position + cameraTransform.forward * halfDistance;
			//Handles.DrawSolidDisc(center, cameraTransform.forward * -1, 2);
		}
	}

	private GameObject GetPrefab()
	{
		float selectionValue = Random.value;

		foreach (PrefabEntry entry in Sets[_selectedSetIndex].Prefabs)
		{
			if (entry.OccurrenceFactor > selectionValue)
			{
				return Instantiate(entry.Prefab);
			}

			selectionValue -= entry.OccurrenceFactor;
		}

		return Instantiate(Sets[_selectedSetIndex].Prefabs.Last().Prefab);
	}

	private void SimpleScatterPlacer(SceneView sceneView)
	{
		Transform cameraTransform = sceneView.camera.transform;

		Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
		Handles.zTest = CompareFunction.LessEqual;

		if (Physics.Raycast(ray, out RaycastHit hit))
		{
			//Handles.color = Color.magenta;
			//Handles.DrawAAPolyLine(5f, hit.point, hit.point + hit.normal);
			Handles.color = new Color(0, 0, 0, .5f);
			Handles.DrawSolidDisc(hit.point, hit.normal, Radius);

			// Determine local tangent space
			Vector3 hitNormal = hit.normal;
			Vector3 hitTangent = Vector3.Cross(hitNormal, cameraTransform.up).normalized;
			Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent)/*.normalized Not needed.*/;

			float topViewHeight = 5f;
			Handles.color = new Color(0, 0, 0, .5f);
			Handles.DrawSolidDisc(hit.point + topViewHeight * hit.normal, hit.normal, Radius);

			foreach (Vector2 point in _randomPoints.Take(SpawnCount))
			{
				Vector2 localPoint = point * Radius;
				Vector3 rayOrigin = hit.point + hitNormal * topViewHeight
				                        + hitTangent * localPoint.x
				                        + hitBitangent * localPoint.y;
				Vector3 radDirection = -hitNormal;

				Ray placementRay = new Ray(rayOrigin, radDirection);

				if (Physics.Raycast(placementRay, out RaycastHit placementHit))
				{
					Handles.color = new Color(1f, 1f, 0, .5f);
					Handles.SphereHandleCap(-1, placementHit.point, Quaternion.identity, .1f, EventType.Repaint);
					Handles.color = Color.black;
					Handles.DrawAAPolyLine(5f, placementHit.point, placementHit.point + placementHit.normal);
				}
			}

			Handles.color = Handles.xAxisColor;
			Handles.DrawAAPolyLine(5f, hit.point, hit.point + hitTangent);

			Handles.color = Handles.zAxisColor;
			Handles.DrawAAPolyLine(5f, hit.point, hit.point + hitBitangent);

			Handles.color = Handles.yAxisColor;
			Handles.DrawAAPolyLine(5f, hit.point, hit.point + hitNormal);
		}
		else
		{
			Handles.color = Color.cyan;
			float halfDistance = (cameraTransform.position / 2).magnitude;
			Vector3 center = cameraTransform.position + cameraTransform.forward * halfDistance;
			Handles.DrawSolidDisc(center, cameraTransform.forward * -1, 2);
		}
	}

	public List<PrefabSet> Sets;
	public int _selectedSetIndex;
	private bool _openEditSet;

	private void OnGUI()
	{
		_serializedObject.Update();

		string[] options = Sets.Select(s => s.Name).ToArray();
		_selectedSetIndex = EditorGUILayout.Popup("Brush set", _selectedSetIndex, options);
		_openEditSet = EditorGUILayout.Foldout(_openEditSet, "Edit set");

		if (_selectedSetIndex < options.Length && _openEditSet)
		{
			using (new GUILayout.VerticalScope("Prefab set", EditorStyles.helpBox))
			{
				PrefabSet set = Sets[_selectedSetIndex];

				EditorGUILayout.Space(8f);
				set.Name = EditorGUILayout.TextField("Name", set.Name);

				for (int index = 0; index < set.Prefabs.Count; index++)
				{
					PrefabEntry prefabEntry = set.Prefabs[index];
					try
					{
						PrefabEntryGUI(prefabEntry);
					}
					catch (Exception exception)
					{
						Debug.LogError($"Failed rendering GUI for {set.Name}[{index}]");
						Debug.LogException(exception, prefabEntry.Prefab);
					}
				}

				/*

				AddButton("Normalize occurrence factors", () => set.Prefabs.Count > 0, () =>
				{
					float totalFactor = set.Prefabs.Sum(p => p.OccurrenceFactor);
					foreach (PrefabEntry entry in set.Prefabs)
					{
						entry.OccurrenceFactor /= totalFactor;
					}
				});

				//*/

				using (new EditorGUILayout.HorizontalScope())
				{
					AddButton("-", () => Debug.LogWarning($"Remove of prefab not implemented yet"));
					AddButton("+", () => set.Prefabs.Add(new PrefabEntry()));
				}
			}
		}

		using (new EditorGUILayout.HorizontalScope())
		{
			AddButton("Remove selected set.", () => Debug.LogWarning($"Remove of set not implemented yet"));
			AddButton("Add new set", () => Sets.Add(new PrefabSet { Name = $"Unnamed {Sets.Count + 1}"}));
		}

		EditorGUILayout.PropertyField(_serializedShowBrush);
		_serializedShowBrush.boolValue = _serializedShowBrush.boolValue;
		EditorGUILayout.PropertyField(_serializedRadius);
		_serializedRadius.floatValue = Mathf.Max(0f, _serializedRadius.floatValue);
		EditorGUILayout.PropertyField(_serializedSpawnCount);
		_serializedSpawnCount.intValue = Mathf.Max(1, _serializedSpawnCount.intValue);

		if (_serializedObject.ApplyModifiedProperties())
		{
			GenerateRandomPoints(SpawnCount, false);
			SceneView.RepaintAll();
		}

		if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			// Cancel focus from editor field.
			GUI.FocusControl(null);
			// Update window UI
			Repaint();
		}
	}

	private void PrefabEntryGUI(PrefabEntry prefabEntry)
	{
		using (new EditorGUILayout.HorizontalScope())
		{
			using (new EditorGUILayout.VerticalScope())
			{
				prefabEntry.Prefab = (GameObject) EditorGUILayout.ObjectField(new GUIContent("Prefab"), prefabEntry.Prefab, typeof(GameObject), false);
				float newFactor = EditorGUILayout.Slider("OccurrenceFactor", prefabEntry.OccurrenceFactor, 0f, 1f);
				if (Math.Abs(prefabEntry.OccurrenceFactor - newFactor) > 0.00001)
				{
					prefabEntry.OccurrenceFactor = newFactor;
					float totalFactor = Sets[_selectedSetIndex].Prefabs.Sum(p => p.OccurrenceFactor);
					foreach (PrefabEntry entry in Sets[_selectedSetIndex].Prefabs)
					{
						entry.OccurrenceFactor /= totalFactor;
					}
				}
			}

			if (prefabEntry.Prefab)
			{
				GUIContent content = new GUIContent(AssetPreview.GetAssetPreview(prefabEntry.Prefab));
				float size = content.image?.width ?? 0f; //  * 2.5f;
				EditorGUILayout.LabelField("", GUILayout.Width(size), GUILayout.Height(size));
				var rect = GUILayoutUtility.GetLastRect();
				EditorGUI.DrawPreviewTexture(new Rect(rect.x, rect.y, size, size), AssetPreview.GetAssetPreview(prefabEntry.Prefab));
			}
		}

		// WHAT DOES THIS DO?
		// var x = new PreviewRenderUtility();
		// x.AddSingleGO(prefabEntry.Prefab);
		// x.Render();
	}

	[Serializable]
	public class PrefabSet
	{
		public string Name = "Unnamed";
		public List<PrefabEntry> Prefabs = new List<PrefabEntry>();
	}

	[Serializable]
	public class PrefabEntry
	{
		public GameObject Prefab;
		public float OccurrenceFactor = 1f;
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