﻿using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class Done_Boundary 
{
	public float xMin, xMax, zMin, zMax;
}

public class Done_PlayerController : NetworkBehaviour
{
    public float speed;
    public float tilt;
    public Done_Boundary boundary;

    public GameObject shot;
    public Transform shotSpawn;
    public float fireRate;

    public int PlayerIndex;

    private void Start()
    {
        var ruyiProfileId = "";
        var ruyiProfileName = "";
        var ruyiNet = FindObjectOfType<RuyiNet>();
        if (ruyiNet != null &&
            ruyiNet.IsRuyiNetAvailable)
        {
            var activePlayer = ruyiNet.ActivePlayer;
            if (activePlayer != null)
            {
                ruyiProfileId = activePlayer.profileId;
                ruyiProfileName = activePlayer.profileName;
            }
            ruyiNet.Subscribe.Subscribe("service/" + Layer0.ServiceIDs.USER_SERVICE_EXTERNAL.ToString().ToLower());
            ruyiNet.Subscribe.AddMessageHandler<Ruyi.SDK.UserServiceExternal.InputActionEvent>(RuyiInputStateChangeHandler);
        }

        if (isLocalPlayer)
        {
            CmdRegister(ruyiProfileId, ruyiProfileName);
        }
    }

    [Command]
    private void CmdRegister(string ruyiProfileId, string ruyiProfileName)
    {
        var gameController = FindObjectOfType<Done_GameController>();
        gameController.RegisterPlayer(netId, ruyiProfileId, ruyiProfileName);
    }

    [Command]
    private void CmdSpawnBullet()
    {
        var bullet = Instantiate(shot, shotSpawn.position, shotSpawn.rotation);
        var mover = bullet.GetComponent<Done_Mover>();
        mover.PlayerNetId = netId;

        GetComponent<AudioSource>().Play();

        NetworkServer.Spawn(bullet);
    }

    void RuyiInputStateChangeHandler(string topic, Ruyi.SDK.UserServiceExternal.InputActionEvent msg)
    {
        for (int i = 0; i < msg.Triggers.Count; ++i)
        {
            Debug.Log("Done_PlayerController RuyiInputStateChangeHandler key:" + msg.Triggers[i].Key + " newValue:" + msg.Triggers[i].NewValue);

            if ( ((int)Ruyi.SDK.GlobalInputDefine.Key.Left == msg.Triggers[i].Key && 1 == msg.Triggers[i].NewValue)
                || ((int)Ruyi.SDK.GlobalInputDefine.RuyiControllerKey.eButtonLeft == msg.Triggers[i].Key || 1 == msg.Triggers[i].NewValue)
                || ((int)Ruyi.SDK.GlobalInputDefine.RuyiControllerKey.eAnalogLeftJoyX == msg.Triggers[i].Key || 1 == msg.Triggers[i].NewValue))
            {
                horizontalAxis = -1;
                isMove = true;
            }
            if ( ((int)Ruyi.SDK.GlobalInputDefine.Key.Right == msg.Triggers[i].Key && 1 == msg.Triggers[i].NewValue)
                || ((int)Ruyi.SDK.GlobalInputDefine.RuyiControllerKey.eButtonRight == msg.Triggers[i].Key || 1 == msg.Triggers[i].NewValue)
                || ((int)Ruyi.SDK.GlobalInputDefine.RuyiControllerKey.eAnalogRightJoyX == msg.Triggers[i].Key || 1 == msg.Triggers[i].NewValue))
            {
                horizontalAxis = 1;
                isMove = true;
            }
            if ( ((int)Ruyi.SDK.GlobalInputDefine.Key.Up == msg.Triggers[i].Key && 1 == msg.Triggers[i].NewValue)
                || ((int)Ruyi.SDK.GlobalInputDefine.RuyiControllerKey.eButtonUp == msg.Triggers[i].Key || 1 == msg.Triggers[i].NewValue)
                || ((int)Ruyi.SDK.GlobalInputDefine.RuyiControllerKey.eAnalogLeftJoyY == msg.Triggers[i].Key || 1 == msg.Triggers[i].NewValue))
            {
                vertiacalAxis = 1;
                isMove = true;
            }
            if ( ((int)Ruyi.SDK.GlobalInputDefine.Key.Down == msg.Triggers[i].Key && 1 == msg.Triggers[i].NewValue)
                || ((int)Ruyi.SDK.GlobalInputDefine.RuyiControllerKey.eButtonDown == msg.Triggers[i].Key || 1 == msg.Triggers[i].NewValue)
                || ((int)Ruyi.SDK.GlobalInputDefine.RuyiControllerKey.eAnalogRightJoyY == msg.Triggers[i].Key || 1 == msg.Triggers[i].NewValue))
            {
                vertiacalAxis = -1;
                isMove = true;
            }

            if ( ((int)Ruyi.SDK.GlobalInputDefine.Key.E == msg.Triggers[i].Key && 1 == msg.Triggers[i].NewValue)
                || ((int)Ruyi.SDK.GlobalInputDefine.RuyiControllerKey.eButtonA == msg.Triggers[i].Key || 1 == msg.Triggers[i].NewValue))
            {
                isFire = true;
            }
        }
    }

