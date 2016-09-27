using UnityEngine;
using System.Collections;

public class RavenController : MonoBehaviour {

    public SkinnedMeshRenderer meshRenderer;
    public Animation animation;

    private float cooldown = 0.0f;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {

        transform.localPosition = new Vector3(Mathf.Sin(cooldown * 5.0f) * 7.0f, 6.0f, 20.0f + Mathf.Abs(Mathf.Sin(cooldown * 5.0f)) * 5.0f);
        cooldown -= Time.deltaTime;
        if(cooldown <= 0.0f)
        {
            meshRenderer.enabled = false;
            animation.enabled = false;
            enabled = false;
        }

	}

    public void Activate ()
    {
        cooldown = 5.0f;
        meshRenderer.enabled = true;
        animation.enabled = true;
        enabled = true;
    }

}
