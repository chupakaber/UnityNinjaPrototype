using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames;
using ExitGames.Client;
using ExitGames.Client.Photon;

public class GameMatchMaker : Photon.PunBehaviour
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
    public Text joinButtonText;
    public Button startLocalButton;
    public Button settingsButton;
    public InputField roomIdField;
    public Button missileSelectorPrevButton;
    public Button missileSelectorNextButton;
    public ArmedMissileController armedMissile;
    public Button[] VenomButtons;
    public Button[] AbilityButtons;
    public RavenController visualEffectRaven;
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


    private Dictionary<int, string> langNotices = new Dictionary<int, string>();
    private LinkedList<BaseObjectMessage> delayedMessages = new LinkedList<BaseObjectMessage>();
    private RoomInfo selectedRoom = null;
    private TypedLobby lobby;
    private float remoteTimestamp = 0.0f;
    private int abilityFirst = 1;
    private int abilitySecond = 2;

    public float GetRemoteTimestamp()
    {
        return remoteTimestamp;
    }

    public void AddDelayedMessage(BaseObjectMessage message)
    {
        delayedMessages.AddLast(message);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("CONNECTED!");
        lobby = new TypedLobby();
        lobby.Type = LobbyType.Default;
        lobby.Name = "Battle";
        PhotonNetwork.JoinLobby(lobby);
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        int i;
        Debug.Log("OnJoinedLobby: " + PhotonNetwork.networkingPeer.lobby.Name + " [" + PhotonNetwork.networkingPeer.lobby.Type + "] (" + PhotonNetwork.networkingPeer.insideLobby + ")");
        joinButton.interactable = true;
    }

    public override void OnReceivedRoomListUpdate()
    {
        base.OnReceivedRoomListUpdate();
        int i;
        RoomInfo[] rooms = PhotonNetwork.GetRoomList();
        Debug.Log("Rooms: " + rooms.Length);
        for (i = 0; i < rooms.Length; i++)
        {
            if (selectedRoom == null && rooms[i].open && rooms[i].playerCount == 1)
            {
                selectedRoom = rooms[i];
            }
            Debug.Log("Room [" + rooms[i].name + "] players: " + rooms[i].playerCount);
        }
        if (selectedRoom != null)
        {
            joinButtonText.text = "Войти в бой #" + selectedRoom.name;
            roomIdField.text = selectedRoom.name;
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        int i;
        Debug.Log("OnJoinedRoom: " + PhotonNetwork.networkingPeer.CurrentRoom.name + " (" + PhotonNetwork.networkingPeer.CurrentRoom.playerCount + ")");
        canvasConnect.enabled = false;
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        Debug.Log("OnCreatedRoom: " + PhotonNetwork.networkingPeer.CurrentRoom.name + " (" + PhotonNetwork.networkingPeer.CurrentRoom.playerCount + ")");
    }

    public override void OnPhotonCreateRoomFailed(object[] codeAndMsg)
    {
        base.OnPhotonCreateRoomFailed(codeAndMsg);
        startButton.interactable = true;
        startLocalButton.interactable = true;
        Debug.LogError("Create room failed");
    }

    public override void OnPhotonJoinRoomFailed(object[] codeAndMsg)
    {
        base.OnPhotonCreateRoomFailed(codeAndMsg);
        joinButtonText.text = "Создать бой";
        startButton.interactable = true;
        startLocalButton.interactable = true;
        Debug.LogError("Join room failed");
    }

    void Start()
    {
        int i;

        Application.runInBackground = true;
        //NetManager.singleton.StartMatchMaker();
        canvasConnect.enabled = true;
        canvasPlay.enabled = false;
        canvasSettings.enabled = false;
        roomIdField.text = "" + UnityEngine.Random.Range(1, 9);
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
            PhotonNetwork.logLevel = PhotonLogLevel.ErrorsOnly;
            //PhotonNetwork.logLevel = PhotonLogLevel.Full;
            PhotonNetwork.OnEventCall += OnEvent;
            Debug.Log("Connecting to Photon Server start #1");
            if (PhotonNetwork.ConnectUsingSettings("1.0"))
            {
                Debug.Log("Connecting to Photon Server process... #1");
                UpdatePreferences();
                gameNetwork = GameObject.Instantiate(gameNetworkPrefab).GetComponent<GameNetwork>();
                gameNetwork.camera = camera;
                gameNetwork.gameMatchMaker = this;
                gameNetwork.isServer = false;
                gameNetwork.isLocal = false;
            }
            else
            {
                Debug.LogError("Connection to Photon Server failed");
                startButton.interactable = true;
                startLocalButton.interactable = true;
                DestroyImmediate(gameNetwork);
            }
            startButton.interactable = false;
            startLocalButton.interactable = false;
            //CreateInternetMatch(roomIdField.text);
        });
        joinButton.onClick.AddListener(delegate () {
            Debug.Log("Joining room");
            if (selectedRoom == null)
            {
                RoomOptions roomOptions = new RoomOptions();
                roomOptions.IsOpen = true;
                roomOptions.IsVisible = true;
                roomOptions.MaxPlayers = 2;
                if (PhotonNetwork.CreateRoom(roomIdField.text, roomOptions, lobby))
                {
                    Debug.Log("Room creating!");
                }
                else
                {
                    Debug.LogError("Can't create room");
                }
            }
            else
            {
                if (PhotonNetwork.JoinRoom(selectedRoom.name))
                {
                    Debug.Log("Room joining!");
                }
                else
                {
                    Debug.LogError("Can't join room");
                }
            }
            //FindInternetMatch(roomIdField.text);
            //canvasConnect.enabled = false;
            joinButton.interactable = false;
        });
        joinButton.interactable = false;
        startLocalButton.onClick.AddListener(delegate () {
            UpdatePreferences();
            gameNetwork = GameObject.Instantiate(gameNetworkPrefab).GetComponent<GameNetwork>();
            gameNetwork.camera = camera;
            gameNetwork.gameMatchMaker = this;
            gameNetwork.isLocal = true;
        });
        //((NetManager)NetManager.singleton).ServerConnect += OnServerConnect;

        missileSelectorPrevButton.onClick.AddListener(delegate() {
            armedMissile.SetPreviousMissile();
        });
        missileSelectorNextButton.onClick.AddListener(delegate () {
            armedMissile.SetNextMissile();
        });

        VenomButtons[1].onClick.AddListener(delegate {
            int j;
            for(j = 1; j < VenomButtons.Length; j++)
            {
                if(j == 1)
                {
                    VenomButtons[j].image.color = Color.green;
                }
                else
                {
                    VenomButtons[j].image.color = Color.white;
                }
            }
        });
        VenomButtons[2].onClick.AddListener(delegate {
            int j;
            for (j = 1; j < VenomButtons.Length; j++)
            {
                if (j == 2)
                {
                    VenomButtons[j].image.color = Color.green;
                }
                else
                {
                    VenomButtons[j].image.color = Color.white;
                }
            }
        });
        VenomButtons[3].onClick.AddListener(delegate {
            int j;
            for (j = 1; j < VenomButtons.Length; j++)
            {
                if (j == 3)
                {
                    VenomButtons[j].image.color = Color.green;
                }
                else
                {
                    VenomButtons[j].image.color = Color.white;
                }
            }
        });
        VenomButtons[4].onClick.AddListener(delegate {
            int j;
            for (j = 1; j < VenomButtons.Length; j++)
            {
                if (j == 4)
                {
                    VenomButtons[j].image.color = Color.green;
                }
                else
                {
                    VenomButtons[j].image.color = Color.white;
                }
            }
        });
        AbilityButtons[1].onClick.AddListener(delegate {
            SelectAbility(1);
        });
        AbilityButtons[2].onClick.AddListener(delegate {
            SelectAbility(2);
        });
        AbilityButtons[3].onClick.AddListener(delegate {
            SelectAbility(3);
        });
        AbilityButtons[4].onClick.AddListener(delegate {
            SelectAbility(4);
        });
        AbilityButtons[5].onClick.AddListener(delegate {
            SelectAbility(5);
        });



        langNotices.Add(0, "");
        langNotices.Add(1, "К");
        langNotices.Add(2, "ГОЛОВА");
        langNotices.Add(3, "НОГА");
        langNotices.Add(4, "РУКА");
        langNotices.Add(5, "(ЛЕГ.)");
        langNotices.Add(6, "(СРЕД.)");
        langNotices.Add(7, "(ТЯЖ.)");
        langNotices.Add(8, "ЯД");
        langNotices.Add(9, "ЩИТ");
        langNotices.Add(10, "КРИТИЧЕСКИЙ УРОН");
        langNotices.Add(11, "УКЛОНЕНИЕ");
        langNotices.Add(12, "ОГЛУШЕН");
        langNotices.Add(13, "ОГЛУШЕНИЕ");


    }

    public void SelectAbility(int id)
    {
        int i;
        if(abilityFirst != id && abilitySecond != id)
        {
            AbilityButtons[abilityFirst].image.color = Color.white;
            abilityFirst = abilitySecond;
            abilitySecond = id;
            AbilityButtons[abilitySecond].image.color = Color.green;
        }
    }

    void OnEvent(byte eventCode, object content, int senderId)
    {
        int i;
        BaseObjectMessage baseObjectMessage;
        PlayerObject playerObject = null;
        PlayerController playerController = null;
        //Debug.Log("RECEIVE EVENT[" + eventCode + "] from [" + senderId + "]");
        switch (eventCode)
        {
            case 1:
                baseObjectMessage = new BaseObjectMessage();
                baseObjectMessage.Unpack((byte[])content);
                remoteTimestamp = baseObjectMessage.timemark;
                gameNetwork.ClientInit();
                gameNetwork.playerId = baseObjectMessage.id;
                Debug.Log("INITIALIZE PLAYER ID: " + gameNetwork.playerId);
                /* duplicate for GameNetwork RpcSpawnObject case PLAYER */
                playerObject = (PlayerObject)gameNetwork.location.GetObject(gameNetwork.playerId);
                if (playerObject != null)
                {
                    camera.transform.position = playerObject.position * 100.0f + Vector3.up * 15.0f;
                    if (gameNetwork.playerId == 1)
                    {
                        camera.transform.eulerAngles = new Vector3(camera.transform.eulerAngles.x, 180.0f, camera.transform.eulerAngles.z);
                    }
                }
                playerObject = (PlayerObject)gameNetwork.location.GetObject(gameNetwork.playerId == 1 ? 0 : 1);
                if (playerObject != null && playerObject.visualObject == null)
                {
                    playerController = (Instantiate(gameNetwork.bodyPrefabs[0])).GetComponent<PlayerController>();
                    playerController.gameNetwork = gameNetwork;
                    playerController.obj = playerObject;
                    playerObject.visualObject = playerController;
                    playerController.transform.position = playerObject.position * 100.0f;
                    //playerController.transform.localScale *= 10.0f;
                }
                /* */
                canvasPlay.enabled = true;

                InitializeMessage initializeMessage = new InitializeMessage();
                for (i = 1; i < AbilityButtons.Length; i++)
                {
                    if (AbilityButtons[i].image.color == Color.green)
                    {
                        if (initializeMessage.abilityFirstId <= -1)
                        {
                            initializeMessage.abilityFirstId = i;
                        }
                        else
                        {
                            initializeMessage.abilitySecondId = i;
                        }
                    }
                }
                gameNetwork.myMissileId = armedMissile.GetCurrentMissile();
                initializeMessage.missileId = gameNetwork.myMissileId;
                for (i = 1; i < VenomButtons.Length; i++)
                {
                    if (VenomButtons[i].image.color == Color.green)
                    {
                        initializeMessage.venomId = i;
                    }
                }
                PhotonNetwork.networkingPeer.OpCustom((byte)1, new Dictionary<byte, object> { { 245, initializeMessage.Pack() } }, true);

                break;
            case 2:
                SpawnObjectMessage spawnObjectMessage = new SpawnObjectMessage();
                spawnObjectMessage.Unpack((byte[])content);
                //Debug.Log(Time.fixedTime + " Spawn." + spawnObjectMessage.objectType + " [" + spawnObjectMessage.id + "]");
                spawnObjectMessage.eventCode = eventCode;
                delayedMessages.AddLast(spawnObjectMessage);
                //gameNetwork.RpcSpawnObject(spawnObjectMessage.id, spawnObjectMessage.objectType, spawnObjectMessage.newPosition, spawnObjectMessage.newFloat, spawnObjectMessage.visualId);
                break;
            case 3:
                DestroyObjectMessage destroyObjectMessage = new DestroyObjectMessage();
                destroyObjectMessage.Unpack((byte[])content);
                //Debug.Log(Time.fixedTime + " Destroy [" + destroyObjectMessage.id + "]: " + destroyObjectMessage.objectId);
                destroyObjectMessage.eventCode = eventCode;
                delayedMessages.AddLast(destroyObjectMessage);
                //gameNetwork.RpcDestroyObject(destroyObjectMessage.id);
                break;
            case 4:
                MoveObjectMessage moveObjectMessage = new MoveObjectMessage();
                moveObjectMessage.Unpack((byte[])content);
                //Debug.Log(Time.fixedTime + " Move [" + moveObjectMessage.id + "]");
                moveObjectMessage.eventCode = eventCode;
                delayedMessages.AddLast(moveObjectMessage);
                //gameNetwork.RpcMoveObject(moveObjectMessage.id, moveObjectMessage.newPosition, moveObjectMessage.newFloat, moveObjectMessage.timestamp);
                break;
            case 5:
                UpdatePlayerMessage updatePlayerMessage = new UpdatePlayerMessage();
                updatePlayerMessage.Unpack((byte[])content);
                //Debug.Log("Player[" + updatePlayerMessage.id + "] health: " + updatePlayerMessage.newHealth + " ; stamina: " + updatePlayerMessage.newStamina);
                gameNetwork.RpcUpdatePlayer(updatePlayerMessage.id, updatePlayerMessage.newHealth, updatePlayerMessage.newStamina, updatePlayerMessage.newStaminaConsumption);
                break;
            case 6:
                gameNetwork.RpcRearmMissile();
                break;
            case 7:
                baseObjectMessage = new BaseObjectMessage();
                baseObjectMessage.Unpack((byte[])content);
                gameNetwork.RpcFlashPlayer(baseObjectMessage.id);
                break;
            case 8:
                baseObjectMessage = new BaseObjectMessage();
                baseObjectMessage.Unpack((byte[])content);
                gameNetwork.RpcGameOver(baseObjectMessage.id);
                break;
            case 9:
                SetAbilityMessage setAbilityMessage = new SetAbilityMessage();
                setAbilityMessage.Unpack((byte[])content);
                gameNetwork.RpcSetAbility(setAbilityMessage.id, setAbilityMessage.value);
                break;
            case 10:
                NoticeMessage noticeMessage = new NoticeMessage();
                noticeMessage.Unpack((byte[])content);
                //Debug.Log("GET NOTICE MESSAGE. timemark: " + noticeMessage.timemark + " ; numericValue: " + noticeMessage.numericValue);
                noticeMessage.eventCode = eventCode;
                delayedMessages.AddLast(noticeMessage);
                break;
            case 11:
                baseObjectMessage = new BaseObjectMessage();
                baseObjectMessage.Unpack((byte[])content);
                gameNetwork.RpcFlashPassiveAbility(baseObjectMessage.id);
                break;
            case 12:
                baseObjectMessage = new BaseObjectMessage();
                baseObjectMessage.Unpack((byte[])content);
                //Debug.Log("FLASH OBSTRUCTION[" + baseObjectMessage.id + "]. timemark: " + baseObjectMessage.timemark);
                gameNetwork.RpcFlashObstruction(baseObjectMessage.id);
                break;
            case 13:
                VisualEffectMessage visualEffectMessage = new VisualEffectMessage();
                visualEffectMessage.Unpack((byte[])content);
                Debug.Log("VISUAL EFFECT [" + visualEffectMessage.id + "]. targetId: " + visualEffectMessage.targetId);
                gameNetwork.RpcVisualEffect(visualEffectMessage.id, visualEffectMessage.invokerId, visualEffectMessage.targetId, visualEffectMessage.duration);
                break;
        }
    }

    void CheckEvents()
    {
        LinkedListNode<BaseObjectMessage> objMessageNode;
        LinkedListNode<BaseObjectMessage> objMessageNodeNext;
        BaseObjectMessage baseObjectMessage;
        PlayerObject playerObject = null;
        PlayerController playerController = null;
        objMessageNode = delayedMessages.First;
        while(objMessageNode != null)
        {
            objMessageNodeNext = objMessageNode.Next;
            if(objMessageNode.Value.timemark <= remoteTimestamp)
            {
                switch(objMessageNode.Value.eventCode)
                {
                    case 2:
                        SpawnObjectMessage spawnObjectMessage = (SpawnObjectMessage)objMessageNode.Value;
                        gameNetwork.RpcSpawnObject(spawnObjectMessage.objectId, spawnObjectMessage.objectType, spawnObjectMessage.newPosition, spawnObjectMessage.newVelocity, spawnObjectMessage.newAcceleration, spawnObjectMessage.newTorsion, spawnObjectMessage.newFloat, spawnObjectMessage.visualId);
                        break;
                    case 3:
                        DestroyObjectMessage destroyObjectMessage = (DestroyObjectMessage)objMessageNode.Value;
                        gameNetwork.RpcDestroyObject(destroyObjectMessage.objectId);
                        break;
                    case 4:
                        MoveObjectMessage moveObjectMessage = (MoveObjectMessage)objMessageNode.Value;
                        gameNetwork.RpcMoveObject(moveObjectMessage.objectId, moveObjectMessage.newPosition, moveObjectMessage.newVelocity, moveObjectMessage.newAcceleration, moveObjectMessage.newTorsion, moveObjectMessage.newFloat, moveObjectMessage.timestamp);
                        break;
                    case 10:
                        NoticeMessage noticeMessage = (NoticeMessage)objMessageNode.Value;
                        //Debug.Log("NOTICE MESSAGE: " + noticeMessage.numericValue + " ; " + noticeMessage.color + " ; " + noticeMessage.floating + " ; " + noticeMessage.offset);
                        string noticeText = "";
                        if (noticeMessage.color == 0)
                        {
                            noticeText += "+";
                        }
                        else
                        {
                            noticeText += "-";
                        }
                        if (noticeMessage.prefixMessage != -1)
                        {
                            noticeText += " " + langNotices[noticeMessage.prefixMessage];
                        }
                        if (noticeMessage.numericValue != 0)
                        {
                            noticeText += " " + noticeMessage.numericValue;
                        }
                        if (noticeMessage.suffixMessage != -1)
                        {
                            noticeText += " " + langNotices[noticeMessage.suffixMessage];
                        }
                        gameNetwork.RpcShowNotice(noticeMessage.id, noticeText, noticeMessage.offset, noticeMessage.color, noticeMessage.floating);
                        break;
                }
                delayedMessages.Remove(objMessageNode);
            }
            objMessageNode = objMessageNodeNext;
        }
    }

    void Update()
    {
        remoteTimestamp += Time.deltaTime;
        CheckEvents();
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
        //NetManager.singleton.matchMaker.CreateMatch(matchName, 4, true, "", "", "", 0, 0, OnInternetMatchCreate);
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
            //NetworkServer.Listen(hostInfo, NetManager.singleton.networkPort);
            //NetManager.singleton.StartHost(hostInfo);
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
            //NetManager.singleton.StopMatchMaker();
            //NetManager.singleton.StartMatchMaker();
        }
        storedMatchName = matchName;
        //NetManager.singleton.matchMaker.ListMatches(0, 20, matchName, true, 0, 0, OnInternetMatchList);
    }

    private void OnInternetMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
    {
        if (matchList != null)
        {
            if (matchList.Count != 0)
            {
                //NetManager.singleton.matchMaker.JoinMatch(matchList[matchList.Count - 1].networkId, "", "", "", 0, 0, OnJoinInternetMatch);
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
            //NetManager.singleton.StartClient(matchInfo);
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
