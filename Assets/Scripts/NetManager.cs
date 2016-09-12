using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using System;
using System.Collections;
using System.Collections.Generic;

public class NetManager : NetworkManager {

    //public static NetManager singleton { get; set; }

    public class NetworkConnectionEventArgs : EventArgs
    {
        public NetworkConnection conn { get; set; }
    }

    public event EventHandler<NetworkConnectionEventArgs> ServerConnect;


    // Use this for initialization
    public override void OnServerConnect(NetworkConnection conn)
    {
        EventHandler<NetworkConnectionEventArgs> handler = ServerConnect;
        NetworkConnectionEventArgs e = new NetworkConnectionEventArgs();
        e.conn = conn;
        if (handler != null)
        {
            handler(this, e);
        }
    }

}
