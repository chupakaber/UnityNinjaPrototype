using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AbilityButtonController : MonoBehaviour {

    public Text text;
    public Button button;
    public Image grayOver;
    public Image greenOver;

    public Sprite activeBackground;
    public Sprite passiveBackground;

    public int abilityId = -1;

    private float startCooldown = 0.0f;
    private float cooldown = 0.0f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        Color color;
        if(cooldown > 0.0f)
        {
            cooldown -= Time.deltaTime;
            if(startCooldown - cooldown < 0.5f)
            {
                if (!greenOver.enabled)
                {
                    greenOver.enabled = true;
                }
                color = greenOver.color;
                greenOver.color = new Color(color.r, color.g, color.b, 1.0f - Mathf.Abs(startCooldown - cooldown - 0.25f) * 4.0f);
            }
            else if(greenOver.enabled)
            {
                greenOver.enabled = false;
            }
            else
            {
                if (!grayOver.enabled)
                {
                    grayOver.enabled = true;
                }
                grayOver.fillAmount = Mathf.Min(1.0f, Mathf.Max(0.0f, cooldown / (startCooldown - 0.5f)));
            }
            if(cooldown <= 0.0f)
            {
                greenOver.enabled = false;
                grayOver.enabled = false;
                cooldown = 0.0f;
            }
        }
	}

    public bool Activate(float duration)
    {
        return Activate(duration, false);
    }

    public bool Activate(float duration, bool force)
    {
        if ((text.color == Color.black && cooldown == 0.0f) || force)
        {
            startCooldown = duration;
            cooldown = startCooldown;
            return true;
        }
        return false;
    }

    public void SwitchToActive()
    {
        button.image.sprite = activeBackground;
        text.color = Color.black;
    }

    public void SwitchToPassive()
    {
        button.image.sprite = passiveBackground;
        text.color = Color.white;
    }

}