    int horizontalAxis = 0;
    int vertiacalAxis = 0;
    bool isMove = false;
    bool isFire = false;
    private void RuyiInputValueListener()
    {
        if (isMove)
        {
            isMove = false;
        } else
        {
            horizontalAxis = 0;
            vertiacalAxis = 0;
        }

        if (isLocalPlayer)
        {
            Debug.Log("RuyiInputValueListener sssss horizontalAxis:" + horizontalAxis + " vertiacalAxis:" + vertiacalAxis);

            Vector3 movement = new Vector3(horizontalAxis, 0.0f, vertiacalAxis);
            GetComponent<Rigidbody>().velocity = movement * speed;

            GetComponent<Rigidbody>().position = new Vector3
            (
                Mathf.Clamp(GetComponent<Rigidbody>().position.x, boundary.xMin, boundary.xMax),
                0.0f,
                Mathf.Clamp(GetComponent<Rigidbody>().position.z, boundary.zMin, boundary.zMax)
            );

            GetComponent<Rigidbody>().rotation = Quaternion.Euler(0.0f, 0.0f, GetComponent<Rigidbody>().velocity.x * -tilt);
        }

        if (isFire)
        {
            isFire = false;
            CmdSpawnBullet();
        }
    }

    private void Update ()
	{
        if (isLocalPlayer)
        {
            if (Input.GetButton("Fire" + PlayerIndex) && 
                Time.time > nextFire)
            {
                nextFire = Time.time + fireRate;
                CmdSpawnBullet();
            }
        }
	}

	private void FixedUpdate ()
	{
        RuyiInputValueListener();
        /*
        if (isLocalPlayer)
        {
            float moveHorizontal = Input.GetAxis("Horizontal" + PlayerIndex);
            float moveVertical = Input.GetAxis("Vertical" + PlayerIndex);

            Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
            GetComponent<Rigidbody>().velocity = movement * speed;

            GetComponent<Rigidbody>().position = new Vector3
            (
                Mathf.Clamp(GetComponent<Rigidbody>().position.x, boundary.xMin, boundary.xMax),
                0.0f,
                Mathf.Clamp(GetComponent<Rigidbody>().position.z, boundary.zMin, boundary.zMax)
            );

            GetComponent<Rigidbody>().rotation = Quaternion.Euler(0.0f, 0.0f, GetComponent<Rigidbody>().velocity.x * -tilt);
        }*/
    }

    private void OnDestroy()
    {
        var gameController = FindObjectOfType<Done_GameController>();
        if (gameController != null)
        {
            gameController.UnregisterPlayer(this);
        }
    }

    private float nextFire;
}
