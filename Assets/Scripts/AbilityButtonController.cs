using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AbilityButtonController : MonoBehaviour {

    public Text text;
    public Button button;

    private float cooldown = 0.0f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
        if(cooldown > 0.0f)
        {
            cooldown -= Time.deltaTime;
            if(cooldown <= 0.0f)
            {
                cooldown = 0.0f;
            }
        }

	}

    public bool Activate()
    {
        if (cooldown == 0.0f)
        {
            return true;
        }
        return false;
    }

}
