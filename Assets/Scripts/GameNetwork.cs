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
    public GameMatchMaker gameMatchMaker;
    public ScreenEffectsController screenEffects;
    public ArmedMissileController armedMissile;
    public AbilityButtonController abilityActiveButton;
    public AbilityButtonController abilityPassiveButton;
    public Image staminaBar;
    public Text healthBarSelf;
    public Text healthBarEnemy;
    public Camera camera;
    public Location location;
    public bool ready = false;
    public bool updating = false;
    public float updateTimeout = 0.2f;
    public float updateCooldown = 0.2f;
    public float lastTouchX = 0.0f;
    public float lastTouchY = 0.0f;

    public ThrowState throwState = ThrowState.NONE;

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
                    missileObject.position = new Vector3(newPosition.x, -newPosition.y, newPosition.z);
                    missileObject.scale = 1.0f - (missileObject.position.y + 1.0f) * 0.3f;
                    missileController = (Instantiate(missilePrefabs[0])).GetComponent<MissileController>();
                    missileController.obj = missileObject;
                    missileObject.visualObject = missileController;
                    missileController.transform.position = new Vector3(missileObject.position.x, missileObject.position.y * 0.8f, missileObject.position.y);
                    missileController.transform.localScale = new Vector3(missileObject.scale, missileObject.scale, 1.0f);
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
                        floatingNotify.transform.position = playerObject.visualObject.transform.position + Vector3.right * 0.2f + Vector3.forward * -1.0f + Vector3.up * -0.05f * offset;
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
                RpcShowNotice(playerObject.id, "+ ЩИТ", 5.0f, 0, false);
            }
            else if(playerObject.abilityStun == 0.0f)
            {
                playerObject.abilityStun = 10.0f;
                playerObject.stunMove = 5.0f;
                ShowNotice(playerObject.id, "+ ОГЛУШЕНИЕ", 3.0f, 0, false);
                RpcShowNotice(playerObject.id, "+ ОГЛУШЕНИЕ", 3.0f, 0, false);
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
            NetManager.singleton.matchMaker.DestroyMatch(NetManager.singleton.matchInfo.networkId, 0, null);
            //NetManager.singleton.StopHost();
            //NetManager.singleton.StopServer();
        }
        else
        {
            NetManager.singleton.StopClient();
        }
        LocationObject.lastObjectId = 0;
        gameMatchMaker.canvasPlay.enabled = false;
        gameMatchMaker.canvasConnect.enabled = true;
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
        if (!isServer)
        {
            ClientInit();
        }
        else
        {
            ServerInit();
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
        //Debug.Log("ClientInit");
    }

    [Server]
    public void ServerInit()
    {
        NetworkServer.RegisterHandler(100, ReceiveClientSimpleMessage);
        NetworkServer.RegisterHandler(101, ReceiveClientIntMessage);
        NetworkServer.RegisterHandler(102, ReceiveClientFloatMessage);
        NetworkServer.RegisterHandler(103, ReceiveClientDoubleFloatMessage);
        abilityActiveButton.button.onClick.AddListener(delegate () {
            if (abilityActiveButton.Activate(10.0f))
            {
                UseAbility(0);
            }
        });
        //Debug.Log("ServerInit");
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
            case ClientEvent.THROW:
                Throw(1, msg.value1, msg.value2);
                break;
        }
        //Debug.Log("Received[" + msg.clientEvent + "]: " + msg.value1 + " ; " + msg.value2);
    }

    public void SendSimpleMessage(ClientEvent clientEvent)
    {
        SimpleEventMessage msg = new SimpleEventMessage();
        msg.clientEvent = clientEvent;
        NetworkManager.singleton.client.Send(100, msg);
    }

    public void SendIntMessage(ClientEvent clientEvent, int value)
    {
        IntEventMessage msg = new IntEventMessage();
        msg.clientEvent = clientEvent;
        msg.value = value;
        NetworkManager.singleton.client.Send(101, msg);
    }

    public void SendFloatMessage(ClientEvent clientEvent, float value)
    {
        FloatEventMessage msg = new FloatEventMessage();
        msg.clientEvent = clientEvent;
        msg.value = value;
        NetworkManager.singleton.client.Send(102, msg);
    }

    public void SendDoubleFloatMessage(ClientEvent clientEvent, float value1, float value2)
    {
        DoubleFloatEventMessage msg = new DoubleFloatEventMessage();
        msg.clientEvent = clientEvent;
        msg.value1 = value1;
        msg.value2 = value2;
        NetworkManager.singleton.client.Send(103, msg);
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
    }

    void OnGUI ()
    {
        float angle = 0.0f;
        float power = 0.0f;
        float mouseX = Input.mousePosition.x / (float)Screen.width;
        float mouseY = 1.0f - Input.mousePosition.y / (float)Screen.height;
        float touchX = 0.0f;
        float touchY = 0.0f;
#if !UNITY_STANDALONE
        Touch touch;
        if (Input.touchCount > 0)
        {
            touch = Input.GetTouch(0);
            touchX = touch.position.x / (float)Screen.width;
            touchY = 1.0f - touch.position.y / (float)Screen.height;
            if (touchX > 0.25f && touchX < 0.75f && touchY > 0.8f && touchY < 1.0f)
            {
                throwState = ThrowState.TOUCHED;
            }
            lastTouchX = touchX;
            lastTouchY = touchY;
        }
        else
        {
            if (throwState == ThrowState.TOUCHED)
            {
                throwState = ThrowState.NONE;
                if (lastTouchY < 0.75f)
                {
                    angle = (lastTouchX - 0.5f) * 2.0f;
                    power = Math.Min(1.0f, lastTouchY * 2.0f);
                    if (isServer)
                    {
                        Throw(0, angle, power);
                    }
                    else
                    {
                        SendDoubleFloatMessage(ClientEvent.THROW, angle, power);
                    }
                }
            }
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            if (throwState != ThrowState.TOUCHED)
            {
                if (mouseX > 0.25f && mouseX < 0.75f && mouseY > 0.8f && mouseY < 1.0f)
                {
                    throwState = ThrowState.TOUCHED;
                }
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (throwState == ThrowState.TOUCHED)
            {
                throwState = ThrowState.NONE;
                if (mouseY < 0.75f)
                {
                    angle = (mouseX - 0.5f) * 2.0f;
                    power = Math.Min(1.0f, mouseY * 2.0f);
                    if (isServer)
                    {
                        Throw(0, angle, power);
                    }
                    else
                    {
                        SendDoubleFloatMessage(ClientEvent.THROW, angle, power);
                    }
                }
            }
        }
#endif
    }

    public void Throw(int player, float angle, float power)
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
                if (player == 0)
                {
                    missileObject.position = new Vector3(playerLocationObject.position.x, -1.0f, 0.1f);
                    missileObject.direction = new Vector3(angle * 0.2f, 0.5f, 0.5f);
                    if (isServer)
                    {
                        armedMissile.Rearm();
                    }
                }
                else
                {
                    missileObject.position = new Vector3(playerLocationObject.position.x, 1.0f, 0.1f);
                    missileObject.direction = new Vector3(angle * 0.2f, -0.5f, 0.5f);
                    if (isServer)
                    {
                        RpcRearmMissile();
                    }
                }
                missileObject.velocity = Mathf.Max(0.7f, power) * 0.1f;
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
        playerObject.position = new Vector3(0.0f, 0.25f, 0.75f);
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
        playerObject.position = new Vector3(0.0f, 0.25f, 0.75f);
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
            RpcSetAbility(false, 0);
            RpcSetAbility(true, 0);
        }
        else
        {
            playerObject.abilityCrit = 0.0f;
            playerObject.abilityShield = 0.0f;
            RpcSetAbility(false, 1);
            RpcSetAbility(true, 1);
        }
        playerController.obj = playerObject;
        location.AddObject(playerObject);
        playerController.transform.position = playerObject.position;

        for (i = 0; i < 6; i++)
        {
            visualId = Math.Min(UnityEngine.Random.Range(0, obstructionPrefabs.Length), obstructionPrefabs.Length - 1);
            obstructionController = (Instantiate(obstructionPrefabs[visualId])).GetComponent<ObstructionController>();
            obstructionController.gameNetwork = this;
            obstructionObject = new ObstructionObject();
            obstructionObject.position = new Vector3(((float)(i - 2)) * 0.66f + UnityEngine.Random.Range(0.05f, 0.6f), UnityEngine.Random.Range(-0.22f, 0.0f), 0.5f);
            switch(visualId)
            {
                case 0:
                    obstructionObject.scale = 0.12f;
                    obstructionObject.durability = 30.0f;
                    break;
                case 1:
                    obstructionObject.scale = 0.04f;
                    obstructionObject.durability = 50.0f;
                    break;
                case 2:
                    obstructionObject.scale = 0.1f;
                    obstructionObject.durability = 10.0f;
                    break;
                case 3:
                    obstructionObject.scale = 0.09f;
                    obstructionObject.durability = 20.0f;
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

class SimpleEventMessage : MessageBase
{
    public GameNetwork.ClientEvent clientEvent;
}

class IntEventMessage : MessageBase
{
    public GameNetwork.ClientEvent clientEvent;
    public int value = -1;
}

class FloatEventMessage : MessageBase
{
    public GameNetwork.ClientEvent clientEvent;
    public float value = 0.0f;
}

class DoubleFloatEventMessage : MessageBase
{
    public GameNetwork.ClientEvent clientEvent;
    public float value1 = 0.0f;
    public float value2 = 0.0f;
}

class LocationObjectEventMessage : MessageBase
{
    public GameNetwork.ClientEvent clientEvent;
    public int id = -1;
    public Location.ObjectType objectType = Location.ObjectType.NONE;
    public Vector3 position = Vector3.zero;
    public float scale = 0.0f;
}

public class Location
{

    public enum ObjectType {
        NONE = 0,
        OBSTRUCTION = 1,
        PLAYER = 2,
        MISSILE = 3
    };

    private GameNetwork network;
    private LinkedList<LocationObject> objects = new LinkedList<LocationObject>();

    public void SetNetworkBehavior(GameNetwork pointer)
    {
        network = pointer;
    }

    public void AddObject(ObstructionObject obj)
    {
        objects.AddLast(obj);
        if (network.isServer)
        {
            network.RpcSpawnObject(obj.id, obj.objectType, obj.position, obj.scale, obj.visualId);
        }
    }

    public void AddObject(PlayerObject obj)
    {
        objects.AddLast(obj);
        if (network.isServer)
        {
            network.RpcSpawnObject(obj.id, obj.objectType, obj.position, obj.scale, obj.visualId);
        }
        else
        {
        }
    }

    public void AddObject(MissileObject obj)
    {
        objects.AddLast(obj);
        if (network.isServer)
        {
            network.RpcSpawnObject(obj.id, obj.objectType, obj.position, obj.scale, obj.visualId);
        }
    }

    public void RemoveObject(LocationObject obj)
    {
        if (network.isServer)
        {
            network.RpcDestroyObject(obj.id);
        }
        objects.Remove(obj);
    }

    public void UpdateCycle(float deltaTime)
    {
        LinkedListNode<LocationObject> objNode;
        LinkedListNode<LocationObject> objNodeNext;
        LinkedListNode<LocationObject> objNode2;
        LinkedListNode<LocationObject> objNodeNext2;
        ObstructionObject obstructionObject;
        PlayerObject playerObject;
        MissileObject missileObject;
        Vector3 v3Delta;
        float scale = 0.0f;
        objNode = objects.First;
        while (objNode != null)
        {
            objNodeNext = objNode.Next;
            if (objNode.Value != null)
            {
                ////Debug.Log("UpdateObject: " + objNode.Value.id);
                switch (objNode.Value.objectType)
                {
                    case ObjectType.PLAYER:
                        playerObject = (PlayerObject)objNode.Value;
                        if (playerObject.visualObject != null)
                        {
                            if (Mathf.Abs(network.camera.transform.position.x - playerObject.position.x) > 2.0f)
                            {
                                if (network.camera.transform.position.x - playerObject.position.x > 0.0f)
                                {
                                    playerObject.position += Vector3.right * 4.0f;
                                }
                                else
                                {
                                    playerObject.position += Vector3.right * -4.0f;
                                }
                            }
                            v3Delta = Vector3.right * (playerObject.position.x * 1.0f - playerObject.visualObject.transform.position.x);
                            if (Mathf.Abs(v3Delta.x) > 1.0f)
                            {
                                if (v3Delta.x > 0.0f)
                                {
                                    playerObject.visualObject.transform.position += Vector3.right * 4.0f;
                                }
                                else
                                {
                                    playerObject.visualObject.transform.position += Vector3.right * -4.0f;
                                }
                                v3Delta = Vector3.right * (playerObject.position.x * 1.0f - playerObject.visualObject.transform.position.x);
                            }
                            playerObject.visualObject.transform.position += v3Delta * Mathf.Min(1.0f, deltaTime * 5.0f);
                        }
                        else
                        {
                            v3Delta = Vector3.right * (playerObject.position.x * 1.0f - network.camera.transform.position.x);
                            if(Mathf.Abs(v3Delta.x) > 1.0f)
                            {
                                if (v3Delta.x > 0.0f)
                                {
                                    network.camera.transform.position += Vector3.right * 4.0f;
                                }
                                else
                                {
                                    network.camera.transform.position += Vector3.right * -4.0f;
                                }
                                v3Delta = Vector3.right * (playerObject.position.x * 1.0f - network.camera.transform.position.x);
                            }
                            network.camera.transform.position += v3Delta * Mathf.Min(1.0f, deltaTime * 5.0f);
                        }
                        if((playerObject.id == 0 && network.isServer) || (playerObject.id == 1 && !network.isServer))
                        {
                            network.healthBarSelf.text = Mathf.Floor(playerObject.health) + "";
                            network.staminaBar.rectTransform.sizeDelta = new Vector2(((float)Screen.width) * playerObject.stamina / 100.0f, network.staminaBar.rectTransform.sizeDelta.y);
                        }
                        else
                        {
                            network.healthBarEnemy.text = Mathf.Floor(playerObject.health) + "";
                        }
                        break;
                    case ObjectType.OBSTRUCTION:
                        obstructionObject = (ObstructionObject)objNode.Value;
                        if (Mathf.Abs(network.camera.transform.position.x - obstructionObject.visualObject.transform.position.x) > 2.0f)
                        {
                            if(network.camera.transform.position.x - obstructionObject.visualObject.transform.position.x > 0.0f)
                            {
                                obstructionObject.visualObject.transform.position += Vector3.right * 4.0f;
                            }
                            else
                            {
                                obstructionObject.visualObject.transform.position += Vector3.right * -4.0f;
                            }
                        }
                        break;
                    case ObjectType.MISSILE:
                        missileObject = (MissileObject)objNode.Value;
                        if (missileObject.visualObject != null)
                        {
                            v3Delta = new Vector3(missileObject.position.x, missileObject.position.y * 0.8f, missileObject.position.y) - missileObject.visualObject.transform.position;
                            missileObject.visualObject.transform.position += v3Delta * Mathf.Min(1.0f, deltaTime * 5.0f);
                            scale = 1.0f - (missileObject.position.y + 1.0f) * 0.3f;
                            missileObject.visualObject.transform.localScale = new Vector3(scale, scale, 1.0f);
                        }
                        break;
                }
            }
            objNode = objNodeNext;
        }
        network.armedMissile.transform.position += Vector3.right * (network.camera.transform.position.x - network.armedMissile.transform.position.x);
    }

    public void PhysicCycle(float deltaTime)
    {
        network.updateTimeout -= deltaTime;
        if (network.updateTimeout <= 0.0f)
        {
            network.updating = true;
            network.updateTimeout = network.updateCooldown;
        }
        LinkedListNode<LocationObject> objNode;
        LinkedListNode<LocationObject> objNodeNext;
        LinkedListNode<LocationObject> objNode2;
        LinkedListNode<LocationObject> objNodeNext2;
        ObstructionObject obstructionObject;
        PlayerObject playerObject;
        PlayerObject attackerObject;
        MissileObject missileObject;
        string notifyMessage = "";
        float moveSpeed = 0.0f;
        float damage = 0.0f;
        float critChance = 0.0f;
        float noticeOffset = 0.0f;
        bool hit = false;
        objNode = objects.First;
        while (objNode != null)
        {
            objNodeNext = objNode.Next;
            if (objNode.Value != null)
            {
                switch (objNode.Value.objectType)
                {
                    case ObjectType.PLAYER:
                        playerObject = (PlayerObject)objNode.Value;
                        moveSpeed = playerObject.direction * playerObject.strafeSpeed;
                        if(playerObject.stun > 0.0f)
                        {
                            moveSpeed = 0.0f;
                        }
                        else if(playerObject.legInjury > 0.0f)
                        {
                            moveSpeed /= 1.0f + playerObject.legInjuryEffect;
                        }
                        playerObject.position.x += moveSpeed * deltaTime;
                        if(playerObject.position.x < -2.0f)
                        {
                            playerObject.position += Vector3.right * 4.0f;
                        }
                        if (playerObject.position.x > 2.0f)
                        {
                            playerObject.position += Vector3.right * -4.0f;
                        }
                        playerObject.strafeTimeout -= deltaTime;
                        if (playerObject.strafeTimeout <= 0.0f)
                        {
                            float playerX = playerObject.position.x;
                            float enemyX = 0.0f;
                            float strafeMin = 0.0f;
                            float strafeMax = 0.0f;
                            float deltaX;
                            if (playerObject.id == 0)
                            {
                                enemyX = objects.First.Next.Value.position.x;
                            }
                            else
                            {
                                enemyX = objects.First.Value.position.x;
                            }
                            deltaX = playerX - enemyX;
                            if(deltaX > 2.0f)
                            {
                                deltaX += -4.0f;
                            }
                            if (deltaX < -2.0f)
                            {
                                deltaX += 4.0f;
                            }
                            if ((deltaX > 0.0f && playerObject.direction > 0.0f) || (deltaX < 0.0f && playerObject.direction < 0.0f))
                            {
                                strafeMin = (playerObject.strafeMinTimeout + playerObject.strafeMaxTimeout) * 0.5f;
                                strafeMax = playerObject.strafeMaxTimeout;
                            }
                            else
                            {
                                strafeMin = playerObject.strafeMinTimeout;
                                strafeMax = (playerObject.strafeMinTimeout + playerObject.strafeMaxTimeout) * 0.5f;
                            }
                            playerObject.strafeTimeout += UnityEngine.Random.Range(strafeMin, strafeMax);
                            playerObject.direction *= -1.0f;
                        }
                        if(playerObject.stamina < playerObject.maxStamina)
                        {
                            playerObject.stamina += playerObject.staminaRegeneration * Time.deltaTime;
                            if(playerObject.stamina > playerObject.maxStamina)
                            {
                                playerObject.stamina = playerObject.maxStamina;
                            }
                        }
                        if (playerObject.legInjury > 0.0f)
                        {
                            playerObject.legInjury -= deltaTime;
                            if(playerObject.legInjury < 0.0f)
                            {
                                playerObject.legInjury = 0.0f;
                            }
                        }
                        if (playerObject.armInjury > 0.0f)
                        {
                            playerObject.armInjury -= deltaTime;
                            if (playerObject.armInjury < 0.0f)
                            {
                                playerObject.armInjury = 0.0f;
                            }
                        }
                        if (playerObject.stun > 0.0f)
                        {
                            playerObject.stun -= deltaTime;
                            if (playerObject.stun < 0.0f)
                            {
                                playerObject.stun = 0.0f;
                            }
                        }
                        if (playerObject.abilityCrit > 0.0f)
                        {
                            playerObject.abilityCrit -= deltaTime;
                            if (playerObject.abilityCrit < 0.0f)
                            {
                                playerObject.abilityCrit = 0.0f;
                            }
                        }
                        if (playerObject.abilityStun > 0.0f)
                        {
                            playerObject.abilityStun -= deltaTime;
                            if (playerObject.abilityStun < 0.0f)
                            {
                                playerObject.abilityStun = 0.0f;
                            }
                        }
                        if (playerObject.abilityEvade > 0.0f)
                        {
                            playerObject.abilityEvade -= deltaTime;
                            if (playerObject.abilityEvade < 0.0f)
                            {
                                playerObject.abilityEvade = 0.0f;
                            }
                        }
                        if (playerObject.abilityShield > 0.0f)
                        {
                            playerObject.abilityShield -= deltaTime;
                            if (playerObject.abilityShield < 0.0f)
                            {
                                playerObject.abilityShield = 0.0f;
                            }
                        }
                        if (network.updating)
                        {
                            network.RpcMoveObject(objNode.Value.id, objNode.Value.position, objNode.Value.scale);
                            network.RpcUpdatePlayer(playerObject.id, playerObject.health, playerObject.stamina);
                        }
                        break;
                    case ObjectType.OBSTRUCTION:
                        //obstructionObject = (ObstructionObject) objNode.Value;
                        break;
                    case ObjectType.MISSILE:
                        missileObject = (MissileObject)objNode.Value;
                        missileObject.direction.z += -0.98f * 1.5f * deltaTime;
                        missileObject.direction.Normalize();
                        missileObject.position.x += missileObject.direction.x * missileObject.velocity;
                        missileObject.position.y += missileObject.direction.y * missileObject.velocity;
                        missileObject.position.z += missileObject.direction.z * missileObject.velocity;
                        if (missileObject.position.z <= 0.0f)
                        {
                            Debug.Log("missileObject DESTROY by position.z");
                            if (missileObject.visualObject != null)
                            {
                                GameObject.Destroy(missileObject.visualObject.gameObject);
                            }
                            RemoveObject(objNode.Value);
                        }
                        else if(missileObject.position.y > 0.7f && (missileObject.position - missileObject.direction * missileObject.velocity).y <= 0.7f)
                        {
                            objNode2 = objects.First;
                            while (objNode2 != null)
                            {
                                objNodeNext2 = objNode2.Next;
                                if (objNode2.Value != null)
                                {
                                    switch (objNode2.Value.objectType)
                                    {
                                        case ObjectType.OBSTRUCTION:
                                            obstructionObject = (ObstructionObject)objNode2.Value;
                                            if(Mathf.Abs(missileObject.position.x - obstructionObject.position.x) < obstructionObject.scale)
                                            {
                                                if(missileObject.direction.y > 0.0f)
                                                {
                                                    attackerObject = (PlayerObject)objects.First.Value;
                                                }
                                                else
                                                {
                                                    attackerObject = (PlayerObject)objects.First.Next.Value;
                                                }
                                                obstructionObject.durability -= UnityEngine.Random.Range(attackerObject.minDamage, attackerObject.maxDamage);
                                                if(obstructionObject.durability <= 0.0f)
                                                {
                                                    Debug.Log("obstructionObject DESTROY");
                                                    if (obstructionObject.visualObject != null)
                                                    {
                                                        GameObject.Destroy(obstructionObject.visualObject.gameObject);
                                                    }
                                                    RemoveObject(objNode2.Value);
                                                    Debug.Log("missileObject DESTROY by obstruction");
                                                    if (missileObject.visualObject != null)
                                                    {
                                                        GameObject.Destroy(missileObject.visualObject.gameObject);
                                                    }
                                                    RemoveObject(objNode.Value);
                                                    objNodeNext2 = null;
                                                    float hpBonus = 0.0f;
                                                    if(UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f)
                                                    {
                                                        hpBonus = 15.0f;
                                                    }
                                                    else
                                                    {
                                                        if (UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f)
                                                        {
                                                            hpBonus = 25.0f;
                                                        }
                                                        else
                                                        {
                                                            if (UnityEngine.Random.Range(0.0f, 1.0f) < 0.66f)
                                                            {
                                                                hpBonus = -15.0f;
                                                            }
                                                        }
                                                    }
                                                    if(hpBonus != 0.0f)
                                                    {
                                                        if(missileObject.direction.y > 0.0f)
                                                        {
                                                            playerObject = (PlayerObject)objects.First.Value;
                                                        }
                                                        else
                                                        {
                                                            playerObject = (PlayerObject)objects.First.Next.Value;
                                                        }
                                                        if(playerObject != null)
                                                        {
                                                            playerObject.health += hpBonus;
                                                            if(hpBonus > 0.0f)
                                                            {
                                                                network.RpcShowNotice(playerObject.id, "+" + hpBonus, noticeOffset, 0, true);
                                                                network.ShowNotice(playerObject.id, "+" + hpBonus, noticeOffset, 0, true);
                                                            }
                                                            else
                                                            {
                                                                network.RpcShowNotice(playerObject.id, "" + hpBonus, noticeOffset, 1, true);
                                                                network.ShowNotice(playerObject.id, "" + hpBonus, noticeOffset, 1, true);
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    network.FlashObstruction(obstructionObject.id);
                                                    network.RpcFlashObstruction(obstructionObject.id);
                                                }
                                            }
                                            break;
                                    }
                                }
                                objNode2 = objNodeNext2;
                            }
                        }
                        else if ((missileObject.direction.y > 0.0f && missileObject.position.y >= 2.0f) || (missileObject.direction.y < 0.0f && missileObject.position.y <= -2.0f))
                        {
                            objNode2 = objects.First;
                            while (objNode2 != null)
                            {
                                objNodeNext2 = objNode2.Next;
                                if (objNode2.Value != null)
                                {
                                    switch (objNode2.Value.objectType)
                                    {
                                        case ObjectType.PLAYER:
                                            playerObject = (PlayerObject)objNode2.Value;
                                            attackerObject = null;
                                            hit = false;
                                            if(missileObject.direction.y > 0.0f && playerObject.id == 1 && Mathf.Abs(missileObject.position.x - playerObject.position.x) < 0.25f)
                                            {
                                                hit = true;
                                                attackerObject = (PlayerObject)objects.First.Value;
                                            }
                                            else if(missileObject.direction.y < 0.0f && playerObject.id == 0 && Mathf.Abs(missileObject.position.x - playerObject.position.x) < 0.25f)
                                            {
                                                hit = true;
                                                attackerObject = (PlayerObject)objects.First.Next.Value;
                                            }
                                            if (hit && playerObject.abilityEvade == 0.0f && UnityEngine.Random.Range(0.0f, 1.0f) < playerObject.abilityEvadeChance)
                                            {
                                                hit = false;
                                                playerObject.abilityEvade = 5.0f;
                                                network.RpcShowNotice(playerObject.id, "+ УКЛОНЕНИЕ", noticeOffset, 0, true);
                                                network.ShowNotice(playerObject.id, "+ УКЛОНЕНИЕ", noticeOffset, 0, true);
                                                noticeOffset += 1.0f;
                                                if(playerObject.id == 0)
                                                {
                                                    network.FlashPassiveAbility(playerObject.id);
                                                }
                                                else
                                                {
                                                    network.RpcFlashPassiveAbility(playerObject.id);
                                                }
                                                // Ability Evade
                                            }
                                            if (hit)
                                            {
                                                notifyMessage = "";
                                                network.FlashPlayer(playerObject.id);
                                                network.RpcFlashPlayer(playerObject.id);
                                                damage = UnityEngine.Random.Range(attackerObject.minDamage, attackerObject.maxDamage);
                                                critChance = attackerObject.critChance;
                                                if (attackerObject.abilityCrit == 0.0f)
                                                {
                                                    attackerObject.abilityCrit = 5.0f;
                                                    critChance += attackerObject.abilityCritChance;
                                                    if (attackerObject.id == 0)
                                                    {
                                                        network.FlashPassiveAbility(attackerObject.id);
                                                    }
                                                    else
                                                    {
                                                        network.RpcFlashPassiveAbility(attackerObject.id);
                                                    }
                                                    // Ability Crit
                                                }
                                                if (UnityEngine.Random.Range(0.0f, 1.0f) < critChance)
                                                {
                                                    notifyMessage += "K";
                                                    damage *= attackerObject.critMultiplier;
                                                }
                                                if(playerObject.abilityShield >= 5.0f)
                                                {
                                                    notifyMessage += "Щ";
                                                    damage *= playerObject.abilityShieldMultiplier;
                                                    network.RpcShowNotice(playerObject.id, "+ ЩИТ", noticeOffset, 0, true);
                                                    network.ShowNotice(playerObject.id, "+ ЩИТ", noticeOffset, 0, true);
                                                    noticeOffset += 1.0f;
                                                }
                                                notifyMessage += " -" + Mathf.Floor(damage);
                                                if (playerObject.id == 0)
                                                {
                                                    network.RpcShowNotice(playerObject.id, notifyMessage, noticeOffset, 1, true);
                                                    network.ShowNotice(playerObject.id, notifyMessage, 1.0f, 1, false);
                                                }
                                                else
                                                {
                                                    network.RpcShowNotice(playerObject.id, notifyMessage, 1.0f, 1, false);
                                                    network.ShowNotice(playerObject.id, notifyMessage, noticeOffset, 1, true);
                                                }
                                                noticeOffset += 1.0f;
                                                playerObject.health -= damage;
                                                if(attackerObject.stunMove > 0.0f)
                                                {
                                                    attackerObject.stunMove = 0.0f;
                                                    playerObject.stun += attackerObject.abilityStunDuration;
                                                    network.RpcShowNotice(playerObject.id, "- ОГЛУШЕН", noticeOffset, 1, true);
                                                    network.ShowNotice(playerObject.id, "- ОГЛУШЕН", noticeOffset, 1, true);
                                                    network.RpcShowNotice(playerObject.id, "- ОГЛУШЕН", 5.0f, 1, false);
                                                    network.ShowNotice(playerObject.id, "- ОГЛУШЕН", 5.0f, 1, false);
                                                    noticeOffset += 1.0f;
                                                }
                                                if (UnityEngine.Random.Range(0.0f, 1.0f) < attackerObject.injuryChance)
                                                {
                                                    if (UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f)
                                                    {
                                                        // Injury Arm
                                                        playerObject.armInjury = 8.0f;
                                                        network.RpcShowNotice(playerObject.id, "- РУКА", noticeOffset, 1, true);
                                                        network.ShowNotice(playerObject.id, "- РУКА", noticeOffset, 1, true);
                                                        network.RpcShowNotice(playerObject.id, "- РУКА", 8.0f, 1, false);
                                                        network.ShowNotice(playerObject.id, "- РУКА", 8.0f, 1, false);
                                                        noticeOffset += 1.0f;
                                                    }
                                                    else
                                                    {
                                                        // Injury Leg
                                                        playerObject.legInjury = 8.0f;
                                                        network.RpcShowNotice(playerObject.id, "- НОГА", noticeOffset, 1, true);
                                                        network.ShowNotice(playerObject.id, "- НОГА", noticeOffset, 1, true);
                                                        network.RpcShowNotice(playerObject.id, "- НОГА", 8.0f, 1, false);
                                                        network.ShowNotice(playerObject.id, "- НОГА", 8.0f, 1, false);
                                                        noticeOffset += 1.0f;
                                                    }
                                                }
                                            }
                                            if (playerObject.health <= 0.0f)
                                            {
                                                if (playerObject.id == 0)
                                                {
                                                    GameOver(1);
                                                    return;
                                                }
                                                else
                                                {
                                                    GameOver(0);
                                                    return;
                                                }
                                            }
                                            break;
                                    }
                                }
                                objNode2 = objNodeNext2;
                            }
                            Debug.Log("missileObject DESTROY by position.y");
                            if (missileObject.visualObject != null)
                            {
                                GameObject.Destroy(missileObject.visualObject.gameObject);
                            }
                            RemoveObject(objNode.Value);
                        }
                        if (network.updating)
                        {
                            network.RpcMoveObject(objNode.Value.id, objNode.Value.position, objNode.Value.scale);
                        }
                        break;
                }
            }
            objNode = objNodeNext;
        }
        if (network.updating)
        {
            network.updating = false;
        }
    }

    public LocationObject GetObject(int id)
    {
        LinkedListNode<LocationObject> objNode;
        objNode = objects.First;
        while(objNode != null)
        {
            if(objNode.Value.id == id)
            {
                return objNode.Value;
            }
            objNode = objNode.Next;
        }
        return null;
    }

    public void GameOver(int winner)
    {
        network.RpcGameOver(winner);
        network.GameOver(winner);
        network = null;
    }

    public void Cleanup()
    {
        LinkedListNode<LocationObject> objNode;
        LinkedListNode<LocationObject> objNodeNext;
        objNode = objects.First;
        while (objNode != null)
        {
            objNodeNext = objNode.Next;
            switch (objNode.Value.objectType)
            {
                case ObjectType.PLAYER:
                    PlayerObject playerObject = (PlayerObject)objNode.Value;
                    if(playerObject.visualObject != null)
                    {
                        GameObject.Destroy(playerObject.visualObject.gameObject);
                    }
                    break;
                case ObjectType.OBSTRUCTION:
                    ObstructionObject obstructionObject = (ObstructionObject)objNode.Value;
                    if (obstructionObject.visualObject != null)
                    {
                        GameObject.Destroy(obstructionObject.visualObject.gameObject);
                    }
                    break;
                case ObjectType.MISSILE:
                    MissileObject missileObject = (MissileObject)objNode.Value;
                    if (missileObject.visualObject != null)
                    {
                        GameObject.Destroy(missileObject.visualObject.gameObject);
                    }
                    break;
            }
            objects.Remove(objNode);
            objNode = objNodeNext;
        }
        network.location = null;
    }

}

public class LocationObject
{

    public static int lastObjectId = 0;

    public int id = -1;
    public Location.ObjectType objectType = Location.ObjectType.NONE;
    public int visualId = -1;
    public Vector3 position = new Vector3(0.0f, 0.0f, 0.0f);
    public float scale = 0.0f;

    public LocationObject()
    {
        id = lastObjectId++;
    }

}

public class ObstructionObject : LocationObject
{

    public ObstructionObject() : base()
    {
        objectType = Location.ObjectType.OBSTRUCTION;
    }

    public float durability = 0.0f;
    public ObstructionController visualObject;

}

public class PlayerObject : LocationObject
{

    public PlayerObject() : base()
    {
        objectType = Location.ObjectType.PLAYER;
    }

    public float strafeMinTimeout = 0.0f;
    public float strafeMaxTimeout = 0.0f;

    public float direction = 0.0f;
    public float stunMove = 0.0f;
    public float stun = 0.0f;
    public float strafeTimeout = 0.0f;
    public float armInjury = 0.0f;
    public float legInjury = 0.0f;
    public float abilityShield = -1.0f;
    public float abilityStun = -1.0f;
    public float abilityCrit = -1.0f;
    public float abilityEvade = -1.0f;

    public float health = 0.0f;
    public float stamina = 0.0f;
    public float maxStamina = 0.0f;
    public float staminaConsumption = 0.0f;
    public float staminaRegeneration = 0.0f;
    public float minDamage = 0.0f;
    public float maxDamage = 0.0f;
    public float critChance = 0.15f;
    public float critMultiplier = 1.5f;
    public float injuryChance = 0.1f;
    public float armInjuryEffect = 0.0f;
    public float legInjuryEffect = 0.0f;
    public float abilityEvadeChance = 0.0f;
    public float abilityCritChance = 0.0f;
    public float abilityStunDuration = 0.0f;
    public float abilityShieldDuration = 0.0f;
    public float abilityShieldMultiplier = 0.0f;
    public float strafeSpeed = 0.0f;

    public PlayerController visualObject;

}

public class MissileObject : LocationObject
{

    public MissileObject() : base()
    {
        objectType = Location.ObjectType.MISSILE;
    }

    public Vector3 direction = new Vector3(0.0f, 0.0f, 0.0f);
    public float velocity = 0.0f;
    public MissileController visualObject;

}
