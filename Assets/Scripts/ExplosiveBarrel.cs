using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

//[ExecuteInEditMode][ExecuteAlways]
public class ExplosiveBarrel : MonoBehaviour
{
	public ExplosiveBarrelManager Manager;

	public BarrelType Type;

	private MaterialPropertyBlock _mpb;

	public MaterialPropertyBlock Mpb
	{
		get
		{
			if (_mpb == null)
			{
				_mpb = new MaterialPropertyBlock();
			}

			return _mpb;
		}
	}

	private void OnStart()
	{
		//Debug.Log($"{name}.{MethodBase.GetCurrentMethod().Name}");
	}

	private void OnEnable()
	{
		//Debug.Log($"{name}.{MethodBase.GetCurrentMethod().Name}");
		Manager?.Add(this);
		TryApplyColor();
	}

	private void OnDisable()
	{
		//Debug.Log($"{name}.{MethodBase.GetCurrentMethod().Name}");
		Manager?.Remove(this);
	}

	private void OnValidate()
	{
		//Debug.Log($"{name}.{MethodBase.GetCurrentMethod().Name}");
		TryApplyColor();
	}

	[ContextMenu("Custom/DoSomething")]
	public void DoSomething()
	{
		Debug.Log("Let's do something...");
	}


	private void Awake()
	{
		Debug.Log($"{name}.{MethodBase.GetCurrentMethod().Name}");
		Manager = FindObjectOfType<ExplosiveBarrelManager>();
		// Will duplicate the material (create a per instance new material and leak materials into the scene)
		//GetComponent<MeshRenderer>().material.color = Color.red;
		// Will duplicate the mesh
		//GetComponent<MeshFilter>().mesh.name = "";

		// Will modify the *asset*
		//GetComponent<MeshRenderer>().sharedMaterial.color = Color.red;

		// Should use "material-blocks"???

		// With the hide-flag we can instruct the engine to don't save this material en just clear it on a reload.
		/*
		Shader shader = Shader.Find("Default/Diffuse");
		Material tempMaterial = new Material(shader)
		{
			hideFlags = HideFlags.HideAndDontSave,
			color = Color.cyan
		};
		//*/
		//GetComponent<MeshRenderer>().m
	}

	public void TryApplyColor()
	{
		if (Type == null)
		{
			return;
		}

		MeshRenderer rnd = GetComponent<MeshRenderer>();
		/*
		rnd.material.color = ExplosionColor;
		// is same as:
		rnd.material.SetColor("_Color", ExplosionColor);
		// is same as:
		rnd.material.SetColor(Shader.PropertyToID("_Color"), ExplosionColor);
		//*/

		Mpb.SetColor("_Color", Type.ExplosionColor);
		rnd.SetPropertyBlock(Mpb);
	}

	private void OnDrawGizmos()
	{
		if (Type == null)
		{
			return;
		}

		Color previousColor = Gizmos.color;
		Color previousHandlesColor = Handles.color;
		CompareFunction previousZTest = Handles.zTest;

		Gizmos.color = Type.ExplosionColor;
		Handles.color = Type.ExplosionColor;
		Handles.zTest = CompareFunction.LessEqual;

		Handles.DrawWireDisc(transform.position, transform.up, Type.Radius);

		Handles.zTest = previousZTest;
		Gizmos.color = previousColor;
		Handles.color = previousHandlesColor;

	}

	public void OnDrawGizmosSelected()
	{
		/*
		Color previousColor = Gizmos.color;

		Gizmos.color = ExplosionColor;
		Gizmos.DrawWireSphere(transform.position, Radius);
		Gizmos.color = previousColor;
		//*/
	}
}