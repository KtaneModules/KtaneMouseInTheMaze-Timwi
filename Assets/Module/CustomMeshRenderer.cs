using UnityEngine;
using System.Collections;

public class CustomMeshRenderer : MonoBehaviour {

	public Camera TargetCamera;
	public Material Mat;
	public string LayerName;

	MeshFilter mMeshFilter;
	Transform mTransform;
	int mLayer;

	void Awake()
	{
		mLayer = LayerMask.NameToLayer (LayerName);
		mMeshFilter = GetComponent<MeshFilter> ();
		mTransform = GetComponent<Transform> ();
	}

	void Update() 
	{
		Graphics.DrawMesh (
			mMeshFilter.mesh, 
			transform.localToWorldMatrix, 
			Mat, 
			mLayer, 
			TargetCamera);

		mTransform.Rotate (new Vector3 (0.0f, 0.0f, 20.0f));

		Graphics.DrawMesh (
			mMeshFilter.mesh, 
			transform.localToWorldMatrix, 
			Mat, 
			mLayer, 
			TargetCamera);

		mTransform.Rotate (new Vector3 (0.0f, 0.0f, -20.0f));
	}
}
