using UnityEngine;
using System.Collections;

public class AbilityTargetController : MonoBehaviour {

    public SpriteRenderer ground;
    public SpriteRenderer crosshair;

    private Color groundBaseColor;
    private Color crosshairBaseColor;
    private Vector3 targetPoint = new Vector3(0.0f, 0.0f, 0.0f);
    private float cooldown = 0.0f;

	// Use this for initialization
	void Start () {
        enabled = false;
        groundBaseColor = ground.color;
        crosshairBaseColor = crosshair.color;
        ground.enabled = false;
        crosshair.enabled = false;
    }

    // Update is called once per frame
    void Update () {

        float f;
        Vector3 crosshairPoint;
        Vector3 crosshairZMargin;
        if(cooldown > 0.0f)
        {
            if(!ground.enabled)
            {
                ground.enabled = true;
                crosshair.enabled = true;
            }
            f = Mathf.Max(0.0f, Mathf.Min(1.0f, cooldown * 4.0f));
            ground.color = new Color(groundBaseColor.r, groundBaseColor.g, groundBaseColor.b, groundBaseColor.a * f);
            crosshair.color = new Color(crosshairBaseColor.r, crosshairBaseColor.g, crosshairBaseColor.b, crosshairBaseColor.a * f);
            crosshairZMargin = -Vector3.forward * targetPoint.z / Mathf.Abs(targetPoint.z) * 10.0f;
            crosshair.transform.position += (targetPoint - (crosshair.transform.position - crosshairZMargin)) * Time.deltaTime * 10.0f;
            ground.transform.position = crosshair.transform.position - crosshairZMargin - Vector3.up * crosshair.transform.position.y;
            cooldown -= Time.deltaTime;
            if(cooldown < 0.0f)
            {
                enabled = false;
                cooldown = 0.0f;
                ground.enabled = false;
                crosshair.enabled = false;
            }
        }

	}

    public void Set(Vector3 point)
    {
        cooldown = 0.5f;
        targetPoint = point;
        if (!enabled)
        {
            enabled = true;
            crosshair.transform.position = point;
            ground.transform.position = crosshair.transform.position - Vector3.up * crosshair.transform.position.y;
        }
    }

}
