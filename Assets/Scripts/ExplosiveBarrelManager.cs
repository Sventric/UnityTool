using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ExplosiveBarrelManager : MonoBehaviour
{
	public List<ExplosiveBarrel> AllBarrels = new List<ExplosiveBarrel>();

	public void Add(ExplosiveBarrel explosiveBarrel)
	{
		AllBarrels.Add(explosiveBarrel);
	}

	public void Remove(ExplosiveBarrel explosiveBarrel)
	{
		AllBarrels.Remove(explosiveBarrel);
	}

	public void OnDrawGizmosSelected()
	{
		//Debug.Log("Manager.Gizmos");
		foreach (ExplosiveBarrel barrel in AllBarrels)
		{
			//Debug.Log($"Manager: Show barrel '{barrel.name}'.");

			Vector3 start = transform.position;
			Vector3 end = barrel.transform.position;
			float halfHeight = (start.y - end.y) * .5f;
			Vector3 offset = Vector3.up * halfHeight;

			Debug.DrawLine(start, end, Color.yellow);


			// E1 : 0:29
			Handles.DrawBezier(
				start, 
				end,
				start - offset, 
				end + offset,
				Color.cyan, 
				EditorGUIUtility.whiteTexture,
				1f
			);
		}
	}
}