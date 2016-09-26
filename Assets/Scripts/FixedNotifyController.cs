using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FixedNotifyController : MonoBehaviour {

    public static int[] topSlots = new int[8];
    public static int[] bottomSlots = new int[8];

    public Text text;

    private int alignment = -1;
    private int slot = -1;
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
            if(cooldown <= 0.25f)
            {
                color = text.color;
                text.color = new Color(color.r, color.g, color.b, Mathf.Max(0.0f, cooldown * 4.0f));
            }
            if(cooldown <= 0.0f)
            {
                if(alignment == 0)
                {
                    bottomSlots[slot] = 0;
                }
                else if(alignment == 1)
                {
                    topSlots[slot] = 0;
                }
                Destroy(gameObject);
            }
        }
	}

    public void Show(int target, string message, int color, float duration)
    {
        alignment = target;
        cooldown = duration;
        if (target == 0)
        {
            if((slot = GetFreeSlot(bottomSlots)) != -1)
            {
                text.alignment = TextAnchor.MiddleLeft;
                text.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                text.rectTransform.anchorMax = new Vector2(0.0f, 0.0f);
                text.rectTransform.pivot = new Vector2(0.0f, 0.0f);
                text.rectTransform.anchoredPosition = new Vector2(10.0f, 20.0f + ((float)slot) * 22.0f);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else if(target == 1)
        {
            if ((slot = GetFreeSlot(topSlots)) != -1)
            {
                text.alignment = TextAnchor.MiddleRight;
                text.rectTransform.anchorMin = new Vector2(1.0f, 1.0f);
                text.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
                text.rectTransform.pivot = new Vector2(1.0f, 1.0f);
                text.rectTransform.anchoredPosition = new Vector2(-10.0f, -10.0f + ((float)slot) * -22.0f);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        if (color == 0)
        {
            text.color = new Color(0.2f, 0.9f, 0.2f, 1.0f);
        }
        else if(color == 1)
        {
            text.color = new Color(0.9f, 0.2f, 0.2f, 1.0f);
        }
        else if (color == 2)
        {
            text.color = new Color(0.0f, 0.7f, 0.5f, 1.0f);
        }
        text.text = message;
    }

    private int GetFreeSlot(int[] slots)
    {
        int i;
        for(i = 0; i < slots.Length; i++)
        {
            if(slots[i] == 0)
            {
                slots[i] = 1;
                return i;
            }
        }
        return -1;
    }

}
