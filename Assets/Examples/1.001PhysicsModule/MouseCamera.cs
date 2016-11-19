using UnityEngine;
using System.Collections;

public class MouseCamera : MonoBehaviour {

	public MeshRenderer TargetDisplay;

	Camera mCam;

	void Awake()
	{
		mCam = GetComponent<Camera> ();
		RenderTexture rt = new RenderTexture (256, 256, 24);
		rt.isPowerOfTwo = true;
		mCam.targetTexture = rt;
		TargetDisplay.material.SetTexture ("_MainTex", mCam.targetTexture);
		mCam.cullingMask = 1<<30;
	}
	/*void Start(){
		TargetDisplay.material.SetTexture ("_MainTex", mCam.targetTexture);
	}*/
}
