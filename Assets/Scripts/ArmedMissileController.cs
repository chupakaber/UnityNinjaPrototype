using UnityEngine;
using System.Collections;

public class ArmedMissileController : MonoBehaviour {


    public Vector3 anchor = new Vector3(0.0f, -0.9f, 0.0f);
    private float margin = 0.0f;

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
        transform.position += (anchor - transform.position) * Mathf.Min(1.0f, Time.deltaTime * 5.0f);
	}

    public void Rearm() {
        margin = -1.0f;
    }

    public void SetAnchor(Vector2 position)
    {
        anchor.x = (position.x - 0.5f) * 0.7f;
        anchor.y = -0.9f + position.y * 0.7f + margin;
        anchor.z = 0.0f;
    }

    public void ResetAnchor()
    {
        anchor.x = 0.0f;
        anchor.y = -0.9f;
        anchor.z = 0.0f;
    }

}
