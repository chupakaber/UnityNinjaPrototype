using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.Networking.Match;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class Structures : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}


public class SwipeEventArgs : EventArgs
{
    public Vector2 angle;
    public float torsion;
    public float speed;
    public bool throwing = false;
}

public class SwipePoint
{
    public Vector2 point;
    public float duration;

    public SwipePoint(Vector2 newPoint, float newDuration)
    {
        point = newPoint;
        duration = newDuration;
    }

}

public class SwipeController
{

    public float minLength = 0.025f;
    public float maxLength = 0.5f;
    public int swipeType = 1;

    public bool active = true;
    public bool started = false;
    public bool locked = false;
    public LinkedList<SwipePoint> pointsList = new LinkedList<SwipePoint>();

    private Vector2 direction = Vector2.zero;

    public SwipeController()
    {
    }

    public void AddPoint(Vector2 newPoint, float newDuration, bool touched)
    {
        if (active)
        {
            int i = 0;
            float length = 0.0f;
            float duration = 0.0f;
            /*
            float beginX = 0.0f;
            float beginY = 0.0f;
            */
            float endX = 0.0f;
            float endY = 0.0f;
            float fullX = 0.0f;
            float fullY = 0.0f;
            int startPointCount = 0;
            float startPointX = 0.0f;
            float startPointY = 0.0f;
            int endPointCount = 0;
            float endPointX = 0.0f;
            float endPointY = 0.0f;
            Vector2 v2Delta = Vector2.zero;
            LinkedListNode<SwipePoint> prevPointNode = null;
            LinkedListNode<SwipePoint> pointNode = null;
            LinkedListNode<SwipePoint> pointNodeNext = null;
            SwipeEventArgs eventArgs;
            if (!started && !touched)
            {
                locked = false;
                return;
            }
            if (started || (touched && newPoint.x > 0.25f && newPoint.x < 0.75f && newPoint.y > 0.75f))
            {
                if (!started)
                {
                    started = true;
                }
                pointsList.AddLast(new SwipePoint(newPoint, newDuration));
            }
            pointNode = pointsList.First;
            while (pointNode != null)
            {
                pointNodeNext = pointNode.Next;
                if (prevPointNode != null)
                {
                    v2Delta = pointNode.Value.point - prevPointNode.Value.point;
                }
                else
                {
                    v2Delta = Vector2.zero;
                }
                v2Delta.y *= -1.0f;
                if (v2Delta.y > 0.0f)
                {
                    length += v2Delta.magnitude;
                    duration += pointNode.Value.duration;
                    /*
                    if (i < pointsList.Count * 3 / 4)
                    {
                        beginX += v2Delta.x;
                        beginY += v2Delta.y;
                    }
                    else
                    {
                        endX += v2Delta.x;
                        endY += v2Delta.y;
                    }
                    */
                    // Конец траектории считаем по последней четверти свайпа
                    if (i >= pointsList.Count * 3 / 4)
                    {
                        endX += v2Delta.x;
                        endY += v2Delta.y;
                    }
                    // Исключаем последнюю точку из общей траектории свайпа
                    if (i < pointsList.Count - 1)
                    {
                        fullX += v2Delta.x;
                        fullY += v2Delta.y;
                        if (swipeType == 1)
                        {
                            if (i <= pointsList.Count / 3)
                            {
                                startPointCount++;
                                startPointX += pointNode.Value.point.x;
                                startPointY += pointNode.Value.point.y;
                            }
                            else if (i >= pointsList.Count * 2 / 3)
                            {
                                endPointCount++;
                                endPointX += pointNode.Value.point.x;
                                endPointY += pointNode.Value.point.y;
                            }
                        }
                    }
                }
                if (v2Delta.y < 0.0f && locked)
                {
                    pointsList.Clear();
                    locked = false;
                    started = false;
                    pointNodeNext = null;
                }
                i++;
                prevPointNode = pointNode;
                pointNode = pointNodeNext;
            }
            if (swipeType == 1)
            {
                if (startPointCount > 0 && endPointCount > 0)
                {
                    fullX = endPointX / (float)endPointCount - startPointX / (float)startPointCount;
                    fullY = (endPointY / (float)endPointCount - startPointY / (float)startPointCount) * -1.0f;
                }
                else
                {
                    fullX = 0.0f;
                    fullY = 0.0f;
                }
            }
            //if (started && !locked && pointsList.Count > 10 && length > minLength && endY > 0.0f && (!touched || length > maxLength || (pointsList.First.Value.duration < duration / pointsList.Count * 0.75f) || (pointsList.Count > 1 && (pointsList.Last.Previous.Value.point.y - newPoint.y < 0.0f || (pointsList.Last.Previous.Value.point - newPoint).magnitude / newDuration < length / duration * 0.1f))))
            if (started && !locked && pointsList.Count > 2 && length > minLength && fullY > 0.0f && (!touched || length > maxLength || (pointsList.First.Value.duration < duration / pointsList.Count * 0.75f) || (pointsList.Count > 1 && (pointsList.Last.Previous.Value.point.y - newPoint.y < 0.0f || (endY < length * 0.1f)))))
            {
                // Угол броска по горизонтали не больше 45 градусов от прямого направления
                if (Mathf.Abs(fullX) > Mathf.Abs(fullY))
                {
                    fullX = fullX / Mathf.Abs(fullX) * Mathf.Abs(fullY);
                }
                v2Delta.x = fullX;
                v2Delta.y = fullY;
                v2Delta.Normalize();
                eventArgs = new SwipeEventArgs();
                /*
                eventArgs.angle = new Vector2(Mathf.Atan(beginX / beginY) * 180.0f / Mathf.PI, length / maxLength);
                eventArgs.torsion = Vector2.Angle(new Vector2(endX, endY), new Vector2(beginX, beginY)) / 30.0f * Mathf.Max(0.0f, Mathf.Min(1.0f, duration / 0.4f));
                if (endX < beginX)
                {
                    eventArgs.torsion *= -1.0f;
                }
                */
                if (Mathf.Abs(v2Delta.x) * 4.0f > Mathf.Abs(v2Delta.y))
                {
                    eventArgs.torsion = -v2Delta.x / Mathf.Abs(v2Delta.x) * 90.0f * (Mathf.Abs(v2Delta.x) * 4.0f - Mathf.Abs(v2Delta.y)) / 2.0f; // 2.0f - Высчитать коефициент в зависимости от времени полета (обратно пропорционально скорости) и угла между 30 и 45 градусами отклонения
                }
                float dxSign = v2Delta.x / Mathf.Abs(v2Delta.x);
                /*
                Debug.Log("#1 d.x: " + v2Delta.x + " ; d.y: " + v2Delta.y);
                Debug.Log("#1 sqrt(d.x): " + (Mathf.Pow(Mathf.Abs(v2Delta.x), 0.75f) * dxSign));
                Debug.Log("#1 sqrt(d.x) / 2: " + (Mathf.Pow(Mathf.Abs(v2Delta.x), 0.75f) * dxSign) / 2.0f);
                Debug.Log("#1 atan(sqrt(d.x) / 2): " + Mathf.Atan(Mathf.Pow(Mathf.Abs(v2Delta.x), 0.75f) * dxSign / 2.0f));
                */
                //if (Mathf.Abs(v2Delta.x) < 0.01)
                //{
                //    eventArgs.angle.x = 0.0f;
                //}
                //else
                //{
                //    eventArgs.angle.x = Mathf.Atan(Mathf.Pow(Mathf.Abs(v2Delta.x), 0.75f /* elliptic distortion */) * dxSign / v2Delta.y) * 180.0f / Mathf.PI;
                //}
                eventArgs.angle.x = Mathf.Atan(v2Delta.x / v2Delta.y);
                if (Mathf.Abs(eventArgs.angle.x) > 0.001f)
                {
                    eventArgs.angle.x = Mathf.Pow(Mathf.Abs(v2Delta.x), 2.0f) * dxSign;
                }
                eventArgs.angle.x *= 180.0f / Mathf.PI;
                eventArgs.angle.y = length / maxLength;
                eventArgs.speed = Mathf.Sqrt(0.2f / duration);
                eventArgs.throwing = true;
                InvokeAction(eventArgs);
                touched = false;
                started = true;
                locked = true;
            }
            else if(touched && pointsList.Count > 2 && length > minLength && fullY > 0.0f)
            {
                // Угол броска по горизонтали не больше 45 градусов от прямого направления
                if (Mathf.Abs(fullX) > Mathf.Abs(fullY))
                {
                    fullX = fullX / Mathf.Abs(fullX) * Mathf.Abs(fullY);
                }
                v2Delta.x = fullX;
                v2Delta.y = fullY;
                v2Delta.Normalize();
                eventArgs = new SwipeEventArgs();
                if (Mathf.Abs(v2Delta.x) * 4.0f > Mathf.Abs(v2Delta.y))
                {
                    eventArgs.torsion = -v2Delta.x / Mathf.Abs(v2Delta.x) * 90.0f * (Mathf.Abs(v2Delta.x) * 4.0f - Mathf.Abs(v2Delta.y)) / 2.0f; // 2.0f - Высчитать коефициент в зависимости от времени полета (обратно пропорционально скорости) и угла между 30 и 45 градусами отклонения
                }
                float dxSign = v2Delta.x / Mathf.Abs(v2Delta.x);
                eventArgs.angle.x = Mathf.Atan(v2Delta.x / v2Delta.y);
                if (Mathf.Abs(eventArgs.angle.x) > 0.001f)
                {
                    eventArgs.angle.x = Mathf.Pow(Mathf.Abs(v2Delta.x), 2.0f) * dxSign;
                }
                eventArgs.angle.x *= 180.0f / Mathf.PI;
                eventArgs.angle.y = length / maxLength;
                eventArgs.speed = Mathf.Sqrt(0.2f / duration);
                eventArgs.throwing = false;
                InvokeAction(eventArgs);
            }
            if ((!touched && started) || (pointsList.Count > 1 && (pointsList.Last.Previous.Value.point - newPoint).y < 0.0f))
            {
                pointsList.Clear();
                started = false;
            }
        }
    }

