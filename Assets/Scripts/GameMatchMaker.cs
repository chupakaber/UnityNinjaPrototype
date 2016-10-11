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
    public Canvas canvasGameover;
    public ButtonController startButton;
    public ButtonController createButton;
    public ButtonController[] joinButtons;
    public Image lobbyPanel;
    public Button startLocalButton;
    public Button settingsButton;
    public InputField roomIdField;
    public Button missileSelectorPrevButton;
    public Button missileSelectorNextButton;
    public Button gameOverButton;
    public Text battleTitleLabel;
    public Text battleTimeLabel;
    public Text battleDamageLabel;
    public Text battleDPSLabel;
    public Text battleWoundLabel;
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
        lobbyPanel.enabled = true;
        for (i = 0; i < joinButtons.Length; i++)
        {
            joinButtons[i].Show();
        }
        createButton.Show();
    }

    public override void OnReceivedRoomListUpdate()
    {
        base.OnReceivedRoomListUpdate();
        int i;
        int j = 0;
        RoomInfo[] rooms = PhotonNetwork.GetRoomList();
        Debug.Log("Rooms: " + rooms.Length);
        for (i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].open && rooms[i].playerCount == 1)
            {
                j++;
                joinButtons[i].text.text = rooms[i].name;
                joinButtons[i].context = rooms[i];
                joinButtons[i].Show();
                if (j >= joinButtons.Length)
                {
                    i = rooms.Length;
                }
            }
            Debug.Log("Room [" + rooms[i].name + "] players: " + rooms[i].playerCount);
        }
        for(i = j; i < 4; i++)
        {
            joinButtons[i].Hide();
        }
        joinButtons[3].text.text = "Одиночный бой";
        joinButtons[3].Show();
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
        startButton.Show();
        startLocalButton.interactable = true;
        Debug.LogError("Create room failed");
    }

    public override void OnPhotonJoinRoomFailed(object[] codeAndMsg)
    {
        base.OnPhotonCreateRoomFailed(codeAndMsg);
        startButton.Show();
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
        canvasGameover.enabled = false;
        roomIdField.text = PlayerPrefs.GetString("nickname", Environment.UserName);
        lobbyPanel.enabled = false;
        createButton.Hide();
        for (i = 0; i < joinButtons.Length; i++)
        {
            joinButtons[i].Hide();
        }
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
        startButton.button.onClick.AddListener(delegate() {
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
                startButton.Show();
                startLocalButton.interactable = true;
                DestroyImmediate(gameNetwork);
            }
            startButton.Hide();
            startLocalButton.interactable = false;
            //CreateInternetMatch(roomIdField.text);
        });
        /*
        createButton.button.onClick.AddListener(delegate () {
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
            createButton.Hide();
        });
        */
        createButton.button.onClick.AddListener(delegate () {
            ClickJoinButton(-1);
        });
        joinButtons[0].button.onClick.AddListener(delegate () {
            ClickJoinButton(0);
        });
        joinButtons[1].button.onClick.AddListener(delegate () {
            ClickJoinButton(1);
        });
        joinButtons[2].button.onClick.AddListener(delegate () {
            ClickJoinButton(2);
        });
        joinButtons[3].button.onClick.AddListener(delegate () {
            ClickJoinButton(3);
        });
        startLocalButton.onClick.AddListener(delegate () {
            UpdatePreferences();
            gameNetwork = GameObject.Instantiate(gameNetworkPrefab).GetComponent<GameNetwork>();
            gameNetwork.camera = camera;
            gameNetwork.gameMatchMaker = this;
            gameNetwork.isLocal = true;
        });
        gameOverButton.onClick.AddListener(delegate() {
            canvasGameover.enabled = false;
            canvasConnect.enabled = true;
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

    public void ClickJoinButton(int index)
    {
        int i;
        RoomInfo roomInfo;
        RoomOptions roomOptions;
        if (index > -1 && index < 3)
        {
            ButtonController button = joinButtons[index];
            roomInfo = (RoomInfo)button.context;
            if (PhotonNetwork.JoinRoom(roomInfo.name))
            {
                Debug.Log("Room joining!");
            }
            else
            {
                Debug.LogError("Can't join room");
            }
        }
        else
        {
            roomOptions = new RoomOptions();
            roomOptions.IsOpen = true;
            roomOptions.IsVisible = true;
            if (index < 3)
            {
                roomOptions.MaxPlayers = 2;
            }
            else
            {
                roomOptions.MaxPlayers = 1;
            }
            if (PhotonNetwork.CreateRoom(roomIdField.text, roomOptions, lobby))
            {
                Debug.Log("Room creating!");
            }
            else
            {
                Debug.LogError("Can't create room");
            }
        }
        lobbyPanel.enabled = false;
        createButton.Hide();
        for (i = 0; i < joinButtons.Length; i++)
        {
            joinButtons[i].Hide();
        }
        PlayerPrefs.SetString("nickname", roomIdField.text);
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
                    camera.transform.position = playerObject.position * 100.0f + Vector3.up * 20.0f;
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
                    if(playerObject.position.z < 0.0f)
                    {
                        playerObject.visualObject.transform.Rotate(0.0f, 180.0f, 0.0f);
                    }
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
                GameOverMessage gameOverMessage = new GameOverMessage();
                gameOverMessage.Unpack((byte[])content);
                gameNetwork.RpcGameOver(gameOverMessage.winner, gameOverMessage.time, gameOverMessage.damage, gameOverMessage.wound);

                gameNetwork = GameObject.Instantiate(gameNetworkPrefab).GetComponent<GameNetwork>();
                gameNetwork.camera = camera;
                gameNetwork.gameMatchMaker = this;
                gameNetwork.isServer = false;
                gameNetwork.isLocal = false;

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
                Debug.Log("RECEIVE FLASH PASSIVE ABILITY. timemark: " + baseObjectMessage.timemark);
                baseObjectMessage.eventCode = eventCode;
                delayedMessages.AddLast(baseObjectMessage);
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
                visualEffectMessage.eventCode = eventCode;
                delayedMessages.AddLast(visualEffectMessage);
                break;
            case 14:
                PingMessage pingMessage = new PingMessage();
                PingMessage newPingMessage;
                pingMessage.Unpack((byte[])content);
                if(pingMessage.time == 0.0f)
                {
                    newPingMessage = new PingMessage(remoteTimestamp, pingMessage.timemark);
                    PhotonNetwork.networkingPeer.OpCustom((byte)4, new Dictionary<byte, object> { { 245, newPingMessage.Pack() } }, true);
                }
                else
                {
                    remoteTimestamp = pingMessage.timemark + pingMessage.time / 2.0f;
                }
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
                    case 11:
                        Debug.Log("@ INVOKE FLASH PASSIVE ABILITY[" + objMessageNode.Value.id + "]");
                        gameNetwork.RpcFlashPassiveAbility(objMessageNode.Value.id);
                        break;
                    case 13:
                        VisualEffectMessage visualEffectMessage = (VisualEffectMessage)objMessageNode.Value;
                        gameNetwork.RpcVisualEffect(visualEffectMessage.id, visualEffectMessage.invokerId, visualEffectMessage.targetId, visualEffectMessage.targetPosition, visualEffectMessage.direction, visualEffectMessage.duration);
                        break;
                }
                delayedMessages.Remove(objMessageNode);
            }
            objMessageNode = objMessageNodeNext;
        }
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
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
