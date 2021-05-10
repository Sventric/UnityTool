using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

public enum EaseType
{
	Linear = 0,
	Power,
	InOut,
	InOutCubic,
	InOutQuint
}

[PublicAPI]
public static class EaseMethods
{
	private static SortedDictionary<EaseType, Func<float, float>> _easeFunctions;

	static EaseMethods()
	{
		_easeFunctions = new SortedDictionary<EaseType, Func<float, float>>
		{
			[EaseType.Linear] = EaseLinear,
			[EaseType.Power] = EasePower,
			//[EaseType.InOut] = EaseInOut,
			[EaseType.InOutCubic] = EaseInOutCubic,
			[EaseType.InOutQuint] = EaseInOutQuint,
		};
	}



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float EaseLinear(float x)
	{
		return x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float EasePower(float x)
	{
		return x * x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float EaseInOut(float x, float steepness)
	{
		// steepness == 1 => linear
		// steepness > 1  => s-curve
		return x < .5 ? Mathf.Pow(2 * x, steepness) / 2 : 1 - Mathf.Pow(2 * (1 - x), steepness) / 2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float EaseInOutQuint(float x)
	{
		return x < 0.5 ? 16 * x * x * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 5) / 2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float EaseInOutCubic(float x)
	{
		return x < 0.5 ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
	}

}