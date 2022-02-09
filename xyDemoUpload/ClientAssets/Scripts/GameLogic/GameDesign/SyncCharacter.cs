using proto.SyncMsg;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncCharacter : BaseCharacter
{
    private Vector3 lastSyncPos = Vector3.zero;
    private Vector3 lastSyncRot = Vector3.zero;
    private Vector3 forecastPos = Vector3.zero;
    private Vector3 forecastRot = Vector3.zero;
    private float lastSyncTime = 0;

    public override void Init(string prefabPath)
    {
        base.Init(prefabPath);

        rigidBody.constraints = RigidbodyConstraints.FreezeAll;
        rigidBody.useGravity = false;

        lastSyncPos = transform.position;
        lastSyncRot = transform.eulerAngles;
        forecastPos = transform.position;
        forecastRot = transform.eulerAngles;
        lastSyncTime = Time.time;
    }

    public void SyncPos(MsgSyncCharacter msgSyncCharacter)
    {
        Vector3 pos = new Vector3(msgSyncCharacter.x, msgSyncCharacter.y, msgSyncCharacter.z);
        Vector3 rot = new Vector3(msgSyncCharacter.ex, msgSyncCharacter.ey, msgSyncCharacter.ez);
        forecastPos = pos + (pos - lastSyncPos);
        forecastRot = rot + (rot - lastSyncRot);

        lastSyncPos = pos;
        lastSyncRot = rot;
        lastSyncTime = Time.time;
    }

    public void SyncFire(MsgFire msgFire)
    {
        Vector3 pos = new Vector3(msgFire.x, msgFire.y, msgFire.z);
        Vector3 rot = new Vector3(msgFire.ex, msgFire.ey, msgFire.ez);

        Bullet bullet = Fire();
        bullet.transform.position = pos;
        bullet.transform.eulerAngles = rot;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();

        ForecastUpdate();
    }

    private void ForecastUpdate()
    {
        float t = (Time.time - lastSyncTime) / CtrlCharacter.syncInterval;
        t = Mathf.Clamp(t, 0, 1);

        Vector3 pos = transform.position;
        pos = Vector3.Lerp(pos, forecastPos, t);
        transform.position = pos;

        Quaternion quat = transform.rotation;
        Quaternion forecastQuat = Quaternion.Euler(forecastRot);
        quat = Quaternion.Lerp(quat, forecastQuat, t);
        transform.rotation = quat;
    }
}
