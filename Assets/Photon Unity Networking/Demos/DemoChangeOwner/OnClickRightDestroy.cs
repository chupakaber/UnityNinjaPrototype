using UnityEngine;

public class OnClickRightDestroy : Photon.MonoBehaviour
{
    public void OnPressRight()
    {
        Debug.Log("RightClick Destroy");
        PhotonNetwork.Destroy(gameObject);
    }
}