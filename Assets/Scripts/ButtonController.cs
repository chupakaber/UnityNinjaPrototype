using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonController : MonoBehaviour {

    public Button button;
    public Text text;
    public string description = "";
    public object context = null;

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void Show()
    {
        button.enabled = true;
        button.image.enabled = true;
        text.enabled = true;
    }

    public void Hide()
    {
        button.enabled = false;
        button.image.enabled = false;
        text.enabled = false;
    }

}
