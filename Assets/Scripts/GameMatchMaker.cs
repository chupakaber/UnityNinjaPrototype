using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameMatchMaker : NetworkBehaviour
{

    //[NonSerialized]
    public Camera camera;
    public Button buttonTest;

    //[NonSerialized]
    public GameObject gameNetworkPrefab;
    [NonSerialized]
    public GameNetwork gameNetwork;
    //[NonSerialized]
    public Canvas canvasConnect;
    //[NonSerialized]
    public Canvas canvasPlay;
    //[NonSerialized]
    public Button startButton;
    //[NonSerialized]
    public Button joinButton;
    public string storedMatchName = "";

    public int joinAttempts = 0;

    private bool isServer = false;

    void Start()
    {
        Application.runInBackground = true;
        NetManager.singleton.StartMatchMaker();
        canvasConnect.enabled = true;
        canvasPlay.enabled = false;
        startButton.onClick.AddListener(delegate() {
            CreateInternetMatch("ninjaprototype");
            canvasConnect.enabled = false;
        });
        joinButton.onClick.AddListener(delegate () {
            FindInternetMatch("ninjaprototype");
            canvasConnect.enabled = false;
        });
        ((NetManager)NetManager.singleton).ServerConnect += OnServerConnect;
    }

    private void OnServerConnect(object sender, NetManager.NetworkConnectionEventArgs e)
    {
        if (e.conn.address != "localServer" && e.conn.address != "localClient")
        {
            gameNetwork = GameObject.Instantiate(gameNetworkPrefab).GetComponent<GameNetwork>();
            gameNetwork.camera = camera;
            gameNetwork.gameMatchMaker = this;
            //gameNetwork.Init();
            //gameNetwork.MatchCreated();
            NetworkServer.Spawn(gameNetwork.gameObject);
        }
        //Debug.Log("Client connected: " + e.conn.address);
    }

    //call this method to request a match to be created on the server
    public void CreateInternetMatch(string matchName)
    {
        //Debug.Log("Create internet match");
        isServer = true;
        NetManager.singleton.matchMaker.CreateMatch(matchName, 4, true, "", "", "", 0, 0, OnInternetMatchCreate);
    }

    //this method is called when your request for creating a match is returned
    private void OnInternetMatchCreate(bool success, string extendedInfo, MatchInfo hostInfo)
    {
        if (hostInfo != null)
        {
            //Debug.Log("Create match succeeded");
            if(gameNetwork != null)
            {
                NetManager.singleton.StopHost();
                NetManager.singleton.StopServer();
                NetManager.singleton.StopMatchMaker();
                NetManager.singleton.StartMatchMaker();
            }
            NetworkServer.Listen(hostInfo, NetManager.singleton.networkPort);
            NetManager.singleton.StartHost(hostInfo);
            canvasPlay.enabled = true;
        }
        else
        {
            //Debug.LogError("Create match failed");
        }
    }

    //call this method to find a match through the matchmaker
    public void FindInternetMatch(string matchName)
    {
        if (gameNetwork != null)
        {
            NetManager.singleton.StopMatchMaker();
            NetManager.singleton.StartMatchMaker();
        }
        storedMatchName = matchName;
        NetManager.singleton.matchMaker.ListMatches(0, 20, matchName, true, 0, 0, OnInternetMatchList);
    }

    //this method is called when a list of matches is returned
    private void OnInternetMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
    {
        if (matchList != null)
        {
            if (matchList.Count != 0)
            {
                NetManager.singleton.matchMaker.JoinMatch(matchList[matchList.Count - 1].networkId, "", "", "", 0, 0, OnJoinInternetMatch);
            }
            else
            {
                joinAttempts++;
                //Debug.Log("No matches in requested room! Attempt: " + joinAttempts);
                if (joinAttempts < 10)
                {
                    FindInternetMatch(storedMatchName);
                }
                else
                {
                    joinAttempts = 0;
                    //Debug.Log("Failed 10 join attempts");
                }
            }
        }
        else
        {
            //Debug.LogError("Couldn't connect to match maker");
        }
    }

    //this method is called when your request to join a match is returned
    private void OnJoinInternetMatch(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        //NetManager.singleton.OnServerConnect()
        if (matchInfo != null)
        {
            //Debug.Log("Able to join a match");
            NetManager.singleton.StartClient(matchInfo);
            //NetManager.singleton
            //gameNetwork.MatchJoined();
            canvasPlay.enabled = true;
        }
        else
        {
            //Debug.LogError("Join match failed");
        }
    }

}