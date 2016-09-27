using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.Networking.Match;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames;
using ExitGames.Client;
using ExitGames.Client.Photon;

public class GameNetwork : Photon.PunBehaviour {


    public enum ClientEvent {
        NONE = 0,
        CONNECTED = 1,
        THROW = 2,
        USE_ABILITY = 3
    }

    public enum ThrowState {
        NONE = 0,
        TOUCHED = 1
    }

    public GameObject[] bodyPrefabs;
    public GameObject[] obstructionPrefabs;
    public GameObject[] missilePrefabs;
    public GameObject floatNotifyPrefab;
    public GameObject fixedNotifyPrefab;
    public GameObject swipeTrailPrefab;
    public GameMatchMaker gameMatchMaker;
    public ScreenEffectsController screenEffects;
    public ArmedMissileController armedMissile;
    public SwipeTrailController swipeTrail = null;
    public AbilityButtonController abilityActiveButton;
    public AbilityButtonController abilityPassiveButton;
    public Button switchSwipeTypeButton;
    public Image staminaBar;
    public Text healthBarSelf;
    public Text healthBarEnemy;
    public Camera camera;
    public Location location;
    public SwipeController swipeController = new SwipeController();
    public int playerId = -1;
    public int myMissileId = 1;
    public int opponentMissileId = 1;
    public bool isLocal = false;
    public bool ready = false;
    public bool updating = false;
    public float updateTimeout = 0.2f;
    public float updateCooldown = 0.2f;
    public float lastTouchX = 0.0f;
    public float lastTouchY = 0.0f;

    public int swipeType = 1;
    public ThrowState throwState = ThrowState.NONE;

    private bool _isServer = false;
    private float touchTime = 0.0f;

    public new bool isServer {
        get {
            if(isLocal)
            {
                return true;
            }
            return _isServer;
        }
        set {
            _isServer = value;
        }
    }

    //[PunRPC]
    public void RpcSpawnObject(int id, Location.ObjectType objectType, Vector3 newPosition, Vector3 newVelocity, Vector3 newAcceleration, Vector3 newTorsion, float newFloat, int visualId)
    {
        PlayerController playerController = null;
        PlayerObject playerObject = null;
        PlayerObject playerObject2 = null;
        ObstructionController obstructionController = null;
        ObstructionObject obstructionObject = null;
        MissileController missileController = null;
        MissileObject missileObject = null;
        if (!isServer)
        {
            //Debug.Log("RpcSpawnObject [CLIENT]: " + id + "; " + newPosition);
            switch (objectType)
            {
                case Location.ObjectType.PLAYER:
                    playerObject = new PlayerObject();
                    playerObject.id = id;
                    playerObject.position = newPosition;
                    playerObject.velocity = newVelocity;
                    playerObject.acceleration = newAcceleration;
                    playerObject.torsion = newTorsion;
                    playerObject.scale = newFloat;
                    playerObject.health = 100.0f;
                    playerObject.direction = 1.0f;
                    /* duplicate for GameMatchMaker.OnEvent case 1 */
                    if (id != playerId && playerId != -1 && playerObject.visualObject == null)
                    {
                        playerController = (Instantiate(bodyPrefabs[0])).GetComponent<PlayerController>();
                        playerController.gameNetwork = this;
                        playerController.obj = playerObject;
                        playerObject.visualObject = playerController;
                        playerController.transform.position = playerObject.position * 100.0f;
                        //playerController.transform.localScale *= 20.0f;
                    }
                    if (id == playerId && playerId != -1)
                    {
                        camera.transform.position = playerObject.position * 100.0f + Vector3.up * 15.0f;
                        if (playerId == 1)
                        {
                            camera.transform.eulerAngles = new Vector3(camera.transform.eulerAngles.x, 180.0f, camera.transform.eulerAngles.z);
                        }
                    }
                    /* */
                    location.AddObject(playerObject);
                    Debug.Log("PLAYER SPAWNED [" + playerObject.id + "]");
                    break;
                case Location.ObjectType.OBSTRUCTION:
                    obstructionObject = new ObstructionObject();
                    obstructionObject.id = id;
                    obstructionObject.position = newPosition;
                    obstructionObject.scale = newFloat;
                    obstructionObject.visualId = visualId;
                    obstructionController = (Instantiate(obstructionPrefabs[obstructionObject.visualId])).GetComponent<ObstructionController>();
                    obstructionController.obj = obstructionObject;
                    obstructionObject.visualObject = obstructionController;
                    obstructionController.transform.position = obstructionObject.position * 100.0f;
                    obstructionController.transform.localScale *= 20.0f;
                    location.AddObject(obstructionObject);
                    break;
                case Location.ObjectType.MISSILE:
                    LocationObject locationObject = location.GetObject(playerId);
                    Debug.Log("GET PLAYER OBJECT[" + playerId + "]: " + (locationObject != null));
                    Debug.Log("LocationObject[" + locationObject.id +  "]: " + locationObject.objectType);
                    playerObject = (PlayerObject)location.GetObject(playerId);
                    if (Mathf.Abs(newPosition.z - playerObject.position.z) < 0.5f) // my missile
                    {
                        //missileObject.position = armedMissile.transform.position / 10.0f;
                        //missileObject.position = newPosition;
                        //missileObject.velocity = newVelocity;
                        //missileObject.acceleration = newAcceleration;
                        //missileObject.torsion = newTorsion;
                    }
                    else
                    {
                        missileObject = new MissileObject();
                        missileObject.id = id;
                        missileObject.position = newPosition;
                        missileObject.velocity = newVelocity;
                        missileObject.acceleration = newAcceleration;
                        missileObject.torsion = newTorsion;
                        missileObject.scale = newFloat;
                        missileController = (Instantiate(missilePrefabs[opponentMissileId])).GetComponent<MissileController>();
                        missileController.obj = missileObject;
                        missileController.torsion = newFloat;
                        missileObject.visualObject = missileController;
                        missileController.transform.position = missileObject.position * 100.0f;
                        missileController.transform.localScale *= 20.0f;
                        location.AddObject(missileObject);
                    }
                    //if (Mathf.Abs(newPosition.z) < 0.1f)
                    //{
                    //    missileController.transform.rotation = armedMissile.transform.rotation;
                    //}
                    break;
            }
        }
        else
        {
            //Debug.Log("RpcMoveObject [SERVER]: " + id + "; " + newPosition);
        }
    }

