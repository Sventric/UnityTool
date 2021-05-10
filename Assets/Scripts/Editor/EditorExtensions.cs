using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class EditorExtensions
{
	public static void DrawHandles(this TangentSpace space, float scale = 1f, float width = 5f)
	{
		DrawHandles(space, space.ReferenceOrigin, scale, width);
	}

	public static void DrawHandles(this TangentSpace space, Vector3 worldPosition, float scale = 1f, float width = 15f)
	{
		Color previousColor = Handles.color;
		CompareFunction previousZTest = Handles.zTest;

		try
		{
			Handles.zTest = CompareFunction.Always;
			const float arrowSize = .1f;
			DrawAxis(Handles.xAxisColor, width, worldPosition, space.Forward, scale, arrowSize);
			DrawAxis(Handles.zAxisColor, width, worldPosition, space.Left, scale, arrowSize);
			DrawAxis(Handles.yAxisColor, width, worldPosition, space.Up, scale, arrowSize);
		}
		finally
		{
			Handles.zTest = previousZTest;
			Handles.color = previousColor;
		}
	}

	private static void DrawAxis(Color axisColor, float width, Vector3 worldPosition, Vector3 direction, float scale, float size)
	{
		Handles.color = axisColor;
		Handles.DrawAAPolyLine(width, worldPosition, worldPosition + direction * scale);

		if (size > 0)
		{
			Handles.ConeHandleCap(-1, worldPosition + direction, Quaternion.LookRotation(direction), size, EventType.Repaint);
		}
	}

	public static IEnumerable<Transform> GetAllChildren([NotNull] this Transform transform)
	{
		if (transform == null) throw new ArgumentNullException(nameof(transform));

		for (int idx = 0; idx < transform.childCount; idx++)
		{
			yield return transform.GetChild(idx);
		}
	}

	public static IEnumerable<T> GetAllChildren<T>([NotNull] this Transform transform, [NotNull] Func<Transform, T> selector)
	{
		if (transform == null) throw new ArgumentNullException(nameof(transform));
		if (selector == null) throw new ArgumentNullException(nameof(selector));

		for (int idx = 0; idx < transform.childCount; idx++)
		{
			yield return selector(transform.GetChild(idx));
		}
	}

	public static IEnumerable<T> GetAllChildren<T>([NotNull] this GameObject gameObject, [NotNull] Func<Transform, T> selector)
	{
		if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));
		if (selector == null) throw new ArgumentNullException(nameof(selector));

		return GetAllChildren(gameObject.transform, selector);
	}
}