using UnityEngine;
using System.Collections;

public class MissileController : MonoBehaviour {

    public GameNetwork gameNetwork;
    public MissileObject obj = null;
    public GameObject meshObject;

    void Start () {

        name = "MissileObject";
        if (obj == null)
        {
            obj = new MissileObject();
        }
        obj.visualObject = this;
        //gameNetwork.location.AddObject(obj);

    }

    void Update () {

        if(transform.rotation.x < 0.6f)
        {
            transform.Rotate(360.0f * Time.deltaTime, 0.0f, 0.0f);
        }
        meshObject.transform.Rotate(0.0f, 0.0f, -900.0f * Time.deltaTime);

    }
}
