using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

public static class Snapper
{
	// % = Ctrl
	// # = Shift
	// & = Alt
	// _ = (no key modifier)
	[MenuItem("Edit/Snap Selected Objects %&`", true)]
	[UsedImplicitly /* Used by Unity UI */]
	public static bool SnapTheThingsValidate()
	{
		return Selection.gameObjects.Any();
	}

	[MenuItem("Edit/Snap Selected Objects %&`")]
	[UsedImplicitly /* Used by Unity UI */]
	public static void SnapTheThings()
	{
		Debug.Log("Snap it.");

		GameObject[] selection = Selection.gameObjects;

		foreach (GameObject go in selection)
		{
			Undo.RecordObject(go.transform, "Snapping Selection To Grid");
			Vector3 snappedPosition = go.transform.position.Round();
			go.transform.position = snappedPosition;
		}
	}

	public static Vector3 Round(this Vector3 v)
	{
		return new Vector3(
			Mathf.Round(v.x),
			Mathf.Round(v.y),
			Mathf.Round(v.z)
		);
	}
}