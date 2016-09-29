using UnityEngine;
using System.Collections;

public class SparksController : MonoBehaviour {

    public ParticleSystem particleEmitter1;
    public ParticleSystem particleEmitter2;

    private float cooldown = 0.0f;

    // Use this for initialization
    void Start () {
        Activate();
    }
	
	// Update is called once per frame
	void Update () {
	    if(cooldown > 0.0f)
        {
            cooldown -= Time.deltaTime;
            if (cooldown <= 0.0f)
            {
                Destroy(gameObject);
            }
        }
	}

    public void Activate()
    {
        cooldown = 2.0f;
        particleEmitter1.Emit(1);
        particleEmitter2.Emit(5);
    }

}
