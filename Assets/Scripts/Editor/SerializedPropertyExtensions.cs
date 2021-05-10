using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class SerializedPropertyExtensions
{
	public static Type GetSerializedType(this SerializedProperty property)
	{
		object sourceObject = property.serializedObject.targetObject;
		string[] pathSegments = GetPathSegments(property.propertyPath);
		//string[] pathSegments = property.propertyPath.Split('.');
		BindingFlags fieldScope = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

		for (int index = 0; index < pathSegments.Length; index++)
		{
			string relativePath = pathSegments[index];

			if (relativePath.StartsWith("index:") && sourceObject is IList list && int.TryParse(relativePath.Substring(6), out int indexValue))
			{
				sourceObject = list[indexValue];
				continue;
			}

			Type sourceType = sourceObject.GetType();
			FieldInfo field = sourceType.GetField(relativePath, fieldScope);

			if (field == null)
			{
				Debug.LogWarning($"Unable to get field '{relativePath}' of type '{sourceType.Name}'.");
				return typeof(object);
			}

			sourceObject = field.GetValue(sourceObject);
		}

		return sourceObject.GetType();
	}

	private static string[] GetPathSegments(string propertyPath)
	{
		string tmpPath = propertyPath.Replace("Array.data[", "Array_data[");
		string[] segments = tmpPath.Split('.');

		for (int index = 0; index < segments.Length; index++)
		{
			string segment = segments[index];

			if (!segment.StartsWith("Array_data["))
			{
				continue;
			}

			string indexValue = segment.Substring(11, segment.Length - 12);
			segments[index] = $"index:{indexValue}";
		}

		return segments;
	}

	public static T GetSerializedValue<T>(this SerializedProperty property)
	{
		object sourceObject = property.serializedObject.targetObject;
		string[] pathSegments = GetPathSegments(property.propertyPath);
		BindingFlags fieldScope = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

		for (int index = 0; index < pathSegments.Length; index++)
		{
			string relativePath = pathSegments[index];

			if (relativePath.StartsWith("index:") && sourceObject is IList list && int.TryParse(relativePath.Substring(6), out int indexValue))
			{
				sourceObject = list[indexValue];
				continue;
			}

			Type sourceType = sourceObject.GetType();
			FieldInfo field = sourceType.GetField(relativePath, fieldScope);

			if (field == null)
			{
				Debug.LogWarning($"Unable to get field '{relativePath}' of type '{sourceType.Name}'.");
				return default(T);
			}

			sourceObject = field.GetValue(sourceObject);
		}

		if (!(sourceObject is T))
		{
			throw new InvalidCastException($"Can not cast property value to {typeof(T).Name}.");
		}

		return (T)sourceObject;
	}

	public static T[] GetArrayValues<T>(this SerializedProperty property, Func<SerializedProperty, T> getValue, params string[] subPath)
	{
		if (property == null) throw new ArgumentNullException(nameof(property));
		if (!property.isArray) throw new ArgumentException("Property is not an array.", nameof(property));

		T[] values = new T[property.arraySize];

		for (int idx = 0; idx < values.Length; idx++)
		{
			SerializedProperty subProperty = property.GetArrayElementAtIndex(idx);

			foreach (string path in subPath)
			{
				subProperty = subProperty.FindPropertyRelative(path);
			}

			values[idx] = getValue(subProperty);
		}

		return values;
	}

	public static T[] GetArrayValues<T>(this SerializedProperty property)
	{
		if (property == null) throw new ArgumentNullException(nameof(property));
		if (!property.isArray) throw new ArgumentException("Property is not an array.", nameof(property));

		T[] values = new T[property.arraySize];

		for (int idx = 0; idx < values.Length; idx++)
		{
			SerializedProperty subProperty = property.GetArrayElementAtIndex(idx);
			values[idx] = subProperty.GetSerializedValue<T>();
		}

		return values;
	}
}