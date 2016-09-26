using UnityEngine;
using System.Collections;

public class ArmedMissileController : MonoBehaviour {

    public MeshRenderer[] meshes;
    public Vector3 anchor = new Vector3(0.0f, -1.5f, 3.0f);

    private Vector3 baseAnchor = new Vector3();
    private float margin = -1.0f;

	// Use this for initialization
	void Start () {
        baseAnchor = anchor;
        Rearm();
    }
	
	// Update is called once per frame
	void Update () {
        if(margin < 0.0f)
        {
            margin += Time.deltaTime * 4.0f;
            if(margin > 0.0f)
            {
                margin = 0.0f;
            }
        }
        transform.localPosition += (anchor + Vector3.up * margin - transform.localPosition) * Mathf.Min(1.0f, Time.deltaTime * 30.0f);
    }

    public void Rearm() {
        margin = -1.0f;
        ResetAnchor();
        transform.localPosition += (anchor + Vector3.up * margin - transform.localPosition);
        transform.localRotation = Quaternion.identity;
    }

    public void SetAnchor(Vector2 position, float time)
    {
        anchor.x = baseAnchor.x + (position.x - 0.5f) * 4.0f;
        anchor.y = baseAnchor.y + 0.0f + position.y * 3.0f;
        anchor.z = baseAnchor.z - Mathf.Min(1.0f, 2.0f * time) + position.y * 3.0f;
    }

    public void ResetAnchor()
    {
        anchor.x = baseAnchor.x;
        anchor.y = baseAnchor.y;
        anchor.z = baseAnchor.z;
    }

    public void SetMissile(int id)
    {
        int i;
        for (i = 1; i < meshes.Length; i++)
        {
            if (i == id)
            {
                meshes[i].enabled = true;
            }
            else
            {
                meshes[i].enabled = false;
            }
        }
    }

    public int GetCurrentMissile()
    {
        int i;
        for(i = 1; i < meshes.Length; i++)
        {
            if(meshes[i].enabled)
            {
                return i;
            }
        }
        return 1;
    }

    public void SetNextMissile()
    {
        int id = GetCurrentMissile();
        id++;
        if(id >= meshes.Length)
        {
            id = 1;
        }
        SetMissile(id);
    }

    public void SetPreviousMissile()
    {
        int id = GetCurrentMissile();
        id--;
        if (id <= 0)
        {
            id = meshes.Length - 1;
        }
        SetMissile(id);
    }

}
