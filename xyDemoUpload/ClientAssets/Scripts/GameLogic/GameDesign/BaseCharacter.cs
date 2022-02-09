using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseCharacter : MonoBehaviour
{
    public int hp = 100;
    public string id = "";
    public int camp = -1;

    public float speed = 5.0f;
    public Transform firePoint = null;
    public float lastFireTime = 0;
    public float fireCd = 0.2f;

    protected Rigidbody rigidBody = null;
    protected BoxCollider collider = null;
    protected Animator animator = null;

    private GameObject prefabIns = null;

    public void WasAttacked(int damage)
    {
        if (IsDie())
        {
            return;
        }

        hp -= damage;

        if (IsDie())
        {
            gameObject.SetActive(false);
        }
    }

    // Start is called before the first frame update
    protected void Start()
    {
        
    }

    // Update is called once per frame
    protected void Update()
    {
        
    }

    public virtual void Init(string prefabPath)
    {
        GameObject prefab = ResourceManager.LoadPrefab(prefabPath);
        prefabIns = Instantiate<GameObject>(prefab);
        prefabIns.transform.parent = this.transform;
        prefabIns.transform.localPosition = Vector3.zero;
        prefabIns.transform.localEulerAngles = Vector3.zero;

        rigidBody = gameObject.AddComponent<Rigidbody>();
        collider = gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 0.8f, 0);
        collider.size = new Vector3(0.6f, 1.6f, 0.6f);

        animator = prefabIns.GetComponent<Animator>();

        firePoint = prefabIns.transform.Find("FirePoint");
    }

    public bool IsDie()
    {
        return hp <= 0;
    }

    public Bullet Fire()
    {
        if (IsDie())
        {
            return null;
        }

        GameObject bulletObj = new GameObject("Bullet");
        Bullet bullet = bulletObj.AddComponent<Bullet>();
        bullet.Init();
        bullet.character = this;
        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = firePoint.rotation;

        lastFireTime = Time.time;

        return bullet;
    }
}
