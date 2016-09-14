using UnityEngine;
using System.Collections;

public class ArmedMissileController : MonoBehaviour {


    public Vector3 anchor = new Vector3(0.0f, -1.5f, 3.0f);
    private float margin = -2.5f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if(margin < 0.0f)
        {
            margin += Time.deltaTime;
            if(margin > 0.0f)
            {
                margin = 0.0f;
            }
        }
        transform.localPosition += (anchor + Vector3.up * margin - transform.localPosition) * Mathf.Min(1.0f, Time.deltaTime * 30.0f);
    }

    public void Rearm() {
        margin = -0.5f;
        ResetAnchor();
        transform.localPosition += (anchor + Vector3.up * margin - transform.localPosition);
    }

    public void SetAnchor(Vector2 position)
    {
        //anchor.x = (position.x - 0.5f) * 0.7f;
        //anchor.y = 0.45f + position.y * 0.5f;
        //anchor.z = -0.25f;
    }

    public void ResetAnchor()
    {
        //anchor.x = 0.0f;
        //anchor.y = 0.45f;
        //anchor.z = -0.25f;
    }

}