    //[PunRPC]
    public void RpcDestroyObject(int id)
    {
        if (!isServer)
        {
            LocationObject locationObject = GetLocationObject(id);
            if (locationObject != null)
            {
                switch (locationObject.objectType)
                {
                    case Location.ObjectType.PLAYER:
                        PlayerObject playerObject = (PlayerObject)locationObject;
                        if(playerObject.visualObject != null)
                        {
                            GameObject.Destroy(playerObject.visualObject.gameObject);
                        }
                        location.RemoveObject(playerObject);
                        break;
                    case Location.ObjectType.OBSTRUCTION:
                        ObstructionObject obstructionObject = (ObstructionObject)locationObject;
                        if (obstructionObject.visualObject != null)
                        {
                            GameObject.Destroy(obstructionObject.visualObject.gameObject);
                        }
                        location.RemoveObject(obstructionObject);
                        break;
                    case Location.ObjectType.MISSILE:
                        MissileObject missileObject = (MissileObject)locationObject;
                        if (missileObject.visualObject != null)
                        {
                            GameObject.Destroy(missileObject.visualObject.gameObject);
                        }
                        location.RemoveObject(missileObject);
                        break;
                }
            }
        }
    }

    //[PunRPC]
    public void RpcMoveObject(int id, Vector3 newPosition, Vector3 newVelocity, Vector3 newAcceleration, Vector3 newTorsion, float newScale, float messageTimestamp)
    {
        if (!isServer)
        {
            //Debug.Log("RpcMoveObject [CLIENT]: " + id + "; " + newPosition);
            //transform.position += Vector3.right * (newPosition.x - transform.position.x);
            LocationObject obj = GetLocationObject(id);
            if (obj != null)
            {
                //if (messageTimestamp > obj.lastRemoteTimestamp)
                //{
                if(obj.lastRemoteTimestamp <= 0.0f)
                {
                    //obj.lastPosition = newPosition;
                    //obj.lastRemoteTimestamp = messageTimestamp;
                }
                obj.position = newPosition;
                obj.velocity = newVelocity;
                obj.acceleration = newAcceleration;
                obj.torsion = newTorsion;
                obj.scale = newScale;
                obj.lastPosition = newPosition;
                obj.lastRemoteTimestamp = messageTimestamp;
                obj.lastTimestamp = Time.time;
                //}
            }
        }
        else
        {
            //Debug.Log("RpcMoveObject [SERVER]: " + id + "; " + newPosition);
        }
    }

    //[PunRPC]
    public void RpcUpdatePlayer(int id, float health, float stamina, float staminaConsumption)
    {
        //Debug.Log("RPC UPDATE PLAYER[" + id + "] health: " + health + " ; stamina: " + stamina);
        //if (!isServer)
        //{
            PlayerObject playerObject;
            LocationObject obj = GetLocationObject(id);
            if (obj != null)
            {
                //Debug.Log("RPC UPDATE PLAYER[" + id + "] found");
                if (obj.objectType == Location.ObjectType.PLAYER)
                {
                    //Debug.Log("RPC UPDATE PLAYER[" + id + "] is PLAYER. changed!");
                    playerObject = (PlayerObject)obj;
                    playerObject.health = health;
                    playerObject.stamina = stamina;
                    playerObject.staminaConsumption = stamina;
                }
            }
        //}
    }

    //[PunRPC]
    public void RpcRearmMissile()
    {
        if (!isServer)
        {
            armedMissile.Rearm();
        }
    }

    //[PunRPC]
    public void RpcFlashPlayer(int id)
    {
        if (!isServer)
        {
            FlashPlayer(id);
        }
    }

    //[PunRPC]
    public void RpcGameOver(int winner)
    {
        if (!isServer)
        {
            GameOver(winner);
        }
    }

    //[PunRPC]
    public void RpcSetAbility(int id, int value)
    {
        SetAbility(id, value);
    }

    public void SetAbility(int id, int value)
    {
        string abilityLabel = "";
        switch (value)
        {
            case 1:
                abilityLabel = "Щ";
                break;
            case 2:
                abilityLabel = "К";
                break;
        }
        switch (id)
        {
            case 0:
                abilityPassiveButton.text.text = abilityLabel;
                break;
            case 1:
                abilityActiveButton.text.text = abilityLabel;
                break;
        }
    }

    //[PunRPC]
    public void RpcShowNotice(int target, string message, float offset, int color, bool floating)
    {
        //if (!isServer)
        //{
            ShowNotice(target, message, offset, color, floating);
        //}
    }

