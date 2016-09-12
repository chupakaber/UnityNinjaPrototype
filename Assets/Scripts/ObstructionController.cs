using UnityEngine;
using System.Collections;

public class ObstructionController : MonoBehaviour {

    public GameNetwork gameNetwork;
    public ObstructionObject obj;
    public SpriteRenderer spriteRenderer;

    private float cooldown = 0.0f;
    
    void Start () {

        name = "ObstructionObject";
        if (obj == null)
        {
            obj = new ObstructionObject();
        }
        obj.visualObject = this;
        //gameNetwork.location.AddObject(obj);

    }
	
	void Update () {
	    if(cooldown > 0.0f)
        {
            cooldown -= Time.deltaTime;
            spriteRenderer.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Abs(cooldown * 4.0f - 1.0f));
            if (cooldown <= 0.0f)
            {
                cooldown = 0.0f;
                spriteRenderer.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            }
        }
	}

    public void Flash()
    {
        cooldown = 0.5f;
    }

}
