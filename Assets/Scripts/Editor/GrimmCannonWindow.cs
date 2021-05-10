using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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
	public LayerMask SurfaceLayers;
	public GameObject Container;

	[SerializeField]
	private List<PrefabSet> _sets;
	[SerializeField]
	private int _selectedSetIndex;
	[SerializeField]
	private bool _openEditSet;

	private SerializedObject _serializedObject;
	private SerializedProperty _serializedShowBrush;
	private SerializedProperty _serializedRadius;
	private SerializedProperty _serializedSpawnCount;
	private SerializedProperty _serializedSurfaceLayers;
	private SerializedProperty _serializedContainer;

	private SerializedProperty _serializedPrefabSets;
	private SerializedProperty _serializedSelectedSetIndex;
	private SerializedProperty _serializedOpenEditSet;

	private void SetupSerialization()
	{
		_serializedObject = new SerializedObject(this);
		_serializedShowBrush = _serializedObject.FindProperty(nameof(ShowBrush));
		_serializedRadius = _serializedObject.FindProperty(nameof(Radius));
		_serializedSpawnCount = _serializedObject.FindProperty(nameof(SpawnCount));
		_serializedSurfaceLayers = _serializedObject.FindProperty(nameof(SurfaceLayers));
		_serializedContainer = _serializedObject.FindProperty(nameof(Container));

		_serializedPrefabSets = _serializedObject.FindProperty(nameof(_sets));
		_serializedSelectedSetIndex = _serializedObject.FindProperty(nameof(_selectedSetIndex));
		_serializedOpenEditSet = _serializedObject.FindProperty(nameof(_openEditSet));
	}

	private PlacementLocation[] _randomLocations = new PlacementLocation[0];

	private IEnumerable<PlacementLocation> RandomLocations
	{
		get { return _randomLocations.Take(SpawnCount); }
	}

	private class PlacementLocation
	{
		public Vector2 UnitLocation;
		public int PrefabIndex;
		public Vector3 WorldPosition;
		public Vector3 Normal;
		public bool HitSurface;
		public Quaternion Rotation;
		public Quaternion Orientation;
	}

	private void GenerateRandomLocations(int count, bool refresh)
	{
		if (!refresh && count <= _randomLocations.Length)
		{
			//Debug.Log($"Skip resize random points [{_randomPoints.Length} / {count}].");
			return;
		}

		//Debug.Log($"Create new set [{refresh}].");
		PlacementLocation[] newSet = new PlacementLocation[count];

		for (int idx = 0; idx < count; idx++)
		{
			if (refresh || idx >= _randomLocations.Length)
			{
				//Debug.Log($"Add new index: {idx}.");
				newSet[idx] = new PlacementLocation
				{
					UnitLocation = Random.insideUnitCircle,
					PrefabIndex = GetRandomPrefabIndex(),
					Orientation = Quaternion.Euler(0, Random.value * 360, 0)
				};
			}
			else
			{
				//Debug.Log($"Copy old index: {idx}.");
				newSet[idx] = _randomLocations[idx];
			}
		}

		//Debug.Log("Apply new set.");
		_randomLocations = newSet;
	}

	[UsedImplicitly /* Unity method */]
	private void OnEnable()
	{
		SetupSerialization();
		GenerateRandomLocations(SpawnCount, true);

		SceneView.duringSceneGui += DuringSceneGUI;
	}

	[UsedImplicitly /* Unity method */]
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

	private TangentSpace _lastHitTangentSpace;
	private float _rayElevationHeight = 5f;

	private void ScatterPlacer(SceneView sceneView)
	{
		if (!ShowBrush)
		{
			return;
		}

		if (UpdateSceneView(sceneView))
		{
			return;
		}

		if (UpdateRadius())
		{
			return;
		}

		//TryPlaceObjects();
		if (PlaceObjects())
		{
			return;
		}

		if (DetermineLayout(sceneView))
		{
			return;
		}

		UpdateSceneUI();
	}

	private PrefabSet ActiveSet
	{
		get
		{
			if (_sets == null || _selectedSetIndex >= _sets.Count)
			{
				return null;
			}

			return _sets[_selectedSetIndex];
		}
	}

	private GameObject GetActivePrefab(int index)
	{
		PrefabSet set = ActiveSet;

		if (index < 0 || set?.Prefabs == null || index >= set.Prefabs.Count)
		{
			return null;
		}

		return set.Prefabs[index].Prefab;
	}

	private void UpdateSceneUI()
	{
		if (Event.current.type != EventType.Repaint || _lastHitTangentSpace == null)
		{
			return;
		}

		Handles.zTest = CompareFunction.LessEqual;

		// Draw the current situation.
		_lastHitTangentSpace.DrawHandles();

		// The tangent plane.
		// Handles.color = new Color(0, 0, 0, .5f);
		// Handles.DrawSolidDisc(hit.point + hit.normal * .01f, hit.normal, Radius);
		// The ray plane.
		// Handles.color = new Color(0, 0, 0, .5f);
		// Handles.DrawSolidDisc(hit.point + _rayElevationHeight * hit.normal, hit.normal, Radius);

		DrawBrush(Color.blue, 6f, 256);

		foreach (PlacementLocation location in RandomLocations.Where(rl => rl.HitSurface))
		{
			// Skip when normal too much off.
			float angle = Vector3.Angle(_lastHitTangentSpace.Up, location.Normal);
			float thresholdAngle = 30;
			GameObject activePrefab = GetActivePrefab(location.PrefabIndex);

			if (activePrefab == null)
			{
				Color handleColor = GetAngleColor(angle, thresholdAngle);
				// Draw a dot where a placement will occur.
				Handles.color = handleColor;
				Handles.SphereHandleCap(-1, location.WorldPosition, Quaternion.identity, .1f, EventType.Repaint);
				// Draw the local normal
				Handles.color = Color.black;
				Handles.DrawAAPolyLine(5f, location.WorldPosition, location.WorldPosition + location.Normal);
			}
			else
			{
				if (angle > thresholdAngle)
				{
					continue;
				}

				RenderModelAtLocation(activePrefab, location);
			}
		}
	}

	private void RenderModelAtLocation(GameObject activePrefab, PlacementLocation location)
	{
		Matrix4x4 rootOrientationMatrix = Matrix4x4.TRS(location.WorldPosition, location.Rotation, Vector3.one); //, activePrefab.transform.localScale);
		MeshFilter[] filters = activePrefab.GetComponentsInChildren<MeshFilter>();

		foreach (MeshFilter filter in filters)
		{
			Matrix4x4 localOrientation = filter.transform.localToWorldMatrix;
			Matrix4x4 worldOrientation = rootOrientationMatrix * localOrientation;

			MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
			Material material = renderer.sharedMaterial;
			material.SetPass(0);

			Mesh mesh = filter.sharedMesh;
			Graphics.DrawMeshNow(mesh, worldOrientation);
		}


		//Material material = activePrefab.GetComponent<MeshRenderer>().sharedMaterial;
		//material.SetPass(0);
		//
		//Mesh mesh = activePrefab.GetComponent<MeshFilter>().sharedMesh;
		//Graphics.DrawMeshNow(mesh, rootOrientationMatrix);
	}

	private bool _isPlacing = false;
	private Vector2 _lastPlacementPosition;

	private bool TryPlaceObjects()
	{
		bool result = false;
		bool allowPlacement = false;

		if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			_isPlacing = true;
			result = true;
			allowPlacement = true;
			_lastPlacementPosition = Event.current.mousePosition;
			Debug.Log($"Start placement: {_lastPlacementPosition}.");
		}
		else if (Event.current.type == EventType.MouseUp && _isPlacing)
		{
			_isPlacing = false;
			allowPlacement = true;
			result = true;
			Debug.Log($"Stopping placement: {Event.current.mousePosition}");
		}
		else if (Event.current.type == EventType.MouseDrag && _isPlacing)
		{
			result = true;
			float distance = (Event.current.mousePosition - _lastPlacementPosition).magnitude;
			Debug.Log($"Drag: {distance}");
		}

		if (_isPlacing && allowPlacement)
		{
		}

		if (result)
		{
			Event.current.Use();
		}

		return result;
	}

	private bool PlaceObjects()
	{
		if (Event.current.type != EventType.KeyDown)
		{
			return false;
		}

		Event.current.Use();

		if (_lastHitTangentSpace == null)
		{
			// No location to place something.
			// But it's handled.
			return true;
		}

		if (Event.current.keyCode != KeyCode.Space)
		{
			// Currently only space is used, ignore the reset...
			// But it's handled.
			return true;
		}

		foreach (PlacementLocation location in RandomLocations.Where(rl => rl.HitSurface))
		{
			// Skip when normal too much off.
			float angle = Vector3.Angle(_lastHitTangentSpace.Up, location.Normal);
			float thresholdAngle = 45;

			if (angle > thresholdAngle)
			{
				// TOO Steep
				continue;
			}

			GameObject prefab = GetPrefab();
			Undo.RegisterCreatedObjectUndo(prefab, "Spawn Objects");
			prefab.name += $" ∠{angle}";
			prefab.transform.position = location.WorldPosition;
			prefab.transform.rotation = location.Rotation;

			if (Container)
			{
				prefab.transform.SetParent(Container.transform, true);
			}
		}

		GenerateRandomLocations(SpawnCount, true);

		return true;
	}

	private int GetRandomPrefabIndex()
	{
		PrefabSet set = ActiveSet;

		if (set?.Prefabs == null || set.Prefabs.Count == 0)
		{
			return -1;
		}

		IList<PrefabEntry> activePrefabSet = set.Prefabs;
		float selectionValue = Random.value;

		for (int index = 0; index < activePrefabSet.Count; index++)
		{
			PrefabEntry entry = activePrefabSet[index];

			if (entry.OccurrenceFactor > selectionValue)
			{
				return index;
			}

			selectionValue -= entry.OccurrenceFactor;
		}

		return activePrefabSet.Count - 1;
	}

	private GameObject GetPrefab()
	{
		float selectionValue = Random.value;

		foreach (PrefabEntry entry in _sets[_selectedSetIndex].Prefabs)
		{
			if (entry.OccurrenceFactor > selectionValue)
			{
				return (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab);
			}

			selectionValue -= entry.OccurrenceFactor;
		}

		return (GameObject)PrefabUtility.InstantiatePrefab(_sets[_selectedSetIndex].Prefabs.Last().Prefab);
	}

	private bool DetermineLayout(SceneView sceneView)
	{
		if (Event.current.type != EventType.Layout)
		{
			return false;
		}

		Transform cameraTransform = sceneView.camera.transform;
		Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		bool validSurface = false;
		bool hitSurface = Physics.Raycast(ray, out RaycastHit hit);

		if (hitSurface)
		{
			GameObject surfaceObject = hit.transform.gameObject;
			int surfaceLayer = surfaceObject.layer;
			int surfaceMask = 1 << surfaceLayer;
			validSurface = (SurfaceLayers.value & surfaceMask) == surfaceMask;
		}

		if(validSurface)
		{
			_lastHitTangentSpace = TangentSpace.CreateFromUp(hit.point, hit.normal, cameraTransform.up);

			UpdatePlacementLocations();
		}
		else
		{
			_lastHitTangentSpace = null;
		}

		return true;

	}

	private void UpdatePlacementLocations()
	{
		foreach (PlacementLocation location in RandomLocations.ToArray())
		{
			// Scale the point in the unit-circle to a circle of specified radius.
			Vector2 localPoint = location.UnitLocation * Radius;
			Ray placementRay = _lastHitTangentSpace.GetTangentRay(localPoint, _rayElevationHeight);
			bool validSurface = false;
			bool hitSurface = Physics.Raycast(placementRay, out RaycastHit placementHit);

			if (hitSurface)
			{
				GameObject surfaceObject = placementHit.transform.gameObject;
				int surfaceLayer = surfaceObject.layer;
				int surfaceMask = 1 << surfaceLayer;
				validSurface = (SurfaceLayers.value & surfaceMask) == surfaceMask;
			}

			if (validSurface)
			{
				// Align the object with the Normal vector of the local plane.
				Quaternion rotation = Quaternion.FromToRotation(Vector3.up, location.Normal);
				// Give it a random rotation around the Normal vector.
				rotation *= location.Orientation; // Quaternion.Euler(0, Random.value * 360, 0);

				location.HitSurface = true;
				location.WorldPosition = placementHit.point;
				location.Normal = placementHit.normal;
				location.Rotation = rotation;
			}
			else
			{
				location.HitSurface = false;
#if DEBUG
				location.WorldPosition = Vector3.zero;
				location.Normal = Vector3.zero;
#endif
			}
		}
	}

	private void DrawBrush(Color color, float width, int segmentCount)
	{
		if (_lastHitTangentSpace == null)
		{
			return;
		}

		// Draw circle
		Handles.color = color;
		Vector3[] circlePoints = new Vector3[segmentCount + 1];

		for (int i = 0; i < segmentCount; i++)
		{
			float t = i / (float)segmentCount;
			float radiusAngle = t * TAU;
			Vector2 direction = new Vector2(Mathf.Cos(radiusAngle), Mathf.Sin(radiusAngle));
			Vector2 location = direction * Radius;
			Ray ray = _lastHitTangentSpace.GetTangentRay(location, _rayElevationHeight);

			if (Physics.Raycast(ray, out RaycastHit hit, 50f, SurfaceLayers.value))
			{
				circlePoints[i] = hit.point + hit.normal * .01f;
			}
			else
			{
				Vector3 circlePoint = _lastHitTangentSpace.ConvertToWorldSpace(location);
				circlePoints[i] = circlePoint;
			}
		}

		// Map the end to the beginning.
		circlePoints[segmentCount] = circlePoints[0];
		Handles.DrawAAPolyLine(width, circlePoints);
	}

	private bool UpdateSceneView(SceneView sceneView)
	{
		if (Event.current.type != EventType.MouseMove)
		{
			return false;
		}

		// Repaint on mouse move.
		sceneView.Repaint();
		return true;
	}

	private bool UpdateRadius()
	{
		if (Event.current.type != EventType.ScrollWheel || Event.current.alt)
		{
			return false;
		}

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

		return true;
	}

	private Color GetAngleColor(float angle, float thresholdAngle)
	{
		if (angle > thresholdAngle) return Color.red;

		float safe = thresholdAngle / 3;
		float edge = safe * 2;

		if (angle <= safe)
		{
			return Color.green;
		}

		if (angle <= edge)
		{
			return Color.Lerp(Color.green, Color.yellow, (angle - safe) / safe);
		}

		return Color.Lerp(Color.yellow, Color.red, (angle - edge) / safe);
	}

	private void OnGUI()
	{
		_serializedObject.Update();


		/*
		 _serializedPrefabSets;
		 _serializedSelectedSetIndex;
		 _serializedOpenEditSet;
		//*/
		bool hasSelectedBrush = DrawBrushSetDropdown();

		_serializedOpenEditSet.boolValue = EditorGUILayout.Foldout(_serializedOpenEditSet.boolValue, "Edit set");

		if (hasSelectedBrush && _serializedOpenEditSet.boolValue)
		{
			DrawBrushSetDetails();
		}

		using (new EditorGUILayout.HorizontalScope())
		{
			AddButton("Remove selected set.", () => Debug.LogWarning($"Remove of set not implemented yet"));
			AddButton("Add new set", () => _sets.Add(new PrefabSet { Name = $"Unnamed {_sets.Count + 1}" }));
		}

		EditorGUILayout.PropertyField(_serializedShowBrush);
		_serializedShowBrush.boolValue = _serializedShowBrush.boolValue;
		EditorGUILayout.PropertyField(_serializedRadius);
		_serializedRadius.floatValue = Mathf.Max(0f, _serializedRadius.floatValue);
		EditorGUILayout.PropertyField(_serializedSpawnCount);
		_serializedSpawnCount.intValue = Mathf.Max(1, _serializedSpawnCount.intValue);
		EditorGUILayout.PropertyField(_serializedSurfaceLayers);
		EditorGUILayout.PropertyField(_serializedContainer);

		if (_serializedObject.ApplyModifiedProperties())
		{
			GenerateRandomLocations(SpawnCount, false);
			SceneView.RepaintAll();
		}

		if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			// Cancel focus from editor field.
			GUI.FocusControl(null);
			// Update window UI
			Repaint();
		}

		if (Container)
		{
			AddButton("Reset", () =>
			{
				GameObject[] children = Container.GetAllChildren(t => t.gameObject).ToArray();
				foreach (GameObject child in children)
				{
					DestroyImmediate(child);
				}
			});
		}
	}

	private void DrawBrushSetDetails()
	{
		using (new GUILayout.VerticalScope("Prefab set", EditorStyles.helpBox))
		{
			// Prevent the first field to overlap the name of the scope.
			EditorGUILayout.Space(8f);

			SerializedProperty selectedSetProperty = _serializedPrefabSets.GetArrayElementAtIndex(_serializedSelectedSetIndex.intValue);
			SerializedProperty nameProperty = selectedSetProperty.FindPropertyRelative(nameof(PrefabSet.Name));

			EditorGUILayout.PropertyField(nameProperty);

			if (nameProperty.stringValue == string.Empty)
			{
				nameProperty.stringValue = "Unnamed set";
			}

			// Normal correction
			SerializedProperty normalCorrectionMinProperty = selectedSetProperty.FindPropertyRelative(nameof(PrefabSet.NormalCorrectionMin));
			SerializedProperty normalCorrectionMaxProperty = selectedSetProperty.FindPropertyRelative(nameof(PrefabSet.NormalCorrectionMax));
			float normalCorrectionMinFactor = normalCorrectionMinProperty.floatValue;
			float normalCorrectionMaxFactor = normalCorrectionMaxProperty.floatValue;
			EditorGUILayout.MinMaxSlider(new GUIContent("Normal correction", "A value of 1 will use the normal of the object and 0 will use the normal of the surface at the placement."), ref normalCorrectionMinFactor, ref normalCorrectionMaxFactor, 0, 1);
			normalCorrectionMinProperty.floatValue = normalCorrectionMinFactor;
			normalCorrectionMaxProperty.floatValue = normalCorrectionMaxFactor;

			SerializedProperty easeMethodProperty = selectedSetProperty.FindPropertyRelative(nameof(PrefabSet.EaseMethod));
			EditorGUILayout.PropertyField(easeMethodProperty);

			SerializedProperty prefabsProperty = selectedSetProperty.FindPropertyRelative(nameof(PrefabSet.Prefabs));
			DrawBrushElements(prefabsProperty);

			// Disable old way
			//PrefabSet set = _sets[_selectedSetIndex];
			//for (int index = 0; index < set.Prefabs.Count; index++)
			//{
			//	PrefabEntry prefabEntry = set.Prefabs[index];
			//	try
			//	{
			//		PrefabEntryGUI(prefabEntry);
			//	}
			//	catch (Exception exception)
			//	{
			//		Debug.LogError($"Failed rendering GUI for {set.Name}[{index}]");
			//		Debug.LogException(exception, prefabEntry.Prefab);
			//	}
			//}

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
				AddButton("+", () =>
				{
					prefabsProperty.InsertArrayElementAtIndex(prefabsProperty.arraySize);
					//set.Prefabs.Add(new PrefabEntry());
					OnSetChanged();
				});
			}
		}
	}

	private void DrawBrushElements(SerializedProperty prefabsProperty)
	{
		if(prefabsProperty == null) throw new ArgumentNullException(nameof(prefabsProperty));
		if(!prefabsProperty.isArray) throw new ArgumentException("Given property is not an array.", nameof(prefabsProperty));

		if (prefabsProperty.arraySize == 0)
		{
			EditorGUILayout.LabelField("No elements in set.", ToolGui.Warning, GUILayout.ExpandWidth(true));
		}
		else
		{
			for (int idx = 0; idx < prefabsProperty.arraySize; idx++)
			{
				SerializedProperty elementProperty = prefabsProperty.GetArrayElementAtIndex(idx);
				DrawBrushElement(elementProperty);
			}
		}
	}

	private void DrawBrushElement(SerializedProperty elementProperty)
	{
		using (new EditorGUILayout.HorizontalScope())
		{
			GameObject prefab = null;
			using (new EditorGUILayout.VerticalScope())
			{
				// Prefab
				SerializedProperty prefabProperty = elementProperty.FindPropertyRelative(nameof(PrefabEntry.Prefab));
				prefab = (GameObject)prefabProperty.objectReferenceValue;
				prefabProperty.objectReferenceValue = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Prefab"), prefab, typeof(GameObject), false);

				// Occurrence factor
				SerializedProperty occurrenceFactorProperty = elementProperty.FindPropertyRelative(nameof(PrefabEntry.OccurrenceFactor));
				float newOccurrenceFactor = EditorGUILayout.Slider("Occurrence factor", occurrenceFactorProperty.floatValue, 0f, 1f);
				if (Math.Abs(occurrenceFactorProperty.floatValue - newOccurrenceFactor) > 0.00001)
				{
					occurrenceFactorProperty.floatValue = newOccurrenceFactor;
					// TODO: Recalculate values.
					//float totalFactor = _sets[_selectedSetIndex].Prefabs.Sum(p => p.OccurrenceFactor);
					//foreach (PrefabEntry entry in _sets[_selectedSetIndex].Prefabs)
					//{
					//	entry.OccurrenceFactor /= totalFactor;
					//}
					OnSetChanged();
				}
			}

			if (prefab)
			{
				Texture2D previewTexture = AssetPreview.GetAssetPreview(prefab);
				GUIContent content = new GUIContent(previewTexture);
				float size = content.image?.width ?? 0f; //  * 2.5f;
				EditorGUILayout.LabelField("", GUILayout.Width(size), GUILayout.Height(size));
				var rect = GUILayoutUtility.GetLastRect();
				EditorGUI.DrawPreviewTexture(new Rect(rect.x, rect.y, size, size), previewTexture);
			}
		}
	}

	private bool DrawBrushSetDropdown()
	{
		// Here we add the index of the element in the label name, else items with no content are not shown or elements with the same name are only shown once.
		string[] labels = _serializedPrefabSets.GetArrayValues(p => p.stringValue, nameof(PrefabSet.Name)).Select((label, idx) => $"{idx + 1}: {label}").ToArray();
		int currentIndex = _serializedSelectedSetIndex.intValue;

		int newSelectedIndex = EditorGUILayout.Popup("Brush set", currentIndex, labels);

		if (newSelectedIndex != currentIndex)
		{
			_serializedSelectedSetIndex.intValue = newSelectedIndex;
			OnActiveSetChanged(currentIndex, newSelectedIndex);
			currentIndex = newSelectedIndex;
		}

		return currentIndex < labels.Length;
	}

	private void OnSetChanged()
	{
		foreach (PlacementLocation location in RandomLocations)
		{
			location.PrefabIndex = GetRandomPrefabIndex();
		}
	}

	private void OnActiveSetChanged(int oldIndex, int newIndex)
	{
		PrefabSet set = _sets[newIndex];

		if (set.Prefabs == null)
		{
			set.Prefabs = new List<PrefabEntry>();
		}

		if (set.Prefabs.Count == 0)
		{
			// Ensure there is always 1 element.
			set.Prefabs.Add(new PrefabEntry());
		}

		OnSetChanged();
	}

	private void PrefabEntryGUI(PrefabEntry prefabEntry)
	{
		using (new EditorGUILayout.HorizontalScope())
		{
			using (new EditorGUILayout.VerticalScope())
			{
				prefabEntry.Prefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Prefab"), prefabEntry.Prefab, typeof(GameObject), false);

				float newFactor = EditorGUILayout.Slider("OccurrenceFactor", prefabEntry.OccurrenceFactor, 0f, 1f);
				if (Math.Abs(prefabEntry.OccurrenceFactor - newFactor) > 0.00001)
				{
					prefabEntry.OccurrenceFactor = newFactor;
					float totalFactor = _sets[_selectedSetIndex].Prefabs.Sum(p => p.OccurrenceFactor);
					foreach (PrefabEntry entry in _sets[_selectedSetIndex].Prefabs)
					{
						entry.OccurrenceFactor /= totalFactor;
					}
					OnSetChanged();
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
		public float NormalCorrectionMin = 0f;
		public float NormalCorrectionMax = 0f;
		public EaseType EaseMethod = EaseType.Linear;
		// Note to self: DON'T USE 'IList'!
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

public static class ToolGui
{
	public static GUIStyle Warning = CreateStyle(GUI.skin.label, TextAnchor.MiddleCenter, Color.red);

	private static GUIStyle CreateStyle(GUIStyle baseStyle = null, TextAnchor alignment = TextAnchor.MiddleLeft, Color? foregroundColor = null)
	{
		GUIStyle style = baseStyle == null ? new GUIStyle() : new GUIStyle(GUI.skin.label);
		
		style.alignment = alignment;

		if (foregroundColor.HasValue)
		{
			style.normal.textColor = foregroundColor.Value;
		}

		return style;
	}
}