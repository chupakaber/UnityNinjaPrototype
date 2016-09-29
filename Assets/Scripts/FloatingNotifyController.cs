using UnityEngine;
using System.Collections;

public class FloatingNotifyController : MonoBehaviour {

    public MeshRenderer quadMesh;
    public TextMesh textMesh;
    public TextMesh textMeshBack;

    private Color baseColor;
    private float cooldown = 0.0f;

	void Start () {
        quadMesh.material = Material.Instantiate(quadMesh.sharedMaterial);
        quadMesh.material.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        textMesh.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        textMeshBack.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        cooldown = 2.0f;
	}
	
	void Update () {
        float f;
        Color color;
        if(cooldown > 0.0f)
        {
            cooldown -= Time.deltaTime;
            f = cooldown / 2.0f;
            quadMesh.material.color = new Color(1.0f, 1.0f, 1.0f, f);
            textMesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, f);
            textMeshBack.color = new Color(baseColor.r * 0.5f, baseColor.g * 0.5f, baseColor.b * 0.5f, f);
            transform.position += Vector3.up * 4.0f * Time.deltaTime;
            if(cooldown <= 0.0f)
            {
                Destroy(gameObject);
            }
        }
	}

    public void Show(Color color, string message)
    {
        baseColor = color;
        textMesh.text = message;
        textMeshBack.text = message;
    }

    public void Show(string message, int color)
    {
        Color _color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        switch(color)
        {
            case 0:
                _color = new Color(0.2f, 0.9f, 0.2f, 1.0f);
                break;
            case 1:
                _color = new Color(1.0f, 0.2f, 0.2f, 1.0f);
                break;
            case 2:
                _color = new Color(0.0f, 0.7f, 0.5f, 1.0f);
                break;
            case 3:
                _color = new Color(0.2f, 0.2f, 0.9f, 1.0f);
                break;
        }
        Show(_color, message);
    }

}
