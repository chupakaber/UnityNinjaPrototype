using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

    public GameNetwork gameNetwork;
    public PlayerObject obj = null;
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public SkinnedMeshRenderer meshRenderer;

    public Vector3 velocity = Vector3.zero;

    private Color baseColor;
    private int lastAnimId = 0;
    private int animate = 0;
    private float flashCooldown = 0.0f;

    void Start () {

        name = "PlayerObject";
        if (obj == null)
        {
            obj = new PlayerObject();
        }
        obj.visualObject = this;
        baseColor = meshRenderer.material.color;
        //gameNetwork.location.AddObject(obj);

    }

    void Update () {

        float f;
        if (flashCooldown > 0.0f)
        {
            flashCooldown -= Time.deltaTime;
            f = Mathf.Abs(flashCooldown * 2.0f - 0.5f);
            //spriteRenderer.color = new Color(1.0f, f, f, 1.0f);
            meshRenderer.material.color = new Color(baseColor.r + (1.0f - baseColor.r) * f, f, f, 1.0f);
            if (flashCooldown <= 0.0f)
            {
                meshRenderer.material.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1.0f);
                //spriteRenderer.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                flashCooldown = 0.0f;
            }
        }
        if(animate > 0)
        {
            animate--;
            if (animate == 0)
            {
                animator.SetBool("animate", false);
            }
        }
        transform.position += velocity;

    }

    public void Flash()
    {
        flashCooldown = 0.5f;
    }

    public void Animate(int id)
    {
        if (lastAnimId != id)
        {
            lastAnimId = id;
            switch (id)
            {
                case 0:
                    animator.SetBool("walk_right", true);
                    Debug.Log("Animate: right");
                    break;
                case 1:
                    animator.SetBool("walk_right", false);
                    Debug.Log("Animate: left");
                    break;
            }
            animator.SetBool("animate", true);
            animate = 2;
        }
    }

}
