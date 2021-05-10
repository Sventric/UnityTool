using UnityEngine;

public class TangentSpace
{
	public static TangentSpace CreateFromUp(Vector3 tangentUp, Vector3 referenceVector)
	{
		return CreateFromUp(Vector3.zero, tangentUp, referenceVector);
	}

	public static TangentSpace CreateFromUp(Vector3 referenceOrigin, Vector3 tangentUp, Vector3 referenceVector)
	{
		Vector3 yAxis = tangentUp.normalized;
		Vector3 xAxis = Vector3.Cross(yAxis, referenceVector).normalized;
		Vector3 zAxis = Vector3.Cross(yAxis, xAxis);

		return new TangentSpace(referenceOrigin, xAxis, yAxis, zAxis);
	}

	private TangentSpace(Vector3 referenceOrigin, Vector3 x, Vector3 y, Vector3 z)
	{
		ReferenceOrigin = referenceOrigin;
		Forward = x;
		Up = y;
		Left = z;
	}

	public readonly Vector3 ReferenceOrigin;
	public readonly Vector3 Forward;
	public readonly Vector3 Up;
	public readonly Vector3 Left;

	/// <summary>
	/// Convert a 2D vector to a 3D world space vector.
	/// </summary>
	/// <param name="point">A point in the XZ-plane of the tangent space.</param>
	/// <param name="height">A height perpendicular to the XZ-plane.</param>
	public Vector3 ConvertToWorldSpace(Vector2 point, float height = 0f)
	{
		return ReferenceOrigin + Forward * point.x + Up * height + Left * point.y;
	}

	/// <summary>
	/// Convert a 3D vector in the tangent space to a 3D world space vector.
	/// </summary>
	/// <param name="point">A point in the tangent space.</param>
	public Vector3 ConvertToWorldSpace(Vector3 point)
	{
		return ReferenceOrigin + Forward * point.x + Up * point.y + Left * point.z;
	}

	public Ray GetTangentRay(Vector2 relativePosition, float elevationHeight)
	{
		Vector3 rayOrigin = ConvertToWorldSpace(relativePosition, elevationHeight);
		Vector3 radDirection = -Up;
		return new Ray(rayOrigin, radDirection);
	}
}