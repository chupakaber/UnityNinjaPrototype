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
    public Canvas canvasSettings;
    public Button startButton;
    public Button joinButton;
    public Button startLocalButton;
    public Button settingsButton;
    public InputField roomIdField;
    public string storedMatchName = "";

    public int joinAttempts = 0;

    public float preferenceHealth = 100.0f;
    public float preferenceStamina = 100.0f;
    public float preferenceStaminaConsume = 30.0f;
    public float preferenceStaminaRegeneration = 10.0f;
    public float preferenceMinDamage = 5.0f;
    public float preferenceMaxDamage = 8.0f;
    public float preferenceCritChance = 0.15f;
    public float preferenceCritMultiplier = 1.5f;
    public float preferenceInjureChance = 0.1f;
    public float preferenceAbilityEvadeChance = 0.2f;
    public float preferenceAbilityCritChance = 0.15f;
    public float preferenceAbilityStunDuration = 5.0f;
    public float preferenceAbilityShieldDuration = 5.0f;
    public float preferenceAbilityShieldMultiplier = 0.5f;
    public float preferenceInjureArmEffect = 0.5f;
    public float preferenceInjureLegEffect = 0.5f;
    public float preferenceStrafeSpeed = 0.5f;


    public InputField preferenceFieldHealth;
    public InputField preferenceFieldStamina;
    public InputField preferenceFieldStaminaConsume;
    public InputField preferenceFieldStaminaRegeneration;
    public InputField preferenceFieldMinDamage;
    public InputField preferenceFieldMaxDamage;
    public InputField preferenceFieldCritChance;
    public InputField preferenceFieldCritMultiplier;
    public InputField preferenceFieldInjureChance;
    public InputField preferenceFieldAbilityEvadeChance;
    public InputField preferenceFieldAbilityCritChance;
    public InputField preferenceFieldAbilityStunDuration;
    public InputField preferenceFieldAbilityShieldDuration;
    public InputField preferenceFieldAbilityShieldMultiplier;
    public InputField preferenceFieldInjureArmEffect;
    public InputField preferenceFieldInjureLegEffect;
    public InputField preferenceFieldStrafeSpeed;

    void Start()
    {
        Application.runInBackground = true;
        NetManager.singleton.StartMatchMaker();
        canvasConnect.enabled = true;
        canvasPlay.enabled = false;
        canvasSettings.enabled = false;
        settingsButton.onClick.AddListener(delegate(){
            if(canvasSettings.enabled)
            {
                canvasSettings.enabled = false;
            }
            else
            {
                canvasSettings.enabled = true;
            }
        });
        startButton.onClick.AddListener(delegate() {
            CreateInternetMatch(roomIdField.text);
            canvasConnect.enabled = false;
        });
        joinButton.onClick.AddListener(delegate () {
            FindInternetMatch(roomIdField.text);
            canvasConnect.enabled = false;
        });
        startLocalButton.onClick.AddListener(delegate () {
            UpdatePreferences();
            gameNetwork = GameObject.Instantiate(gameNetworkPrefab).GetComponent<GameNetwork>();
            gameNetwork.camera = camera;
            gameNetwork.gameMatchMaker = this;
            gameNetwork.isLocal = true;
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
        UpdatePreferences();
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

    public void UpdatePreferences()
    {
        preferenceHealth = float.Parse(preferenceFieldHealth.text);
        preferenceStamina = float.Parse(preferenceFieldStamina.text);
        preferenceStaminaConsume = float.Parse(preferenceFieldStaminaConsume.text);
        preferenceStaminaRegeneration = float.Parse(preferenceFieldStaminaRegeneration.text);
        preferenceMinDamage = float.Parse(preferenceFieldMinDamage.text);
        preferenceMaxDamage = float.Parse(preferenceFieldMaxDamage.text);
        preferenceCritChance = float.Parse(preferenceFieldCritChance.text) * 0.01f;
        preferenceCritMultiplier = float.Parse(preferenceFieldCritMultiplier.text);
        preferenceInjureChance = float.Parse(preferenceFieldInjureChance.text) * 0.01f;
        preferenceAbilityEvadeChance = float.Parse(preferenceFieldAbilityEvadeChance.text) * 0.01f;
        preferenceAbilityCritChance = float.Parse(preferenceFieldAbilityCritChance.text) * 0.01f;
        preferenceAbilityStunDuration = float.Parse(preferenceFieldAbilityStunDuration.text);
        preferenceAbilityShieldDuration = float.Parse(preferenceFieldAbilityShieldDuration.text);
        preferenceAbilityShieldMultiplier = float.Parse(preferenceFieldAbilityShieldMultiplier.text) * 0.01f;
        preferenceInjureArmEffect = float.Parse(preferenceFieldInjureArmEffect.text) * 0.01f;
        preferenceInjureLegEffect = float.Parse(preferenceFieldInjureLegEffect.text) * 0.01f;
        preferenceStrafeSpeed = float.Parse(preferenceFieldStrafeSpeed.text);
    }

}