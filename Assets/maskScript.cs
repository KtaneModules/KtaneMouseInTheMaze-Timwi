using UnityEngine;

public class maskScript : MonoBehaviour {

	Light mazeLigth;
	// Use this for initialization
	void Start () {
	
	}

	void Awake(){
		mazeLigth = GetComponent<Light> ();
		mazeLigth.cullingMask = 1 << 30;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
