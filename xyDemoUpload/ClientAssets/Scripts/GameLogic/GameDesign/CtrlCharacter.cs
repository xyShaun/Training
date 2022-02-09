using proto.SyncMsg;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CtrlCharacter : BaseCharacter
{
    public static float syncInterval = 0.1f;

    private float lastSyncTime = 0;

    // Start is called before the first frame update
    void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();

        MoveUpdate();
        FireUpdate();
        SyncUpdate();
    }

    private void SyncUpdate()
    {
        if (Time.time - lastSyncTime < syncInterval)
        {
            return;
        }

        lastSyncTime = Time.time;

        MsgSyncCharacter msgSyncCharacter = new MsgSyncCharacter();
        msgSyncCharacter.x = transform.position.x;
        msgSyncCharacter.y = transform.position.y;
        msgSyncCharacter.z = transform.position.z;
        msgSyncCharacter.ex = transform.eulerAngles.x;
        msgSyncCharacter.ey = transform.eulerAngles.y;
        msgSyncCharacter.ez = transform.eulerAngles.z;
        NetManager.Send(msgSyncCharacter);
    }

    private void FireUpdate()
    {
        if (IsDie())
        {
            return;
        }

        if (!Input.GetMouseButton(0))
        {
            return;
        }

        if (Time.time - lastFireTime < fireCd)
        {
            return;
        }

        Bullet bullet = Fire();

        MsgFire msgFire = new MsgFire();
        msgFire.x = bullet.transform.position.x;
        msgFire.y = bullet.transform.position.y;
        msgFire.z = bullet.transform.position.z;
        msgFire.ex = bullet.transform.eulerAngles.x;
        msgFire.ey = bullet.transform.eulerAngles.y;
        msgFire.ez = bullet.transform.eulerAngles.z;
        NetManager.Send(msgFire);
    }

    private void MoveUpdate()
    {
        float h = Input.GetAxis("Horizontal");
        transform.Rotate(0, h * Time.deltaTime * 300, 0);

        float v = Input.GetAxis("Vertical");
        Vector3 deltaPos = v * Time.deltaTime * speed * transform.forward;
        transform.position += deltaPos;

        //if (v > 0)
        //{
        //    animator.SetBool("isMoving", true);
        //}
        //else
        //{
        //    animator.SetBool("isMoving", false);
        //}
    }
}
