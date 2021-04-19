using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Mobs/Barrel")]
public class BarrelType : ScriptableObject
{
	[Range(1f, 8f)]
	public float Radius = 4f;
	[Range(-30f, 30f)]
	public float Damage = 10;

	public Color ExplosionColor = Color.red;
}
