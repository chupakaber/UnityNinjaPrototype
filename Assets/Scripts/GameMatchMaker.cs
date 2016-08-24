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


    public GameObject gameNetworkPrefab;
    [NonSerialized]
    public GameNetwork gameNetwork;
    public Camera camera;
    public Canvas canvasConnect;
    public Canvas canvasPlay;
    public Button startButton;
    public Button joinButton;
    public InputField roomIdField;
    public string storedMatchName = "";

    public int joinAttempts = 0;

    void Start()
    {
        Application.runInBackground = true;
        NetManager.singleton.StartMatchMaker();
        canvasConnect.enabled = true;
        canvasPlay.enabled = false;
        startButton.onClick.AddListener(delegate() {
            CreateInternetMatch(roomIdField.text);
            canvasConnect.enabled = false;
        });
        joinButton.onClick.AddListener(delegate () {
            FindInternetMatch(roomIdField.text);
            canvasConnect.enabled = false;
        });
        ((NetManager)NetManager.singleton).ServerConnect += OnServerConnect;
    }

    void OnGUI()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    private void OnServerConnect(object sender, NetManager.NetworkConnectionEventArgs e)
    {
        if (e.conn.address != "localServer" && e.conn.address != "localClient")
        {
            gameNetwork = GameObject.Instantiate(gameNetworkPrefab).GetComponent<GameNetwork>();
            gameNetwork.camera = camera;
            gameNetwork.gameMatchMaker = this;
            NetworkServer.Spawn(gameNetwork.gameObject);
        }
        //Debug.Log("Client connected: " + e.conn.address);
    }

    public void CreateInternetMatch(string matchName)
    {
        //Debug.Log("Create internet match");
        NetManager.singleton.matchMaker.CreateMatch(matchName, 4, true, "", "", "", 0, 0, OnInternetMatchCreate);
    }

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
            canvasPlay.enabled = true;
        }
        else
        {
            //Debug.LogError("Join match failed");
        }
    }

}