using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

    public GameNetwork gameNetwork;
    public PlayerObject obj = null;
    public SpriteRenderer spriteRenderer;

    private float flashCooldown = 0.0f;

	void Start () {

        name = "PlayerObject";
        if (obj == null)
        {
            obj = new PlayerObject();
        }
        obj.visualObject = this;
        //gameNetwork.location.AddObject(obj);

    }

    void Update () {

        float f;
        if (flashCooldown > 0.0f)
        {
            flashCooldown -= Time.deltaTime;
            f = 0.6f + 0.4f * Mathf.Abs(flashCooldown * 4.0f - 0.5f);
            spriteRenderer.color = new Color(1.0f, f, f, 1.0f);
            if (flashCooldown <= 0.0f)
            {
                spriteRenderer.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                flashCooldown = 0.0f;
            }
        }

    }

    public void Flash()
    {
        flashCooldown = 0.25f;
    }

}
