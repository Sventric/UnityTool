using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(menuName = "Mobs/Test")]
public class MiscType : ScriptableObject
{
	public Dictionary<string, MiscProperty> Properties = new Dictionary<string, MiscProperty>();

	[Serializable]
	public class MiscProperty
	{
		public int Id;
		public string Name;
		public Type Type;
		public object Value;
	}

	public void Fill()
	{
		foreach (MiscProperty property in GetRandomProperties(3))
		{
			Properties.Add(property.Name, property);
		}
	}

	private IEnumerable<MiscProperty> GetRandomProperties(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return new MiscProperty
			{
				Id = i + 1,
				Name = $"Property {i + 1}",
				Type = typeof(float),
				Value = Random.value
			};
		}
	}
}