    public void ShowNotice(int target, string message, float offset, int color, bool floating)
    {
        if (floating)
        {
            //Debug.Log("FLOATING NOTICE[" + target + ":" + playerId + "]");
            float distanceScale = 1.0f;
            FloatingNotifyController floatingNotify = GameObject.Instantiate(floatNotifyPrefab).GetComponent<FloatingNotifyController>();
            floatingNotify.Show(message, color);
            if (target != playerId)
            {
                if (location != null)
                {
                    PlayerObject playerObject = (PlayerObject)location.GetObject(target);
                    if (playerObject != null && playerObject.visualObject != null)
                    {
                        floatingNotify.transform.localScale = Vector3.one * Mathf.Pow(Mathf.Abs(playerObject.visualObject.transform.position.z), 0.5f) * 5.0f;
                        floatingNotify.transform.position = playerObject.visualObject.transform.position + playerObject.visualObject.transform.right * 3.0f + playerObject.visualObject.transform.forward * 2.0f + Vector3.up * (7.5f - 3.0f * offset);
                        if(playerId == 1)
                        {
                            floatingNotify.transform.Rotate(0.0f, 180.0f, 0.0f);
                        }
                    }
                }
            }
            else if (target == playerId)
            {
                floatingNotify.transform.position = camera.transform.position + Vector3.right * -0.2f + Vector3.forward * 1.0f + Vector3.up * (-0.95f + 0.5f * offset);
            }
        }
        else
        {
            //Debug.Log("FIXED NOTICE[" + target + ":" + playerId + "]");
            FixedNotifyController fixedNotify = GameObject.Instantiate(fixedNotifyPrefab).GetComponent<FixedNotifyController>();
            fixedNotify.text.rectTransform.SetParent(gameMatchMaker.canvasPlay.transform);
            if (target == playerId)
            {
                fixedNotify.Show(0, message, color, offset);
            }
            else
            {
                fixedNotify.Show(1, message, color, offset);
            }
        }
    }

    public void OnUseAbility(int id)
    {
        BaseObjectMessage baseMessage = new BaseObjectMessage();
        baseMessage.id = id;
        PhotonNetwork.networkingPeer.OpCustom((byte)3, new Dictionary<byte, object> { { 245, baseMessage.Pack() } }, true);
    }

    public void UseAbility(int id)
    {
        PlayerObject playerObject = null;
        playerObject = (PlayerObject)location.GetObject(id);
        if (playerObject != null)
        {
            if(playerObject.abilityShield == 0.0f)
            {
                playerObject.abilityShield = 10.0f;
                ShowNotice(playerObject.id, "+ ЩИТ", 5.0f, 0, false);
                if (!isLocal)
                {
                    RpcShowNotice(playerObject.id, "+ ЩИТ", 5.0f, 0, false);
                }
            }
            else if(playerObject.abilityStun == 0.0f)
            {
                playerObject.abilityStun = 10.0f;
                playerObject.stunMove = 5.0f;
                ShowNotice(playerObject.id, "+ ОГЛУШЕНИЕ", 3.0f, 0, false);
                if (!isLocal)
                {
                    RpcShowNotice(playerObject.id, "+ ОГЛУШЕНИЕ", 3.0f, 0, false);
                }
            }
        }
    }

    //[PunRPC]
    public void RpcFlashPassiveAbility(int id)
    {
        //if (!isServer)
        //{
            FlashPassiveAbility(id);
        //}
    }

    public void FlashPassiveAbility(int id)
    {
        /*
        PlayerObject playerObject = null;
        playerObject = (PlayerObject)location.GetObject(id);
        if (playerObject != null)
        {
            abilityPassiveButton.Activate(5.0f);
        }
        */
        if(
            abilityPassiveButton.text.text == "К" && id == 2
            || abilityPassiveButton.text.text == "У" && id == 4
            )
        {
            abilityPassiveButton.Activate(5.0f);
        }
        if (
            abilityActiveButton.text.text == "К" && id == 2
            || abilityActiveButton.text.text == "У" && id == 4
            )
        {
            abilityActiveButton.Activate(5.0f);
        }
    }

    //[PunRPC]
    public void RpcFlashObstruction(int id)
    {
        //if(!isServer)
        //{
            FlashObstruction(id);
        //}
    }

    public void FlashObstruction(int id)
    {
        LocationObject locationObject = location.GetObject(id);
        ObstructionObject obstructionObject;
        if (locationObject != null)
        {
            obstructionObject = (ObstructionObject)locationObject;
            if (obstructionObject.visualObject != null)
            {
                //Debug.Log("FlashObstruction[" + obstructionObject.id + "]");
                obstructionObject.visualObject.Flash();
            }
        }
    }

    public void RpcVisualEffect(int id, int invokerId, int targetId, float duration)
    {
        VisualEffect((Location.VisualEffects)id, invokerId, targetId, duration);
    }

    public void VisualEffect(Location.VisualEffects effect, int invokerId, int targetId, float duration)
    {
        PlayerObject playerObject;
        switch(effect)
        {
            case Location.VisualEffects.RAVEN:
                gameMatchMaker.visualEffectRaven.Activate();
                playerObject = (PlayerObject) location.GetObject(targetId);
                if (playerObject.visualObject != null)
                {
                    gameMatchMaker.visualEffectRaven.transform.parent = playerObject.visualObject.transform;
                }
                else
                {
                    gameMatchMaker.visualEffectRaven.transform.parent = camera.transform;
                }
                break;
        }
    }

