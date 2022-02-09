using proto.SyncMsg;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 100;
    public BaseCharacter character = null;

    private GameObject prefabIns = null;
    private Rigidbody rigidbody = null;

    public void Init()
    {
        GameObject prefab = ResourceManager.LoadPrefab("Prefab/" + "Bullet");
        prefabIns = Instantiate<GameObject>(prefab);
        prefabIns.transform.parent = this.transform;
        prefabIns.transform.localPosition = Vector3.zero;
        prefabIns.transform.localEulerAngles = Vector3.zero;

        rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject collisionObj = collision.gameObject;
        BaseCharacter hitCharacter = collisionObj.GetComponent<BaseCharacter>();

        if (hitCharacter == character)
        {
            return;
        }

        if (hitCharacter != null)
        {
            SendMsgHit(character, hitCharacter);
        }

        Destroy(gameObject);
    }

    private void SendMsgHit(BaseCharacter character, BaseCharacter hitCharacter)
    {
        if (hitCharacter == null || character == null)
        {
            return;
        }

        if (character.id != GameMain.id)
        {
            return;
        }

        MsgHit msgHit = new MsgHit();
        msgHit.targetId = hitCharacter.id;
        msgHit.id = character.id;
        msgHit.x = transform.position.x;
        msgHit.y = transform.position.y;
        msgHit.z = transform.position.z;
        NetManager.Send(msgHit);
    }
}