    public event EventHandler<SwipeEventArgs> OnInvokeAction;

    public void InvokeAction(SwipeEventArgs e)
    {
        EventHandler<SwipeEventArgs> handler = OnInvokeAction;
        if (handler != null)
        {
            handler(this, e);
        }
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

class FourFloatEventMessage : MessageBase
{
    public GameNetwork.ClientEvent clientEvent;
    public float value1 = 0.0f;
    public float value2 = 0.0f;
    public float value3 = 0.0f;
    public float value4 = 0.0f;
}

class GameMessage
{
    public GameNetwork.ClientEvent clientEvent;
}

class FourFloatMessage : GameMessage
{
    public float value1 = 0.0f;
    public float value2 = 0.0f;
    public float value3 = 0.0f;
    public float value4 = 0.0f;
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

    public enum ObjectType
    {
        NONE = 0,
        OBSTRUCTION = 1,
        PLAYER = 2,
        MISSILE = 3
    };

    public enum VisualEffects
    {
        NONE = 0,
        HIT = 1,
        SPARKS = 2,
        RED_SCREEN = 3,
        GREEN_SCREEN = 4,
        RAVEN = 1001
    };

    public static float gravity = -0.098f;

    private GameNetwork network;
    private LinkedList<LocationObject> objects = new LinkedList<LocationObject>();

    public void SetNetworkBehavior(GameNetwork pointer)
    {
        network = pointer;
    }

    public void AddObject(ObstructionObject obj)
    {
        objects.AddLast(obj);
        if (network.isServer && !network.isLocal)
        {

            network.RpcSpawnObject(obj.id, obj.objectType, obj.position, obj.velocity, obj.acceleration, obj.torsion, obj.scale, obj.visualId);
        }
    }

    public void AddObject(PlayerObject obj)
    {
        objects.AddLast(obj);
        if (network.isServer && !network.isLocal)
        {
            network.RpcSpawnObject(obj.id, obj.objectType, obj.position, obj.velocity, obj.acceleration, obj.torsion, obj.scale, obj.visualId);
        }
        else
        {
        }
    }

    public void AddObject(MissileObject obj)
    {
        objects.AddLast(obj);
        if (network.isServer && !network.isLocal)
        {
            network.RpcSpawnObject(obj.id, obj.objectType, obj.position, obj.velocity, obj.acceleration, obj.torsion, obj.torsion.y, obj.visualId);
        }
    }

    public void RemoveObject(LocationObject obj)
    {
        if (network.isServer && !network.isLocal)
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
        LocationObject obj;
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
                obj = objNode.Value;
                obj.velocity += obj.acceleration * deltaTime;
                obj.position += obj.velocity * deltaTime;
                switch (objNode.Value.objectType)
                {
                    case ObjectType.PLAYER:
                        playerObject = (PlayerObject)objNode.Value;
                        //Debug.Log("[" + playerObject.id + "] position: " + playerObject.position + " ; velocity: " + playerObject.velocity);
                        if (playerObject.visualObject != null)
                        {
                            /*
                            if (Mathf.Abs(network.camera.transform.position.x - playerObject.position.x * 10.0f) > 30.0f)
                            {
                                if (network.camera.transform.position.x - playerObject.position.x * 10.0f > 0.0f)
                                {
                                    playerObject.position += Vector3.right * 6.0f;
                                }
                                else
                                {
                                    playerObject.position += Vector3.right * -6.0f;
                                }
                            }
                            v3Delta = Vector3.right * (playerObject.position.x * 10.0f - playerObject.visualObject.transform.position.x);
                            if (Mathf.Abs(v3Delta.x) > 10.0f)
                            {
                                if (v3Delta.x > 0.0f)
                                {
                                    playerObject.visualObject.transform.position += Vector3.right * 6.0f * 10.0f;
                                }
                                else
                                {
                                    playerObject.visualObject.transform.position += Vector3.right * -6.0f * 10.0f;
                                }
                                v3Delta = Vector3.right * (playerObject.position.x * 10.0f - playerObject.visualObject.transform.position.x);
                            }
                            playerObject.visualObject.transform.position += v3Delta * Mathf.Min(1.0f, deltaTime * 15.0f);
                            */
                            playerObject.visualObject.transform.position = playerObject.position * 100.0f;
                            if ((playerObject.velocity.x > 0.0f && playerObject.position.z < 0.0f) || (playerObject.velocity.x < 0.0f && playerObject.position.z > 0.0f))
                            {
                                playerObject.visualObject.Animate(1);
                            }
                            else
                            {
                                playerObject.visualObject.Animate(0);
                            }
                        }
                        else
                        {
                            /*
                            v3Delta = Vector3.right * (playerObject.position.x * 10.0f - network.camera.transform.position.x);
                            if (Mathf.Abs(v3Delta.x) > 10.0f)
                            {
                                if (v3Delta.x > 0.0f)
                                {
                                    network.camera.transform.position += Vector3.right * 4.0f * 10.0f;
                                }
                                else
                                {
                                    network.camera.transform.position += Vector3.right * -4.0f * 10.0f;
                                }
                                v3Delta = Vector3.right * (playerObject.position.x * 10.0f - network.camera.transform.position.x);
                            }
                            network.camera.transform.position += v3Delta * Mathf.Min(1.0f, deltaTime * 15.0f);
                            */
                            network.camera.transform.position = playerObject.position * 100.0f + Vector3.up * 20.0f;
                        }
                        if (playerObject.id == network.playerId)
                        {
                            network.healthBarSelf.text = Mathf.Floor(playerObject.health) + "";
                            network.staminaBar.rectTransform.sizeDelta = new Vector2(network.staminaBar.rectTransform.sizeDelta.x + (network.gameMatchMaker.canvasPlay.pixelRect.width * playerObject.stamina / 100.0f - network.staminaBar.rectTransform.sizeDelta.x) * Mathf.Min(1.0f, Time.deltaTime * 15.0f), network.staminaBar.rectTransform.sizeDelta.y);
                        }
                        else
                        {
                            network.healthBarEnemy.text = Mathf.Floor(playerObject.health) + "";
                        }
                        break;
                    case ObjectType.OBSTRUCTION:
                        obstructionObject = (ObstructionObject)objNode.Value;
                        if (Mathf.Abs(network.camera.transform.position.x - obstructionObject.visualObject.transform.position.x) > 3.0f * 100.0f)
                        {
                            if (network.camera.transform.position.x - obstructionObject.visualObject.transform.position.x > 0.0f)
                            {
                                obstructionObject.visualObject.transform.position += Vector3.right * 6.0f * 100.0f;
                            }
                            else
                            {
                                obstructionObject.visualObject.transform.position += Vector3.right * -6.0f * 100.0f;
                            }
                        }
                        break;
                    case ObjectType.MISSILE:
                        missileObject = (MissileObject)objNode.Value;
                        //Debug.Log("[" + Time.time + "] position: " + missileObject.position + " ; velocity: " + missileObject.velocity);
                        if (missileObject.visualObject != null)
                        {
                            //v3Delta = missileObject.position * 10.0f - missileObject.visualObject.transform.position;
                            //missileObject.visualObject.transform.position += v3Delta * Mathf.Min(1.0f, deltaTime * 15.0f);
                            missileObject.visualObject.transform.position = missileObject.position * 100.0f;
                            //scale = 1.0f - (missileObject.position.y + 1.0f) * 0.3f;
                            //missileObject.visualObject.transform.localScale = new Vector3(scale, Mathf.Pow(scale, 1.5f), 1.0f);
                        }
                        break;
                }
                if(objNode.Value.floatingNotifyOffset > 0.0f)
                {
                    objNode.Value.floatingNotifyOffset -= Time.deltaTime;
                    if(objNode.Value.floatingNotifyOffset < 0.0f)
                    {
                        objNode.Value.floatingNotifyOffset = 0.0f;
                    }
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
                        playerObject.position.x += playerObject.MoveSpeed() * deltaTime;
                        if (playerObject.position.x < -3.0f)
                        {
                            playerObject.position += Vector3.right * 6.0f;
                        }
                        if (playerObject.position.x > 3.0f)
                        {
                            playerObject.position += Vector3.right * -6.0f;
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
                            if (deltaX > 3.0f)
                            {
                                deltaX += -6.0f;
                            }
                            if (deltaX < -3.0f)
                            {
                                deltaX += 6.0f;
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
                        if (playerObject.stamina < playerObject.maxStamina)
                        {
                            playerObject.stamina += playerObject.staminaRegeneration * Time.deltaTime;
                            if (playerObject.stamina > playerObject.maxStamina)
                            {
                                playerObject.stamina = playerObject.maxStamina;
                            }
                        }
                        if (playerObject.legInjury > 0.0f)
                        {
                            playerObject.legInjury -= deltaTime;
                            if (playerObject.legInjury < 0.0f)
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
                        if (network.updating && !network.isLocal)
                        {
                            network.RpcMoveObject(objNode.Value.id, objNode.Value.position, objNode.Value.velocity, objNode.Value.acceleration, objNode.Value.torsion, objNode.Value.scale, 0.0f);
                            network.RpcUpdatePlayer(playerObject.id, playerObject.health, playerObject.stamina, playerObject.staminaConsumption);
                        }
                        break;
                    case ObjectType.OBSTRUCTION:
                        //obstructionObject = (ObstructionObject) objNode.Value;
                        break;
                    case ObjectType.MISSILE:
                        missileObject = (MissileObject)objNode.Value;
                        missileObject.direction = Quaternion.Euler(missileObject.torsion.x * deltaTime, missileObject.torsion.y * deltaTime, missileObject.torsion.z * deltaTime) * missileObject.direction;
                        missileObject.direction.y += -0.98f * 0.45f * deltaTime;
                        missileObject.direction.Normalize();
                        missileObject.velocity.x += missileObject.acceleration.x * deltaTime;
                        missileObject.velocity.y += missileObject.acceleration.y * deltaTime;
                        missileObject.velocity.z += missileObject.acceleration.z * deltaTime;
                        missileObject.position.x += missileObject.velocity.x * deltaTime;
                        missileObject.position.y += missileObject.velocity.y * deltaTime;
                        missileObject.position.z += missileObject.velocity.z * deltaTime;
                        //Debug.Log("missileObject position: " + missileObject.position);
                        if (missileObject.position.y <= 0.0f)
                        {
                            Debug.Log("missileObject DESTROY by position.z");
                            if (missileObject.visualObject != null)
                            {
                                GameObject.Destroy(missileObject.visualObject.gameObject);
                            }
                            RemoveObject(objNode.Value);
                        }
                        else if (missileObject.position.z > 2.5f && missileObject.position.z - missileObject.direction.z * missileObject.velocity.z * deltaTime <= 2.5f)
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
                                            if (Mathf.Abs(missileObject.position.x - obstructionObject.position.x) < obstructionObject.scale)
                                            {
                                                if (missileObject.direction.y > 0.0f)
                                                {
                                                    attackerObject = (PlayerObject)objects.First.Value;
                                                }
                                                else
                                                {
                                                    attackerObject = (PlayerObject)objects.First.Next.Value;
                                                }
                                                obstructionObject.durability -= UnityEngine.Random.Range(attackerObject.minDamage, attackerObject.maxDamage);
                                                if (obstructionObject.durability <= 0.0f)
                                                {
                                                    if (obstructionObject.visualObject != null)
                                                    {
                                                        GameObject.Destroy(obstructionObject.visualObject.gameObject);
                                                    }
                                                    RemoveObject(objNode2.Value);
                                                    if (missileObject.visualObject != null)
                                                    {
                                                        GameObject.Destroy(missileObject.visualObject.gameObject);
                                                    }
                                                    RemoveObject(objNode.Value);
                                                    objNodeNext2 = null;
                                                    float hpBonus = 0.0f;
                                                    if (UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f)
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
                                                    if (hpBonus != 0.0f)
                                                    {
                                                        if (missileObject.direction.y > 0.0f)
                                                        {
                                                            playerObject = (PlayerObject)objects.First.Value;
                                                        }
                                                        else
                                                        {
                                                            playerObject = (PlayerObject)objects.First.Next.Value;
                                                        }
                                                        if (playerObject != null)
                                                        {
                                                            playerObject.health += hpBonus;
                                                            if (hpBonus > 0.0f)
                                                            {
                                                                if (!network.isLocal)
                                                                {
                                                                    network.RpcShowNotice(playerObject.id, "+" + hpBonus, noticeOffset, 0, true);
                                                                }
                                                                network.ShowNotice(playerObject.id, "+" + hpBonus, noticeOffset, 0, true);
                                                            }
                                                            else
                                                            {
                                                                if (!network.isLocal)
                                                                {
                                                                    network.RpcShowNotice(playerObject.id, "" + hpBonus, noticeOffset, 1, true);
                                                                }
                                                                network.ShowNotice(playerObject.id, "" + hpBonus, noticeOffset, 1, true);
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    network.FlashObstruction(obstructionObject.id);
                                                    if (!network.isLocal)
                                                    {
                                                        network.RpcFlashObstruction(obstructionObject.id);
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                }
                                objNode2 = objNodeNext2;
                            }
                        }
                        else if ((missileObject.direction.z > 0.0f && missileObject.position.z >= 5.0f) || (missileObject.direction.z < 0.0f && missileObject.position.z <= -5.0f))
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
                                            if (missileObject.direction.z > 0.0f && playerObject.id == 1 && Mathf.Abs(missileObject.position.x - playerObject.position.x) < 0.3f)
                                            {
                                                hit = true;
                                                attackerObject = (PlayerObject)objects.First.Value;
                                            }
                                            else if (missileObject.direction.z < 0.0f && playerObject.id == 0 && Mathf.Abs(missileObject.position.x - playerObject.position.x) < 0.3f)
                                            {
                                                hit = true;
                                                attackerObject = (PlayerObject)objects.First.Next.Value;
                                            }
                                            if (hit && playerObject.abilityEvade == 0.0f && UnityEngine.Random.Range(0.0f, 1.0f) < playerObject.abilityEvadeChance)
                                            {
                                                hit = false;
                                                playerObject.abilityEvade = 5.0f;
                                                if (!network.isLocal)
                                                {
                                                    network.RpcShowNotice(playerObject.id, "+ УКЛОНЕНИЕ", noticeOffset, 0, true);
                                                }
                                                network.ShowNotice(playerObject.id, "+ УКЛОНЕНИЕ", noticeOffset, 0, true);
                                                noticeOffset += 1.0f;
                                                if (playerObject.id == 0)
                                                {
                                                    network.FlashPassiveAbility(playerObject.id);
                                                }
                                                else
                                                {
                                                    if (!network.isLocal)
                                                    {
                                                        network.RpcFlashPassiveAbility(playerObject.id);
                                                    }
                                                }
                                                // Ability Evade
                                            }
                                            if (hit)
                                            {
                                                notifyMessage = "";
                                                network.FlashPlayer(playerObject.id);
                                                if (!network.isLocal)
                                                {
                                                    network.RpcFlashPlayer(playerObject.id);
                                                }
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
                                                        if (!network.isLocal)
                                                        {
                                                            network.RpcFlashPassiveAbility(attackerObject.id);
                                                        }
                                                    }
                                                    // Ability Crit
                                                }
                                                if (UnityEngine.Random.Range(0.0f, 1.0f) < critChance)
                                                {
                                                    notifyMessage += "K";
                                                    damage *= attackerObject.critMultiplier;
                                                }
                                                if (playerObject.abilityShield >= 5.0f)
                                                {
                                                    notifyMessage += "Щ";
                                                    damage *= playerObject.abilityShieldMultiplier;
                                                    if (!network.isLocal)
                                                    {
                                                        network.RpcShowNotice(playerObject.id, "+ ЩИТ", noticeOffset, 0, true);
                                                    }
                                                    network.ShowNotice(playerObject.id, "+ ЩИТ", noticeOffset, 0, true);
                                                    noticeOffset += 1.0f;
                                                }
                                                notifyMessage += " -" + Mathf.Floor(damage);
                                                if (playerObject.id == 0)
                                                {
                                                    if (!network.isLocal)
                                                    {
                                                        network.RpcShowNotice(playerObject.id, notifyMessage, noticeOffset, 1, true);
                                                    }
                                                    network.ShowNotice(playerObject.id, notifyMessage, 1.0f, 1, false);
                                                }
                                                else
                                                {
                                                    if (!network.isLocal)
                                                    {
                                                        network.RpcShowNotice(playerObject.id, notifyMessage, 1.0f, 1, false);
                                                    }
                                                    network.ShowNotice(playerObject.id, notifyMessage, noticeOffset, 1, true);
                                                }
                                                noticeOffset += 1.0f;
                                                playerObject.health -= damage;
                                                if (attackerObject.stunMove > 0.0f)
                                                {
                                                    attackerObject.stunMove = 0.0f;
                                                    playerObject.stun += attackerObject.abilityStunDuration;
                                                    if (!network.isLocal)
                                                    {
                                                        network.RpcShowNotice(playerObject.id, "- ОГЛУШЕН", noticeOffset, 1, true);
                                                    }
                                                    network.ShowNotice(playerObject.id, "- ОГЛУШЕН", noticeOffset, 1, true);
                                                    if (!network.isLocal)
                                                    {
                                                        network.RpcShowNotice(playerObject.id, "- ОГЛУШЕН", 5.0f, 1, false);
                                                    }
                                                    network.ShowNotice(playerObject.id, "- ОГЛУШЕН", 5.0f, 1, false);
                                                    noticeOffset += 1.0f;
                                                }
                                                if (UnityEngine.Random.Range(0.0f, 1.0f) < attackerObject.injuryChance)
                                                {
                                                    if (UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f)
                                                    {
                                                        // Injury Arm
                                                        playerObject.armInjury = 8.0f;
                                                        if (!network.isLocal)
                                                        {
                                                            network.RpcShowNotice(playerObject.id, "- РУКА", noticeOffset, 1, true);
                                                        }
                                                        network.ShowNotice(playerObject.id, "- РУКА", noticeOffset, 1, true);
                                                        if (!network.isLocal)
                                                        {
                                                            network.RpcShowNotice(playerObject.id, "- РУКА", 8.0f, 1, false);
                                                        }
                                                        network.ShowNotice(playerObject.id, "- РУКА", 8.0f, 1, false);
                                                        noticeOffset += 1.0f;
                                                    }
                                                    else
                                                    {
                                                        // Injury Leg
                                                        playerObject.legInjury = 8.0f;
                                                        if (!network.isLocal)
                                                        {
                                                            network.RpcShowNotice(playerObject.id, "- НОГА", noticeOffset, 1, true);
                                                        }
                                                        network.ShowNotice(playerObject.id, "- НОГА", noticeOffset, 1, true);
                                                        if (!network.isLocal)
                                                        {
                                                            network.RpcShowNotice(playerObject.id, "- НОГА", 8.0f, 1, false);
                                                        }
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
                            if (missileObject.visualObject != null)
                            {
                                GameObject.Destroy(missileObject.visualObject.gameObject);
                            }
                            RemoveObject(objNode.Value);
                        }
                        if (network.updating && !network.isLocal)
                        {
                            network.RpcMoveObject(objNode.Value.id, objNode.Value.position, objNode.Value.velocity, objNode.Value.acceleration, objNode.Value.torsion, objNode.Value.scale, 0.0f);
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
        while (objNode != null)
        {
            if (objNode.Value.id == id)
            {
                return objNode.Value;
            }
            objNode = objNode.Next;
        }
        return null;
    }

    public void GameOver(int winner)
    {
        if (!network.isLocal)
        {
            network.RpcGameOver(winner, 0.0f, 0.0f, 0.0f);
        }
        network.GameOver(winner, 0.0f, 0.0f, 0.0f);
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
                    if (playerObject.visualObject != null)
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

    public float lastTimestamp = 0.0f;
    public float lastRemoteTimestamp = 0.0f;
    public Vector3 lastPosition = new Vector3();

    public int id = -1;
    public Location.ObjectType objectType = Location.ObjectType.NONE;
    public int visualId = -1;
    public Vector3 position = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 velocity = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 acceleration = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 torsion = new Vector3(0.0f, 0.0f, 0.0f);
    //public Vector3 localVelocity = new Vector3(0.0f, 0.0f, 0.0f);
    //public Vector3 passiveVelocity = new Vector3(0.0f, 0.0f, 0.0f);
    public float scale = 0.0f;

    public float floatingNotifyOffset = 0.0f;

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

    public float MoveSpeed()
    {
        float moveSpeed = direction * strafeSpeed;
        if (stun > 0.0f)
        {
            moveSpeed = 0.0f;
        }
        else if (legInjury > 0.0f)
        {
            moveSpeed /= 1.0f + legInjuryEffect;
        }
        return moveSpeed;
    }

}

public class MissileObject : LocationObject
{

    public MissileObject() : base()
    {
        objectType = Location.ObjectType.MISSILE;
    }

    public Vector3 direction = new Vector3(0.0f, 0.0f, 0.0f);
    //public float velocity = 0.0f;
    public MissileController visualObject;

}

public class BaseObjectMessage
{

    public int id;
    public float timestamp = 0.0f;
    public float timemark = 0.0f;
    public byte eventCode = 0;

    public BaseObjectMessage()
    {

    }

    public BaseObjectMessage(float currentTimestamp, float targetTimemark)
    {
        timestamp = currentTimestamp;
        timemark = targetTimemark;
    }

    public virtual byte[] Pack()
    {
        int index = 0;
        byte[] data = new byte[4 * 3];
        PackBase(ref data, ref index);
        return data;
    }

    public virtual void Unpack(byte[] data)
    {
        int index = 0;
        UnpackBase(ref data, ref index);
    }

    public void PackBase(ref byte[] data, ref int index)
    {
        PutInt(data, id, ref index);
        PutFloat(data, timestamp, ref index);
        PutFloat(data, timemark, ref index);
    }

    public void UnpackBase(ref byte[] data, ref int index)
    {
        id = GetInt(data, ref index);
        timestamp = GetFloat(data, ref index);
        timemark = GetFloat(data, ref index);
    }

    public bool GetBool(byte[] data, ref int index)
    {
        bool value = BitConverter.ToBoolean(data, index);
        index += 1;
        return value;
    }

    public int GetInt(byte[] data, ref int index)
    {
        int value = BitConverter.ToInt32(data, index);
        index += 4;
        return value;
    }

    public float GetFloat(byte[] data, ref int index)
    {
        float value = BitConverter.ToSingle(data, index);
        index += 4;
        return value;
    }

    public void PutBool(byte[] data, bool value, ref int index)
    {
        byte[] b1 = BitConverter.GetBytes(value);
        Buffer.BlockCopy(b1, 0, data, index, 1);
        index += 1;
    }

    public void PutInt(byte[] data, int value, ref int index)
    {
        byte[] b4 = BitConverter.GetBytes(value);
        Buffer.BlockCopy(b4, 0, data, index, 4);
        index += 4;
    }

    public void PutFloat(byte[] data, float value, ref int index)
    {
        byte[] b4 = BitConverter.GetBytes(value);
        Buffer.BlockCopy(b4, 0, data, index, 4);
        index += 4;
    }

}

public class SpawnObjectMessage : BaseObjectMessage
{

    public Location.ObjectType objectType;
    public int objectId = -1;
    public Vector3 newPosition;
    public Vector3 newVelocity;
    public Vector3 newAcceleration;
    public Vector3 newTorsion;
    public float newFloat;
    public int visualId;

    public SpawnObjectMessage() : base()
    {
    }

    public SpawnObjectMessage(float currentTimestamp, float targetTimemark) : base(currentTimestamp, targetTimemark)
    {
    }

    public override byte[] Pack()
    {
        int index = 0;
        byte[] data = new byte[4 * 19];
        PackBase(ref data, ref index);
        PutInt(data, objectId, ref index);
        PutInt(data, (int)objectType, ref index);
        PutFloat(data, newPosition.x, ref index);
        PutFloat(data, newPosition.y, ref index);
        PutFloat(data, newPosition.z, ref index);
        PutFloat(data, newVelocity.x, ref index);
        PutFloat(data, newVelocity.y, ref index);
        PutFloat(data, newVelocity.z, ref index);
        PutFloat(data, newAcceleration.x, ref index);
        PutFloat(data, newAcceleration.y, ref index);
        PutFloat(data, newAcceleration.z, ref index);
        PutFloat(data, newTorsion.x, ref index);
        PutFloat(data, newTorsion.y, ref index);
        PutFloat(data, newTorsion.z, ref index);
        PutFloat(data, newFloat, ref index);
        PutInt(data, visualId, ref index);
        return data;
    }

    public override void Unpack(byte[] data)
    {
        int index = 0;
        UnpackBase(ref data, ref index);
        objectId = GetInt(data, ref index);
        objectType = (Location.ObjectType)GetInt(data, ref index);
        newPosition = new Vector3();
        newPosition.x = GetFloat(data, ref index);
        newPosition.y = GetFloat(data, ref index);
        newPosition.z = GetFloat(data, ref index);
        newVelocity.x = GetFloat(data, ref index);
        newVelocity.y = GetFloat(data, ref index);
        newVelocity.z = GetFloat(data, ref index);
        newAcceleration.x = GetFloat(data, ref index);
        newAcceleration.y = GetFloat(data, ref index);
        newAcceleration.z = GetFloat(data, ref index);
        newTorsion.x = GetFloat(data, ref index);
        newTorsion.y = GetFloat(data, ref index);
        newTorsion.z = GetFloat(data, ref index);
        newFloat = GetFloat(data, ref index);
        visualId = GetInt(data, ref index);
    }

}

public class DestroyObjectMessage : BaseObjectMessage
{

    public int objectId = -1;

    public DestroyObjectMessage() : base()
    {
    }

    public DestroyObjectMessage(float currentTimestamp, float targetTimemark) : base(currentTimestamp, targetTimemark)
    {
    }

    public override byte[] Pack()
    {
        int index = 0;
        byte[] data = new byte[4 * 4];
        PackBase(ref data, ref index);
        PutInt(data, objectId, ref index);
        return data;
    }

    public override void Unpack(byte[] data)
    {
        int index = 0;
        UnpackBase(ref data, ref index);
        objectId = GetInt(data, ref index);
    }

}

public class MoveObjectMessage : BaseObjectMessage
{

    public int objectId = -1;
    public Vector3 newPosition;
    public Vector3 newVelocity;
    public Vector3 newAcceleration;
    public Vector3 newTorsion;
    public float newFloat;

    public MoveObjectMessage() : base()
    {
    }

    public MoveObjectMessage(float currentTimestamp, float targetTimemark) : base(currentTimestamp, targetTimemark)
    {
    }

    public override byte[] Pack()
    {
        int index = 0;
        byte[] data = new byte[4 * 17];
        PackBase(ref data, ref index);
        PutInt(data, objectId, ref index);
        PutFloat(data, newPosition.x, ref index);
        PutFloat(data, newPosition.y, ref index);
        PutFloat(data, newPosition.z, ref index);
        PutFloat(data, newVelocity.x, ref index);
        PutFloat(data, newVelocity.y, ref index);
        PutFloat(data, newVelocity.z, ref index);
        PutFloat(data, newAcceleration.x, ref index);
        PutFloat(data, newAcceleration.y, ref index);
        PutFloat(data, newAcceleration.z, ref index);
        PutFloat(data, newTorsion.x, ref index);
        PutFloat(data, newTorsion.y, ref index);
        PutFloat(data, newTorsion.z, ref index);
        PutFloat(data, newFloat, ref index);
        return data;
    }

    public override void Unpack(byte[] data)
    {
        int index = 0;
        UnpackBase(ref data, ref index);
        objectId = GetInt(data, ref index);
        newPosition = new Vector3();
        newPosition.x = GetFloat(data, ref index);
        newPosition.y = GetFloat(data, ref index);
        newPosition.z = GetFloat(data, ref index);
        newVelocity.x = GetFloat(data, ref index);
        newVelocity.y = GetFloat(data, ref index);
        newVelocity.z = GetFloat(data, ref index);
        newAcceleration.x = GetFloat(data, ref index);
        newAcceleration.y = GetFloat(data, ref index);
        newAcceleration.z = GetFloat(data, ref index);
        newTorsion.x = GetFloat(data, ref index);
        newTorsion.y = GetFloat(data, ref index);
        newTorsion.z = GetFloat(data, ref index);
        newFloat = GetFloat(data, ref index);
        index += 4;
    }

}

public class UpdatePlayerMessage : BaseObjectMessage
{

    public float newHealth;
    public float newStamina;
    public float newStaminaConsumption;

    public UpdatePlayerMessage() : base()
    {
    }

    public UpdatePlayerMessage(float currentTimestamp, float targetTimemark) : base(currentTimestamp, targetTimemark)
    {
    }

    public override byte[] Pack()
    {
        int index = 0;
        byte[] data = new byte[4 * 6];
        PackBase(ref data, ref index);
        PutFloat(data, newHealth, ref index);
        PutFloat(data, newStamina, ref index);
        PutFloat(data, newStaminaConsumption, ref index);
        return data;
    }

    public override void Unpack(byte[] data)
    {
        int index = 0;
        UnpackBase(ref data, ref index);
        newHealth = GetFloat(data, ref index);
        newStamina = GetFloat(data, ref index);
        newStaminaConsumption = GetFloat(data, ref index);
    }

}

public class SetAbilityMessage : BaseObjectMessage
{

    public int value;

    public SetAbilityMessage() : base()
    {
    }

    public SetAbilityMessage(float currentTimestamp, float targetTimemark) : base(currentTimestamp, targetTimemark)
    {
    }

    public override byte[] Pack()
    {
        int index = 0;
        byte[] data = new byte[4 * 4];
        PackBase(ref data, ref index);
        PutInt(data, value, ref index);
        return data;
    }

    public override void Unpack(byte[] data)
    {
        int index = 0;
        UnpackBase(ref data, ref index);
        value = GetInt(data, ref index);
    }

}

public class NoticeMessage : BaseObjectMessage
{

    public int numericValue;
    public int prefixMessage;
    public int suffixMessage;
    public float offset;
    public int color;
    public bool floating;

    public NoticeMessage() : base()
    {
    }

    public NoticeMessage(float currentTimestamp, float targetTimemark) : base(currentTimestamp, targetTimemark)
    {
    }

    public override byte[] Pack()
    {
        int index = 0;
        byte[] data = new byte[4 * 8 + 1];
        PackBase(ref data, ref index);
        PutInt(data, numericValue, ref index);
        PutInt(data, prefixMessage, ref index);
        PutInt(data, suffixMessage, ref index);
        PutFloat(data, offset, ref index);
        PutInt(data, color, ref index);
        PutBool(data, floating, ref index);
        return data;
    }

    public override void Unpack(byte[] data)
    {
        int index = 0;
        UnpackBase(ref data, ref index);
        numericValue = GetInt(data, ref index);
        prefixMessage = GetInt(data, ref index);
        suffixMessage = GetInt(data, ref index);
        offset = GetFloat(data, ref index);
        color = GetInt(data, ref index);
        floating = GetBool(data, ref index);
    }

}

public class ThrowMessage : BaseObjectMessage
{

    public float angleX;
    public float angleY;
    public float torsion;
    public float speed;

    public ThrowMessage() : base()
    {
    }

    public ThrowMessage(float currentTimestamp) : base(currentTimestamp, 0.0f)
    {
    }

    public override byte[] Pack()
    {
        int index = 0;
        byte[] data = new byte[4 * 7];
        PackBase(ref data, ref index);
        PutFloat(data, angleX, ref index);
        PutFloat(data, angleY, ref index);
        PutFloat(data, torsion, ref index);
        PutFloat(data, speed, ref index);
        return data;
    }

    public override void Unpack(byte[] data)
    {
        int index = 0;
        UnpackBase(ref data, ref index);
        angleX = GetFloat(data, ref index);
        angleY = GetFloat(data, ref index);
        torsion = GetFloat(data, ref index);
        speed = GetFloat(data, ref index);
    }

}

public class InitializeMessage : BaseObjectMessage
{

    public int abilityFirstId = -1;
    public int abilitySecondId = -1;
    public int missileId = -1;
    public int venomId = -1;

    public InitializeMessage() : base()
    {
    }

    public InitializeMessage(float currentTimestamp, float targetTimemark) : base(currentTimestamp, targetTimemark)
    {
    }

    public override byte[] Pack()
    {
        int index = 0;
        byte[] data = new byte[4 * 7];
        PackBase(ref data, ref index);
        PutInt(data, abilityFirstId, ref index);
        PutInt(data, abilitySecondId, ref index);
        PutInt(data, missileId, ref index);
        PutInt(data, venomId, ref index);
        return data;
    }

    public override void Unpack(byte[] data)
    {
        int index = 0;
        UnpackBase(ref data, ref index);
        abilityFirstId = GetInt(data, ref index);
        abilitySecondId = GetInt(data, ref index);
        missileId = GetInt(data, ref index);
        venomId = GetInt(data, ref index);
    }

}

public class VisualEffectMessage : BaseObjectMessage
{

    public int invokerId = -1;
    public int targetId = -1;
    public Vector3 targetPosition;
    public Vector3 direction;
    public float duration = 0.0f;

    public VisualEffectMessage() : base()
    {
    }

    public VisualEffectMessage(float currentTimestamp, float targetTimemark) : base(currentTimestamp, targetTimemark)
    {
    }

    public override byte[] Pack()
    {
        int index = 0;
        byte[] data = new byte[4 * 12];
        PackBase(ref data, ref index);
        PutInt(data, invokerId, ref index);
        PutInt(data, targetId, ref index);
        PutFloat(data, targetPosition.x, ref index);
        PutFloat(data, targetPosition.y, ref index);
        PutFloat(data, targetPosition.z, ref index);
        PutFloat(data, direction.x, ref index);
        PutFloat(data, direction.y, ref index);
        PutFloat(data, direction.z, ref index);
        PutFloat(data, duration, ref index);
        return data;
    }

    public override void Unpack(byte[] data)
    {
        int index = 0;
        UnpackBase(ref data, ref index);
        invokerId = GetInt(data, ref index);
        targetId = GetInt(data, ref index);
        targetPosition.x = GetFloat(data, ref index);
        targetPosition.y = GetFloat(data, ref index);
        targetPosition.z = GetFloat(data, ref index);
        direction.x = GetFloat(data, ref index);
        direction.y = GetFloat(data, ref index);
        direction.z = GetFloat(data, ref index);
        duration = GetFloat(data, ref index);
    }

}

public class GameOverMessage : BaseObjectMessage
{

    public int winner = -1;
    public float time = 0.0f;
    public float damage = 0.0f;
    public float wound = 0.0f;
    public int rank = -1;
    public int rankChange = -1;
    public int rankPoints = -1;
    public int rankPointsChange = -1;
    public int regionUnlocked = -1;

    public GameOverMessage() : base()
    {
    }

    public GameOverMessage(float currentTimestamp, float targetTimemark) : base(currentTimestamp, targetTimemark)
    {
    }

    public override byte[] Pack()
    {
        int index = 0;
        byte[] data = new byte[4 * 12];
        PackBase(ref data, ref index);
        PutInt(data, winner, ref index);
        PutFloat(data, time, ref index);
        PutFloat(data, damage, ref index);
        PutFloat(data, wound, ref index);
        PutInt(data, rank, ref index);
        PutInt(data, rankChange, ref index);
        PutInt(data, rankPointsChange, ref index);
        PutInt(data, rankPointsChange, ref index);
        PutInt(data, regionUnlocked, ref index);
        return data;
    }

    public override void Unpack(byte[] data)
    {
        int index = 0;
        UnpackBase(ref data, ref index);
        winner = GetInt(data, ref index);
        time = GetFloat(data, ref index);
        damage = GetFloat(data, ref index);
        wound = GetFloat(data, ref index);
        rank = GetInt(data, ref index);
        rankChange = GetInt(data, ref index);
        rankPoints = GetInt(data, ref index);
        rankPointsChange = GetInt(data, ref index);
        regionUnlocked = GetInt(data, ref index);
    }

}