    public void GameOver(int winner)
    {
        location.Cleanup();
        if (isServer)
        {
            if (!isLocal)
            {
                NetManager.singleton.matchMaker.DestroyMatch(NetManager.singleton.matchInfo.networkId, 0, null);
                //NetManager.singleton.StopHost();
                //NetManager.singleton.StopServer();
            }
        }
        else
        {
            NetManager.singleton.StopClient();
        }
        LocationObject.lastObjectId = 0;
        gameMatchMaker.canvasPlay.enabled = false;
        gameMatchMaker.canvasConnect.enabled = true;
        if (isLocal)
        {
            Destroy(gameObject);
        }
    }

    void Start ()
    {
        location = new Location();
        location.SetNetworkBehavior(this);
        //Debug.Log("Start SERVER=" + isServer);
        if(gameMatchMaker == null)
        {
            gameMatchMaker = GameObject.Find("NetworkManager").GetComponent<GameMatchMaker>();
            camera = GameObject.Find("Camera").GetComponent<Camera>();
        }
        armedMissile = GameObject.Find("ArmedMissile").GetComponent<ArmedMissileController>();
        screenEffects = GameObject.Find("ScreenEffects").GetComponent<ScreenEffectsController>();
        staminaBar = GameObject.Find("StaminaBar").GetComponent<Image>();
        healthBarSelf = GameObject.Find("HealthValueSelf").GetComponent<Text>();
        healthBarEnemy = GameObject.Find("HealthValueEnemy").GetComponent<Text>();
        abilityActiveButton = GameObject.Find("AbilityActiveButton").GetComponent<AbilityButtonController>();
        abilityPassiveButton = GameObject.Find("AbilityPassiveButton").GetComponent<AbilityButtonController>();
        /*
        switchSwipeTypeButton = GameObject.Find("SwitchSwipeType").GetComponent<Button>();
        switchSwipeTypeButton.onClick.AddListener(delegate() {
            swipeController.swipeType++;
            if(swipeController.swipeType > 2)
            {
                swipeController.swipeType = 1;
            }
            switchSwipeTypeButton.GetComponentInChildren<Text>().text = "РЕЖИМ\nСВАЙПА\n#" + swipeController.swipeType;
        });
        */
        swipeController.OnInvokeAction += OnThrow;
        if (!isServer)
        {
            ClientInit();
        }
        else
        {
            ServerInit();
        }
        Debug.Log("Start. isLocal: " + isLocal + "; isServer: " + isServer);
        if(isLocal)
        {
            ready = true;
            InitializeLocation();
            gameMatchMaker.canvasConnect.enabled = false;
            gameMatchMaker.canvasSettings.enabled = false;
            gameMatchMaker.canvasPlay.enabled = true;
        }
    }

    public void ClientInit()
    {
        if(isServer && !isLocal)
        {
            OnClientReady();
        }
        //SendSimpleMessage(ClientEvent.CONNECTED);
        abilityPassiveButton.button.onClick.AddListener(delegate () {
            if (abilityPassiveButton.Activate(10.0f))
            {
                OnUseAbility(0);
                //SendSimpleMessage(ClientEvent.USE_ABILITY);
            }
        });
        abilityActiveButton.button.onClick.AddListener(delegate() {
            if (abilityActiveButton.Activate(10.0f))
            {
                OnUseAbility(1);
                //SendSimpleMessage(ClientEvent.USE_ABILITY);
            }
        });
    }

    [PunRPC]
    public void OnClientReady()
    {
        if(isServer)
        {
            Debug.Log("OnClientReady [Server]");
            InitializeLocation();
            gameMatchMaker.canvasConnect.enabled = false;
            gameMatchMaker.canvasSettings.enabled = false;
            gameMatchMaker.canvasPlay.enabled = true;
        }
        else
        {
            Debug.Log("OnClientReady [Client]");
        }
    }

    public void ServerInit()
    {
        PhotonPeer.RegisterType(typeof(FourFloatMessage), (byte)'A', SerializeGameMessage, DeserializeGameMessage);
        PhotonPeer.RegisterType(typeof(FourFloatMessage), (byte)'C', SerializeFourFloatMessage, DeserializeFourFloatMessage);
        /*
        NetworkServer.RegisterHandler(100, ReceiveClientSimpleMessage);
        NetworkServer.RegisterHandler(101, ReceiveClientIntMessage);
        NetworkServer.RegisterHandler(102, ReceiveClientFloatMessage);
        NetworkServer.RegisterHandler(103, ReceiveClientDoubleFloatMessage);
        NetworkServer.RegisterHandler(104, ReceiveClientFourFloatMessage);
        */
        abilityActiveButton.button.onClick.AddListener(delegate () {
            if (abilityActiveButton.Activate(10.0f))
            {
                UseAbility(0);
            }
        });
    }

    private static byte[] SerializeGameMessage(object customobject)
    {
        GameMessage o = (GameMessage)customobject;

        byte[] bytes = new byte[4];
        int index = 0;
        Protocol.Serialize((int)o.clientEvent, bytes, ref index);
        return bytes;
    }

    private static object DeserializeGameMessage(byte[] bytes)
    {
        FourFloatMessage o = new FourFloatMessage();
        int n = 0;
        int index = 0;
        Protocol.Deserialize(out n, bytes, ref index);
        o.clientEvent = (ClientEvent)n;
        return o;
    }

