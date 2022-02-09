using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameObject obj = new GameObject("xyTest");
        CtrlCharacter cc = obj.AddComponent<CtrlCharacter>();
        cc.Init("Prefab/" + "Ethan0");

        obj.AddComponent<CameraFollow>();

        //PanelManager.Init();
        //PanelManager.Open<MainPanel>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
