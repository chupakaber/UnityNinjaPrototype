using UnityEngine;
using System.Collections;

public class MissileController : MonoBehaviour {

    public GameNetwork gameNetwork;
    public MissileObject obj = null;
    public GameObject meshObject;

    public Vector3 velocity = Vector3.zero;

    public float torsion = 0.0f;

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
        transform.Rotate(0.0f, 0.0f, -2.0f * torsion * Time.deltaTime);
        meshObject.transform.Rotate(0.0f, 0.0f, -(900.0f + 50.0f * torsion) * Time.deltaTime);
        transform.position += velocity;

    }

    public void DestroyDelayed(Vector3 passiveVelocity)
    {
        velocity = passiveVelocity;
        Destroy(gameObject, 1.0f);
    }

    public void DestroyImmediate()
    {

        Destroy(gameObject, 0.2f);
    }

}