    private static byte[] SerializeFourFloatMessage(object customobject)
    {
        FourFloatMessage o = (FourFloatMessage)customobject;

        byte[] bytes = new byte[5 * 4];
        int index = 0;
        Protocol.Serialize((int)o.clientEvent, bytes, ref index);
        Protocol.Serialize(o.value1, bytes, ref index);
        Protocol.Serialize(o.value2, bytes, ref index);
        Protocol.Serialize(o.value3, bytes, ref index);
        Protocol.Serialize(o.value4, bytes, ref index);
        return bytes;
    }

    private static object DeserializeFourFloatMessage(byte[] bytes)
    {
        FourFloatMessage o = new FourFloatMessage();
        int n = 0;
        int index = 0;
        Protocol.Deserialize(out n, bytes, ref index);
        o.clientEvent = (ClientEvent)n;
        Protocol.Deserialize(out o.value1, bytes, ref index);
        Protocol.Deserialize(out o.value2, bytes, ref index);
        Protocol.Deserialize(out o.value3, bytes, ref index);
        Protocol.Deserialize(out o.value4, bytes, ref index);
        return o;
    }

    public void ReceiveClientSimpleMessage(NetworkMessage netMsg)
    {
        SimpleEventMessage msg = netMsg.ReadMessage<SimpleEventMessage>();
        switch(msg.clientEvent)
        {
            case ClientEvent.CONNECTED:
                //Debug.Log("ClientConnected");
                ready = true;
                InitializeLocation();
                break;
            case ClientEvent.USE_ABILITY:
                UseAbility(1);
                break;
        }
        //Debug.Log("Received[" + msg.clientEvent + "]");
    }

    public void ReceiveClientIntMessage(NetworkMessage netMsg)
    {
        IntEventMessage msg = netMsg.ReadMessage<IntEventMessage>();
        switch (msg.clientEvent)
        {
            case ClientEvent.USE_ABILITY:
                break;
        }
        //Debug.Log("Received[" + msg.clientEvent + "]: " + msg.value);
    }

    public void ReceiveClientFloatMessage(NetworkMessage netMsg)
    {
        FloatEventMessage msg = netMsg.ReadMessage<FloatEventMessage>();
        switch (msg.clientEvent)
        {
            case ClientEvent.NONE:
                break;
        }
        //Debug.Log("Received[" + msg.clientEvent + "]: " + msg.value);
    }

    public void ReceiveClientDoubleFloatMessage(NetworkMessage netMsg)
    {
        DoubleFloatEventMessage msg = netMsg.ReadMessage<DoubleFloatEventMessage>();
        switch (msg.clientEvent)
        {
        }
        //Debug.Log("Received[" + msg.clientEvent + "]: " + msg.value1 + " ; " + msg.value2);
    }

    public void ReceiveClientFourFloatMessage(NetworkMessage netMsg)
    {
        FourFloatEventMessage msg = netMsg.ReadMessage<FourFloatEventMessage>();
        switch (msg.clientEvent)
        {
            case ClientEvent.THROW:
                Throw(1, new Vector2(msg.value1, msg.value2), msg.value3, msg.value4);
                break;
        }
        //Debug.Log("Received[" + msg.clientEvent + "]: " + msg.value1 + " ; " + msg.value2);
    }

    public void SendSimpleMessage(ClientEvent clientEvent)
    {
        if(isLocal)
        {
            return;
        }
        SimpleEventMessage msg = new SimpleEventMessage();
        msg.clientEvent = clientEvent;
        NetworkManager.singleton.client.Send(100, msg);
    }

    public void SendIntMessage(ClientEvent clientEvent, int value)
    {
        if (isLocal)
        {
            return;
        }
        IntEventMessage msg = new IntEventMessage();
        msg.clientEvent = clientEvent;
        msg.value = value;
        NetworkManager.singleton.client.Send(101, msg);
    }

    public void SendFloatMessage(ClientEvent clientEvent, float value)
    {
        if (isLocal)
        {
            return;
        }
        FloatEventMessage msg = new FloatEventMessage();
        msg.clientEvent = clientEvent;
        msg.value = value;
        NetworkManager.singleton.client.Send(102, msg);
    }

    public void SendDoubleFloatMessage(ClientEvent clientEvent, float value1, float value2)
    {
        if (isLocal)
        {
            return;
        }
        DoubleFloatEventMessage msg = new DoubleFloatEventMessage();
        msg.clientEvent = clientEvent;
        msg.value1 = value1;
        msg.value2 = value2;
        NetworkManager.singleton.client.Send(103, msg);
    }

    public void SendFourFloatMessage(ClientEvent clientEvent, float value1, float value2, float value3, float value4)
    {
        if (isLocal)
        {
            return;
        }
        FourFloatEventMessage msg = new FourFloatEventMessage();
        msg.clientEvent = clientEvent;
        msg.value1 = value1;
        msg.value2 = value2;
        msg.value3 = value3;
        msg.value4 = value4;
        NetworkManager.singleton.client.Send(104, msg);
    }


