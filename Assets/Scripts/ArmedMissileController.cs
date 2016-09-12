using UnityEngine;
using System.Collections;

public class ArmedMissileController : MonoBehaviour {


    public Vector3 anchor = new Vector3(0.0f, 0.45f, -0.25f);
    private float margin = -0.5f;

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
        transform.position += (anchor + Vector3.up * margin - transform.position) * Mathf.Min(1.0f, Time.deltaTime * 15.0f);
	}

    public void Rearm() {
        margin = -0.5f;
        ResetAnchor();
        transform.position += (anchor + Vector3.up * margin - transform.position);
    }

    public void SetAnchor(Vector2 position)
    {
        anchor.x = (position.x - 0.5f) * 0.7f;
        anchor.y = 0.45f + position.y * 0.5f;
        anchor.z = -0.25f;
    }

    public void ResetAnchor()
    {
        anchor.x = 0.0f;
        anchor.y = 0.45f;
        anchor.z = -0.25f;
    }

}
