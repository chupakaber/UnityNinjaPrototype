using UnityEngine;
using System.Collections;

public class MissileController : MonoBehaviour {

    public GameNetwork gameNetwork;
    public MissileObject obj = null;

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
	
	}
}