    void Update ()
    {
        if (location != null)
        {
            location.UpdateCycle(Time.deltaTime);
            if (ready && isServer)
            {
                location.PhysicCycle(Time.deltaTime);
            }
        }
        /*
    }

    void OnGUI ()
    {
        */
        float angle = 0.0f;
        float power = 0.0f;
        float mouseX = Input.mousePosition.x / (float)Screen.width;
        float mouseY = 1.0f - Input.mousePosition.y / (float)Screen.height;
        float touchX = 0.0f;
        float touchY = 0.0f;
        Vector3 position = Vector3.zero;
#if !UNITY_STANDALONE
        Touch touch;
        if (Input.touchCount > 0)
        {
            touch = Input.GetTouch(0);
            touchX = touch.position.x / (float)Screen.width;
            touchY = 1.0f - touch.position.y / (float)Screen.height;
            position = (camera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 0.1f)) - camera.transform.position);
            if (throwState != ThrowState.TOUCHED && touchX > 0.25f && touchX < 0.75f && touchY > 0.8f && touchY < 1.0f)
            {
                throwState = ThrowState.TOUCHED;
                swipeTrail = ((GameObject)GameObject.Instantiate(swipeTrailPrefab, position, Quaternion.identity)).GetComponent<SwipeTrailController>();
                swipeTrail.transform.parent = camera.transform;
                swipeTrail.transform.localPosition = Vector3.zero;
            }
            swipeTrail.lineRenderer.SetVertexCount(swipeTrail.pointsCount);
            swipeTrail.lineRenderer.SetPosition(swipeTrail.pointsCount - 1, position);
            swipeTrail.pointsCount++;
            lastTouchX = touchX;
            lastTouchY = touchY;
            if (throwState == ThrowState.TOUCHED)
            {
                armedMissile.SetAnchor(new Vector2(lastTouchX, 1.0f - lastTouchY), touchTime);
            }
        }
        else
        {
            if (throwState == ThrowState.TOUCHED)
            {
                throwState = ThrowState.NONE;
                armedMissile.ResetAnchor();
            }
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            position = (camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.1f)) - camera.transform.position);
            if (throwState != ThrowState.TOUCHED)
            {
                swipeTrail = ((GameObject)GameObject.Instantiate(swipeTrailPrefab, position, Quaternion.identity)).GetComponent<SwipeTrailController>();
                swipeTrail.transform.parent = camera.transform;
                swipeTrail.transform.localPosition = Vector3.zero;
                swipeTrail.lineRenderer.SetPosition(0, position);
                //if (mouseX > 0.25f && mouseX < 0.75f && mouseY > 0.8f && mouseY < 1.0f)
                //{
                throwState = ThrowState.TOUCHED;
                //}
            }
        }
        if( Input.GetMouseButton(0))
        {
            lastTouchX = mouseX;
            lastTouchY = mouseY;
            if (swipeTrail != null)
            {
                position = (camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.1f)) - camera.transform.position);
                swipeTrail.pointsCount++;
                swipeTrail.lineRenderer.SetVertexCount(swipeTrail.pointsCount);
                swipeTrail.lineRenderer.SetPosition(swipeTrail.pointsCount - 1, position);
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (throwState == ThrowState.TOUCHED)
            {
                throwState = ThrowState.NONE;
                //if (mouseY < 0.75f)
                //{
                //}
            }
        }
        if (throwState == ThrowState.TOUCHED)
        {
            armedMissile.SetAnchor(new Vector2(lastTouchX, 1.0f - lastTouchY), touchTime);
        }
