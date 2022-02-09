using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Camera camera = null;
    public Vector3 distance = new Vector3(0, 3, -6);
    public Vector3 offset = new Vector3(0, 3, 0);
    public float speed = 10;

    // Start is called before the first frame update
    void Start()
    {
        camera = Camera.main;

        Vector3 pos = transform.position;
        Vector3 forward = transform.forward;
        Vector3 initPos = pos - forward * 10 + Vector3.up * 5;
        camera.transform.position = initPos;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        Vector3 pos = transform.position;
        Vector3 forward = transform.forward;
        Vector3 targetCameraPos = pos + forward * distance.z;
        targetCameraPos.y += distance.y;

        Vector3 curCameraPos = camera.transform.position;
        camera.transform.position = Vector3.MoveTowards(curCameraPos, targetCameraPos, speed * Time.deltaTime);
        camera.transform.LookAt(pos + offset);
    }
}
