using proto.LoginMsg;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MainPanel : BasePanel
{
    private Button startGameBtn = null;
    private Button optionBtn = null;
    private Button exitGameBtn = null;

    private bool isShowConnectFailPanel = false;

    public override void OnInit()
    {
        prefabPath = "Prefab/" + "MainPanel";
        layer = PanelManager.Layer.Panel;
    }

    public override void OnShow(params object[] objects)
    {
        startGameBtn = prefabIns.transform.Find("StartGameBtn").GetComponent<Button>();
        optionBtn = prefabIns.transform.Find("OptionBtn").GetComponent<Button>();
        exitGameBtn = prefabIns.transform.Find("ExitGameBtn").GetComponent<Button>();

        startGameBtn.onClick.AddListener(OnStartGameBtnClick);
        optionBtn.onClick.AddListener(OnOptionBtnClick);
        exitGameBtn.onClick.AddListener(OnExitGameBtnClick);

        NetManager.AddMsgListener("MsgLogin", OnMsgLogin);
        NetManager.AddEventListener(NetManager.NetEvent.ConnectSuccess, OnConnectSuccess);
        NetManager.AddEventListener(NetManager.NetEvent.ConnectFailure, OnConnectFailure);
    }

    private void OnOptionBtnClick()
    {
        PanelManager.Open<OptionPanel>();
        Close();
    }

    private void OnExitGameBtnClick()
    {
        Application.Quit();
    }

    private void OnMsgLogin(IExtensible msgBase)
    {
        MsgLogin msgLogin = msgBase as MsgLogin;
        if (msgLogin.isLoginSuccess)
        {
            Debug.Log("Login success.");

            GameMain.id = msgLogin.id;

            PanelManager.Open<RoomListPanel>();
            Close();
        }
        else
        {
            PanelManager.Open<TipPanel>("Login fail.");
        }
    }

    private void OnStartGameBtnClick()
    {
        NetManager.Connect(GameMain.ip, GameMain.port);
    }

    public override void OnClose()
    {
        NetManager.RemoveMsgListener("MsgLogin", OnMsgLogin);
        NetManager.RemoveEventListener(NetManager.NetEvent.ConnectSuccess, OnConnectSuccess);
        NetManager.RemoveEventListener(NetManager.NetEvent.ConnectFailure, OnConnectFailure);
    }

    private void OnConnectFailure(string error)
    {
        Debug.Log("OnConnectFailure");

        //PanelManager.Open<TipPanel>(error);
        isShowConnectFailPanel = true;
    }

    private void OnConnectSuccess(string error)
    {
        Debug.Log("OnConnectSuccess");

        MsgLogin msgLogin = new MsgLogin();
        //msgLogin.id = Environment.UserName;
        //msgLogin.id = NetManager.GetLocalEndPointStr();
        msgLogin.id = Environment.UserName + NetManager.GetLocalEndPointStr().Split(':').Last();
        NetManager.Send(msgLogin);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isShowConnectFailPanel)
        {
            isShowConnectFailPanel = false;
            PanelManager.Open<TipPanel>("Connect fail.");
        }
    }
}