#endif
        swipeController.AddPoint(new Vector2(lastTouchX, lastTouchY), Time.deltaTime, throwState == ThrowState.TOUCHED);
        if (throwState == ThrowState.TOUCHED)
        {
            touchTime += Time.deltaTime;
            armedMissile.transform.localRotation = Quaternion.AngleAxis(Mathf.Min(60.0f, transform.localRotation.eulerAngles.x + touchTime * 180.0f), Vector3.right);
        }
        else
        {
            if (touchTime > 0.0f)
            {
                touchTime = 0.0f;
                armedMissile.Rearm();
            }
        }
    }

    public void OnThrow(object sender, SwipeEventArgs e)
    {
        Throw(playerId, e.angle, e.torsion, e.speed);
        //if (isServer)
        //{
        //}
        //else
        //{
            ThrowMessage throwMessage = new ThrowMessage();
            throwMessage.id = 0;
            throwMessage.angleX = e.angle.x;
            throwMessage.angleY = e.angle.y;
            throwMessage.torsion = e.torsion;
            throwMessage.speed = e.speed;
            PhotonNetwork.networkingPeer.OpCustom((byte)2, new Dictionary<byte, object> { { 245, throwMessage.Pack() } }, true);
            //SendFourFloatMessage(ClientEvent.THROW, e.angle.x, e.angle.y, e.torsion, e.speed);
        //}
        throwState = ThrowState.NONE;
    }

    public void Throw(int player, Vector2 angle, float torsion, float speed)
    {
        MissileController missileController;
        MissileObject missileObject;
        PlayerObject playerObject;
        LocationObject playerLocationObject = GetLocationObject(player);
        float staminaConsumption = 0.0f;
        float trimmedSpeed = Mathf.Min(1.2f, Math.Max(1.0f, speed));
        float horizontalAngle = Mathf.Min(22.0f, Math.Max(-22.0f, angle.x));
        float t = 1.0f / trimmedSpeed;
        if (playerLocationObject != null)
        {
            playerObject = (PlayerObject)playerLocationObject;
            staminaConsumption = playerObject.staminaConsumption;
            if(playerObject.armInjury > 0.0f)
            {
                staminaConsumption *= 1.0f + playerObject.armInjuryEffect;
            }
            if (playerObject.stamina >= staminaConsumption)
            {
                playerObject.stamina -= staminaConsumption;
                missileController = (Instantiate(missilePrefabs[myMissileId])).GetComponent<MissileController>();
                missileController.gameNetwork = this;
                missileObject = new MissileObject();
                //missileObject.position = new Vector3(playerLocationObject.position.x, 0.2f + angle.y * 0.2f, 0.1f);
                //missileObject.direction = (new Vector3(0.0f, Mathf.Min(1.0f, 0.2f + Mathf.Min(0.5f, angle.y)) * 0.09f, Mathf.Min(0.5f, Math.Max(0.2f, speed / 5.0f)))).normalized;
                //missileObject.direction = Quaternion.Euler(0, Mathf.Min(30.0f, Mathf.Max(-30.0f, angle.x)), 0) * missileObject.direction;
                //missileObject.torsion = new Vector3(0.0f, torsion, 0.0f);

                missileObject.position = armedMissile.transform.position * 0.01f; //new Vector3(playerLocationObject.position.X + playerLocationObject.velocity.X * (currentTimestamp - playerLocationObject.lastTimestamp + 0.2f), 0.2f + angle.Y * 0.2f, playerLocationObject.position.Z + viewDirection.Z * 0.1f);
                missileObject.acceleration = new Vector3(0.0f, Location.gravity, 0.0f);
                missileObject.velocity = new Vector3(0.0f, Math.Min(0.05f, Math.Max(-0.15f, (angle.y - 0.35f) * 2.5f)) - missileObject.acceleration.y / 2, trimmedSpeed + Math.Abs(horizontalAngle) / 30.0f * 0.1f); // !!! not trigonometrical coeficient
                missileObject.velocity = Quaternion.Euler(0.0f, horizontalAngle + (1.0f + playerObject.position.z / Mathf.Abs(playerObject.position.z)) * 90.0f, 0.0f) * missileObject.velocity; // / 180.0f * (float)Math.PI
                missileObject.torsion = new Vector3(0.0f, Math.Min(90.0f, Math.Max(-90.0f, torsion)), 0.0f);

                if (isServer)
                {
                    if (player == playerId)
                    {
                        //armedMissile.Rearm();
                    }
                    else
                    {
                        if (!isLocal)
                        {
                            RpcRearmMissile();
                        }
                    }
                }
                //missileObject.velocity = Mathf.Min(1.2f, Mathf.Max(0.8f, speed)) * 8.0f;
                missileController.obj = missileObject;
                missileController.torsion = missileObject.torsion.y;
                missileObject.id *= -1;
                Debug.Log("ADD LOCAL MISSILE: " + missileObject.id);
                location.AddObject(missileObject);
                missileController.transform.position = missileObject.position * 100.0f;
                missileController.transform.localScale *= 20.0f;

                DestroyObjectMessage destroyObjectMessage = new DestroyObjectMessage();
                destroyObjectMessage.id = missileObject.id;
                destroyObjectMessage.objectId = missileObject.id;
                destroyObjectMessage.timemark = gameMatchMaker.GetRemoteTimestamp() + t;
                destroyObjectMessage.eventCode = 3;
                gameMatchMaker.AddDelayedMessage(destroyObjectMessage);

            }
        }
    }

    public void FlashPlayer(int id)
    {
        PlayerObject playerObject;
        LocationObject obj;
        if ((id == 1 && isServer) || (id == 0 && !isServer))
        {
            obj = GetLocationObject(id);
            if (obj != null)
            {
                playerObject = (PlayerObject)obj;
                if (playerObject.visualObject != null)
                {
                    playerObject.visualObject.Flash();
                }
            }
        }
        else
        {
            screenEffects.RedFlash();
        }
    }

    public void InitializeLocation()
    {
        int i;
        int visualId = 0;
        PlayerController playerController;
        ObstructionController obstructionController;
        PlayerObject playerObject;
        ObstructionObject obstructionObject;

        playerObject = new PlayerObject();
        playerObject.position = new Vector3(0.0f, 0.0f, 5.0f);
        playerObject.direction = 1.0f;
        playerObject.strafeMinTimeout = 0.9f;
        playerObject.strafeMaxTimeout = 2.2f;
        playerObject.health = gameMatchMaker.preferenceHealth;
        playerObject.stamina = gameMatchMaker.preferenceStamina;
        playerObject.maxStamina = gameMatchMaker.preferenceStamina;
        playerObject.staminaConsumption = gameMatchMaker.preferenceStaminaConsume;
        playerObject.staminaRegeneration = gameMatchMaker.preferenceStaminaRegeneration;
        playerObject.minDamage = gameMatchMaker.preferenceMinDamage;
        playerObject.maxDamage = gameMatchMaker.preferenceMaxDamage;
        playerObject.critChance = gameMatchMaker.preferenceCritChance;
        playerObject.critMultiplier = gameMatchMaker.preferenceCritMultiplier;
        playerObject.injuryChance = gameMatchMaker.preferenceInjureChance;
        playerObject.armInjuryEffect = gameMatchMaker.preferenceInjureArmEffect;
        playerObject.legInjuryEffect = gameMatchMaker.preferenceInjureLegEffect;
        playerObject.abilityEvadeChance = gameMatchMaker.preferenceAbilityEvadeChance;
        playerObject.abilityCritChance = gameMatchMaker.preferenceAbilityCritChance;
        playerObject.abilityStunDuration = gameMatchMaker.preferenceAbilityStunDuration;
        playerObject.abilityShieldDuration = gameMatchMaker.preferenceAbilityShieldDuration;
        playerObject.abilityShieldMultiplier = gameMatchMaker.preferenceAbilityShieldMultiplier;
        playerObject.strafeSpeed = gameMatchMaker.preferenceStrafeSpeed;
        if (UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f)
        {
            playerObject.abilityEvade = 0.0f;
            playerObject.abilityStun = 0.0f;
            //SetAbility(false, 0);
            //SetAbility(true, 0);
        }
        else
        {
            playerObject.abilityCrit = 0.0f;
            playerObject.abilityShield = 0.0f;
            //SetAbility(false, 1);
            //SetAbility(true, 1);
        }
        location.AddObject(playerObject);

        playerController = (Instantiate(bodyPrefabs[0])).GetComponent<PlayerController>();
        playerController.gameNetwork = this;
        playerObject = new PlayerObject();
        playerObject.position = new Vector3(0.0f, 0.0f, 5.0f);
        playerObject.direction = 1.0f;
        playerObject.strafeMinTimeout = 0.9f;
        playerObject.strafeMaxTimeout = 2.2f;
        playerObject.health = gameMatchMaker.preferenceHealth;
        playerObject.stamina = gameMatchMaker.preferenceStamina;
        playerObject.maxStamina = gameMatchMaker.preferenceStamina;
        playerObject.staminaConsumption = gameMatchMaker.preferenceStaminaConsume;
        playerObject.staminaRegeneration = gameMatchMaker.preferenceStaminaRegeneration;
        playerObject.minDamage = gameMatchMaker.preferenceMinDamage;
        playerObject.maxDamage = gameMatchMaker.preferenceMaxDamage;
        playerObject.critChance = gameMatchMaker.preferenceCritChance;
        playerObject.critMultiplier = gameMatchMaker.preferenceCritMultiplier;
        playerObject.injuryChance = gameMatchMaker.preferenceInjureChance;
        playerObject.armInjuryEffect = gameMatchMaker.preferenceInjureArmEffect;
        playerObject.legInjuryEffect = gameMatchMaker.preferenceInjureLegEffect;
        playerObject.abilityEvadeChance = gameMatchMaker.preferenceAbilityEvadeChance;
        playerObject.abilityCritChance = gameMatchMaker.preferenceAbilityCritChance;
        playerObject.abilityStunDuration = gameMatchMaker.preferenceAbilityStunDuration;
        playerObject.abilityShieldDuration = gameMatchMaker.preferenceAbilityShieldDuration;
        playerObject.abilityShieldMultiplier = gameMatchMaker.preferenceAbilityShieldMultiplier;
        playerObject.strafeSpeed = gameMatchMaker.preferenceStrafeSpeed;
        if (UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f)
        {
            playerObject.abilityEvade = 0.0f;
            playerObject.abilityStun = 0.0f;
            if (!isLocal)
            {
                //RpcSetAbility(false, 0);
                //RpcSetAbility(true, 0);
            }
        }
        else
        {
            playerObject.abilityCrit = 0.0f;
            playerObject.abilityShield = 0.0f;
            if (!isLocal)
            {
                //RpcSetAbility(false, 1);
                //RpcSetAbility(true, 1);
            }
        }
        playerController.obj = playerObject;
        location.AddObject(playerObject);
        playerController.transform.position = playerObject.position;

        for (i = 0; i < 4; i++)
        {
            visualId = Math.Min(UnityEngine.Random.Range(0, obstructionPrefabs.Length), obstructionPrefabs.Length - 1);
            if(visualId == 2)
            {
                visualId = 1;
            }
            obstructionController = (Instantiate(obstructionPrefabs[visualId])).GetComponent<ObstructionController>();
            obstructionController.gameNetwork = this;
            obstructionObject = new ObstructionObject();
            obstructionObject.position = new Vector3(((float)(i - 2)) + UnityEngine.Random.Range(0.01f, 0.9f), 0.0f, UnityEngine.Random.Range(1.2f, 2.5f));
            switch(visualId)
            {
                case 0:
                    obstructionObject.scale = 0.3f;
                    obstructionObject.durability = 1000.0f;
                    break;
                case 1:
                    obstructionObject.scale = 0.1f;
                    obstructionObject.durability = 1000.0f;
                    obstructionObject.position.z = 1.2f;
                    break;
                case 3:
                    obstructionObject.scale = 0.18f;
                    obstructionObject.durability = 1000.0f;
                    break;
            }
            obstructionObject.visualId = visualId;
            obstructionController.obj = obstructionObject;
            location.AddObject(obstructionObject);
            obstructionController.transform.position = obstructionObject.position;
        }
        for (i = 0; i < 6; i++)
        {
            visualId = 2;
            obstructionController = (Instantiate(obstructionPrefabs[visualId])).GetComponent<ObstructionController>();
            obstructionController.gameNetwork = this;
            obstructionObject = new ObstructionObject();
            obstructionObject.position = new Vector3(((float)(i - 2)) * 0.66f + UnityEngine.Random.Range(0.05f, 0.6f), 0.0f, UnityEngine.Random.Range(1.2f, 2.5f));
            switch (visualId)
            {
                case 2:
                    obstructionObject.scale = 0.2f;
                    obstructionObject.durability = 10.0f;
                    break;
            }
            obstructionObject.visualId = visualId;
            obstructionController.obj = obstructionObject;
            location.AddObject(obstructionObject);
            obstructionController.transform.position = obstructionObject.position;
        }
    }

    public LocationObject GetLocationObject(int id)
    {
        return location.GetObject(id);
    }

}
