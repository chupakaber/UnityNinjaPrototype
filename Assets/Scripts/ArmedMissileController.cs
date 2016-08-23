using UnityEngine;
using System.Collections;

public class ArmedMissileController : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if(transform.position.y < -0.9f)
        {
            transform.position += Vector3.up * 1.0f * Time.deltaTime;
        }
	}

    public void Rearm() {
        transform.position = Vector3.up * -1.3f;
    }

}
