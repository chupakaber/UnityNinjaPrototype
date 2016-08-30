using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.Networking.Match;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameNetwork : NetworkBehaviour {


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
    public bool isLocal = false;
    public bool ready = false;
    public bool updating = false;
    public float updateTimeout = 0.2f;
    public float updateCooldown = 0.2f;
    public float lastTouchX = 0.0f;
    public float lastTouchY = 0.0f;

    public int swipeType = 1;
    public ThrowState throwState = ThrowState.NONE;

    public new bool isServer {
        get {
            if(isLocal)
            {
                return true;
            }
            return base.isServer;
        }
        set {
        }
    }

    [ClientRpc]
    public void RpcSpawnObject(int id, Location.ObjectType objectType, Vector3 newPosition, float newScale, int visualId)
    {
        if (!isServer)
        {
            //Debug.Log("RpcSpawnObject [CLIENT]: " + id + "; " + newPosition);
            switch (objectType)
            {
                case Location.ObjectType.PLAYER:
                    PlayerController playerController = null;
                    PlayerObject playerObject = null;
                    playerObject = new PlayerObject();
                    playerObject.id = id;
                    playerObject.position = newPosition;
                    playerObject.scale = newScale;
                    playerObject.health = 100.0f;
                    playerObject.direction = 1.0f;
                    if (id == 0)
                    {
                        playerController = (Instantiate(bodyPrefabs[0])).GetComponent<PlayerController>();
                        playerController.gameNetwork = this;
                        playerController.obj = playerObject;
                        playerObject.visualObject = playerController;
                        playerController.transform.position = playerObject.position;
                    }
                    location.AddObject(playerObject);
                    break;
                case Location.ObjectType.OBSTRUCTION:
                    ObstructionController obstructionController = null;
                    ObstructionObject obstructionObject = new ObstructionObject();
                    obstructionObject.id = id;
                    obstructionObject.position = newPosition;
                    obstructionObject.scale = newScale;
                    obstructionObject.visualId = visualId;
                    obstructionController = (Instantiate(obstructionPrefabs[obstructionObject.visualId])).GetComponent<ObstructionController>();
                    obstructionController.obj = obstructionObject;
                    obstructionObject.visualObject = obstructionController;
                    obstructionController.transform.position = obstructionObject.position;
                    location.AddObject(obstructionObject);
                    break;
                case Location.ObjectType.MISSILE:
                    MissileController missileController = null;
                    MissileObject missileObject = new MissileObject();
                    missileObject.id = id;
                    if (Mathf.Abs(newPosition.z) < 0.1f)
                    {
                        missileObject.position = armedMissile.transform.position;
                    }
                    else
                    {
                        missileObject.position = new Vector3(newPosition.x, newPosition.y, -newPosition.z);
                    }
                    missileObject.scale = newScale;
                    missileController = (Instantiate(missilePrefabs[0])).GetComponent<MissileController>();
                    missileController.obj = missileObject;
                    missileObject.visualObject = missileController;
                    missileController.transform.position = missileObject.position;
                    if (Mathf.Abs(newPosition.z) < 0.1f)
                    {
                        missileController.transform.rotation = armedMissile.transform.rotation;
                    }
                    location.AddObject(missileObject);
                    break;
            }
        }
        else
        {
            //Debug.Log("RpcMoveObject [SERVER]: " + id + "; " + newPosition);
        }
    }

    [ClientRpc]
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

    [ClientRpc]
    public void RpcMoveObject(int id, Vector3 newPosition, float newScale)
    {
        if (!isServer)
        {
            //Debug.Log("RpcMoveObject [CLIENT]: " + id + "; " + newPosition);
            //transform.position += Vector3.right * (newPosition.x - transform.position.x);
            LocationObject obj = GetLocationObject(id);
            if (obj != null)
            {
                if (obj.objectType == Location.ObjectType.MISSILE)
                {
                    obj.position = new Vector3(newPosition.x, -newPosition.y, newPosition.z);
                }
                else
                {
                    obj.position = newPosition;
                }
                obj.scale = newScale;
            }
        }
        else
        {
            //Debug.Log("RpcMoveObject [SERVER]: " + id + "; " + newPosition);
        }
    }

    [ClientRpc]
    public void RpcUpdatePlayer(int id, float health, float stamina)
    {
        if (!isServer)
        {
            PlayerObject playerObject;
            LocationObject obj = GetLocationObject(id);
            if (obj != null)
            {
                if (obj.objectType == Location.ObjectType.PLAYER)
                {
                    playerObject = (PlayerObject)obj;
                    playerObject.health = health;
                    playerObject.stamina = stamina;
                }
            }
        }
    }

    [ClientRpc]
    public void RpcRearmMissile()
    {
        if (!isServer)
        {
            armedMissile.Rearm();
        }
    }

    [ClientRpc]
    public void RpcFlashPlayer(int id)
    {
        if (!isServer)
        {
            FlashPlayer(id);
        }
    }

    [ClientRpc]
    public void RpcGameOver(int winner)
    {
        if (!isServer)
        {
            GameOver(winner);
        }
    }

    [ClientRpc]
    public void RpcSetAbility(bool active, int id)
    {
        if(!isServer)
        {
            SetAbility(active, id);
        }
    }

    public void SetAbility(bool active, int id)
    {
        if (active)
        {
            switch (id)
            {
                case 0:
                    abilityActiveButton.text.text = "О";
                    break;
                case 1:
                    abilityActiveButton.text.text = "Щ";
                    break;
            }
        }
        else
        {
            switch (id)
            {
                case 0:
                    abilityPassiveButton.text.text = "У";
                    break;
                case 1:
                    abilityPassiveButton.text.text = "К";
                    break;
            }
        }
    }

    [ClientRpc]
    public void RpcShowNotice(int target, string message, float offset, int color, bool floating)
    {
        if (!isServer)
        {
            ShowNotice(target, message, offset, color, floating);
        }
    }

    public void ShowNotice(int target, string message, float offset, int color, bool floating)
    {
        if (floating)
        {
            float distanceScale = 1.0f;
            FloatingNotifyController floatingNotify = GameObject.Instantiate(floatNotifyPrefab).GetComponent<FloatingNotifyController>();
            switch (color)
            {
                case 0:
                    floatingNotify.ShowGreen(message);
                    break;
                case 1:
                    floatingNotify.ShowRed(message);
                    break;
            }
            if ((target == 0 && !isServer) || (target == 1 && isServer))
            {
                if (location != null)
                {
                    PlayerObject playerObject = (PlayerObject)location.GetObject(target);
                    if (playerObject != null && playerObject.visualObject != null)
                    {
                        floatingNotify.transform.localScale = Vector3.one * Mathf.Pow(playerObject.visualObject.transform.position.z, 0.5f);
                        floatingNotify.transform.position = playerObject.visualObject.transform.position + Vector3.right * 0.2f + Vector3.forward * -1.0f + Vector3.up * -0.01f * offset * (1.0f + playerObject.visualObject.transform.position.z * 2.0f);
                    }
                }
            }
            else if ((target == 0 && isServer) || (target == 1 && !isServer))
            {
                floatingNotify.transform.position = camera.transform.position + Vector3.right * -0.2f + Vector3.forward * 1.0f + Vector3.up * (-0.95f + 0.05f * offset);
            }
        }
        else
        {
            FixedNotifyController fixedNotify = GameObject.Instantiate(fixedNotifyPrefab).GetComponent<FixedNotifyController>();
            fixedNotify.text.rectTransform.SetParent(gameMatchMaker.canvasPlay.transform);
            if ((isServer && target == 0) || (!isServer && target == 1))
            {
                fixedNotify.Show(0, message, color, offset);
            }
            else
            {
                fixedNotify.Show(1, message, color, offset);
            }
        }
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

    [ClientRpc]
    public void RpcFlashPassiveAbility(int id)
    {
        if (!isServer)
        {
            FlashPassiveAbility(id);
        }
    }

    public void FlashPassiveAbility(int id)
    {
        PlayerObject playerObject = null;
        playerObject = (PlayerObject)location.GetObject(id);
        if (playerObject != null)
        {
            abilityPassiveButton.Activate(5.0f);
        }
    }

    [ClientRpc]
    public void RpcFlashObstruction(int id)
    {
        if(!isServer)
        {
            FlashObstruction(id);
        }
    }

    public void FlashObstruction(int id)
    {
        ObstructionObject obstructionObject = (ObstructionObject)location.GetObject(id);
        if(obstructionObject != null && obstructionObject.visualObject != null)
        {
            obstructionObject.visualObject.Flash();
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
        switchSwipeTypeButton = GameObject.Find("SwitchSwipeType").GetComponent<Button>();
        switchSwipeTypeButton.onClick.AddListener(delegate() {
            swipeController.swipeType++;
            if(swipeController.swipeType > 2)
            {
                swipeController.swipeType = 1;
            }
            switchSwipeTypeButton.GetComponentInChildren<Text>().text = "РЕЖИМ\nСВАЙПА\n#" + swipeController.swipeType;
        });
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

    [Client]
    public void ClientInit()
    {
        SendSimpleMessage(ClientEvent.CONNECTED);
        abilityActiveButton.button.onClick.AddListener(delegate() {
            if (abilityActiveButton.Activate(10.0f))
            {
                SendSimpleMessage(ClientEvent.USE_ABILITY);
            }
        });
    }

    public void ServerInit()
    {
        NetworkServer.RegisterHandler(100, ReceiveClientSimpleMessage);
        NetworkServer.RegisterHandler(101, ReceiveClientIntMessage);
        NetworkServer.RegisterHandler(102, ReceiveClientFloatMessage);
        NetworkServer.RegisterHandler(103, ReceiveClientDoubleFloatMessage);
        NetworkServer.RegisterHandler(104, ReceiveClientFourFloatMessage);
        abilityActiveButton.button.onClick.AddListener(delegate () {
            if (abilityActiveButton.Activate(10.0f))
            {
                UseAbility(0);
            }
        });
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
                armedMissile.SetAnchor(new Vector2(lastTouchX, 1.0f - lastTouchY));
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
        swipeController.AddPoint(new Vector2(lastTouchX, lastTouchY), Time.deltaTime, throwState == ThrowState.TOUCHED);
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
        swipeController.AddPoint(new Vector2(lastTouchX, lastTouchY), Time.deltaTime, throwState == ThrowState.TOUCHED);
#endif
    }

    public void OnThrow(object sender, SwipeEventArgs e)
    {
        if (isServer)
        {
            Throw(0, e.angle, e.torsion, e.speed);
        }
        else
        {
            SendFourFloatMessage(ClientEvent.THROW, e.angle.x, e.angle.y, e.torsion, e.speed);
        }
        throwState = ThrowState.NONE;
    }

    public void Throw(int player, Vector2 angle, float torsion, float speed)
    {
        MissileController missileController;
        MissileObject missileObject;
        PlayerObject playerObject;
        LocationObject playerLocationObject = GetLocationObject(player);
        float staminaConsumption = 0.0f;
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
                missileController = (Instantiate(missilePrefabs[0])).GetComponent<MissileController>();
                missileController.gameNetwork = this;
                missileObject = new MissileObject();
                missileObject.position = new Vector3(playerLocationObject.position.x, 0.2f, 0.1f);
                missileObject.direction = (new Vector3(0.0f, Mathf.Min(1.0f, 0.2f + Mathf.Min(0.5f, angle.y)) * 0.1f, Mathf.Min(0.5f, Math.Max(0.2f, speed / 5.0f)))).normalized;
                missileObject.direction = Quaternion.Euler(0, Mathf.Min(30.0f, Mathf.Max(-30.0f, angle.x)), 0) * missileObject.direction;
                //missileObject.passiveVelocity = new Vector3(playerObject.MoveSpeed(), 0.0f, 0.0f);
                missileObject.torsion = new Vector3(0.0f, torsion, 0.0f);
                if (isServer)
                {
                    if (player == 0)
                    {
                        armedMissile.Rearm();
                    }
                    else
                    {
                        if (!isLocal)
                        {
                            RpcRearmMissile();
                        }
                    }
                }
                missileObject.velocity = Mathf.Min(1.2f, Mathf.Max(0.8f, speed)) * 8.0f;
                missileController.obj = missileObject;
                location.AddObject(missileObject);
                missileController.transform.position = missileObject.position;
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
            SetAbility(false, 0);
            SetAbility(true, 0);
        }
        else
        {
            playerObject.abilityCrit = 0.0f;
            playerObject.abilityShield = 0.0f;
            SetAbility(false, 1);
            SetAbility(true, 1);
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
                RpcSetAbility(false, 0);
                RpcSetAbility(true, 0);
            }
        }
        else
        {
            playerObject.abilityCrit = 0.0f;
            playerObject.abilityShield = 0.0f;
            if (!isLocal)
            {
                RpcSetAbility(false, 1);
                RpcSetAbility(true, 1);
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
