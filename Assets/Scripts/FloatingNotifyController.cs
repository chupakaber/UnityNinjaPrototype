using UnityEngine;
using System.Collections;

public class FloatingNotifyController : MonoBehaviour {

    public TextMesh textMesh;

    private Color baseColor;
    private float cooldown = 0.0f;

	void Start () {
        textMesh.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        cooldown = 1.0f;
	}
	
	void Update () {
        float f;
        Color color;
        if(cooldown > 0.0f)
        {
            cooldown -= Time.deltaTime;
            f = cooldown;
            textMesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, f);
            transform.position += Vector3.up * 0.2f * Time.deltaTime;
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
    }

    public void ShowRed(string message)
    {
        Show(new Color(1.0f, 0.4f, 0.4f, 1.0f), message);
    }

    public void ShowGreen(string message)
    {
        Show(new Color(0.4f, 0.9f, 0.4f, 1.0f), message);
    }

}
