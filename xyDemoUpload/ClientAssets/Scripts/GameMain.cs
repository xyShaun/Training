using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMain : MonoBehaviour
{
    public static string id = "";
    public static string ip = "127.0.0.1";
    public static int port = 8888;

    // Start is called before the first frame update
    void Start()
    {
        NetManager.AddEventListener(NetManager.NetEvent.Disconnect, OnDisconnect);

        PanelManager.Init();
        BattleManager.Init();

        PanelManager.Open<MainPanel>();
    }

    private void OnDisconnect(string error)
    {
        Debug.Log("OnDisconnect: " + error);
    }

    // Update is called once per frame
    void Update()
    {
        NetManager.Update();
    }
}
