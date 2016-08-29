using UnityEngine;
using System.Collections;

public class SwipeTrailController : MonoBehaviour {

    public LineRenderer lineRenderer;
    public Color startColor = Color.white;
    public Color endColor = Color.white;

    public int pointsCount = 1;
    public float cooldown = 1.0f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        if(cooldown > 0.0f)
        {
            cooldown -= Time.deltaTime;
            startColor.a = Mathf.Max(0.0f, Mathf.Min(1.0f, cooldown * 2.0f));
            endColor.a = Mathf.Max(0.0f, Mathf.Min(1.0f, cooldown * 2.0f - 0.5f));
            lineRenderer.SetColors(startColor, endColor);
            if(cooldown < 0.0f)
            {
                Destroy(gameObject);
            }
        }
	
	}
}
