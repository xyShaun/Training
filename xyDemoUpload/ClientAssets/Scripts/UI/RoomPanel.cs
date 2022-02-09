using proto.RoomMsg;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomPanel : BasePanel
{
    private Button startBattleBtn = null;
    private Button exitRoomBtn = null;
    private Transform content = null;
    private GameObject playerObj = null;

    public override void OnInit()
    {
        prefabPath = "Prefab/" + "RoomPanel";
        layer = PanelManager.Layer.Panel;
    }

    public override void OnShow(params object[] objects)
    {
        startBattleBtn = prefabIns.transform.Find("OperationPanel/StartBattleBtn").GetComponent<Button>();
        exitRoomBtn = prefabIns.transform.Find("OperationPanel/ExitRoomBtn").GetComponent<Button>();
        content = prefabIns.transform.Find("ListPanel/Scroll View/Viewport/Content");
        playerObj = prefabIns.transform.Find("Player").gameObject;

        playerObj.SetActive(false);

        startBattleBtn.onClick.AddListener(OnStartBattleBtnClick);
        exitRoomBtn.onClick.AddListener(OnExitRoomBtnClick);

        NetManager.AddMsgListener("MsgGetRoomInfo", OnMsgGetRoomInfo);
        NetManager.AddMsgListener("MsgLeaveRoom", OnMsgLeaveRoom);
        NetManager.AddMsgListener("MsgStartBattle", OnMsgStartBattle);

        MsgGetRoomInfo msgGetRoomInfo = new MsgGetRoomInfo();
        NetManager.Send(msgGetRoomInfo);
    }

    private void OnMsgStartBattle(IExtensible msgBase)
    {
        MsgStartBattle msgStartBattle = msgBase as MsgStartBattle;
        if (msgStartBattle.result == 0)
        {
            Close();
        }
        else
        {
            PanelManager.Open<TipPanel>("Start game fail.");
        }
    }

    private void OnStartBattleBtnClick()
    {
        MsgStartBattle msgStartBattle = new MsgStartBattle();
        NetManager.Send(msgStartBattle);
    }

    private void OnMsgLeaveRoom(IExtensible msgBase)
    {
        MsgLeaveRoom msgLeaveRoom = msgBase as MsgLeaveRoom;
        if (msgLeaveRoom.result == 0)
        {
            PanelManager.Open<TipPanel>("Exit room.");
            PanelManager.Open<RoomListPanel>();
            Close();
        }
        else
        {
            PanelManager.Open<TipPanel>("Exit room fail.");
        }
    }

    private void OnExitRoomBtnClick()
    {
        MsgLeaveRoom msgLeaveRoom = new MsgLeaveRoom();
        NetManager.Send(msgLeaveRoom);
    }

    public override void OnClose()
    {
        NetManager.RemoveMsgListener("MsgGetRoomInfo", OnMsgGetRoomInfo);
        NetManager.RemoveMsgListener("MsgLeaveRoom", OnMsgLeaveRoom);
        NetManager.RemoveMsgListener("MsgStartBattle", OnMsgStartBattle);
    }

    private void OnMsgGetRoomInfo(IExtensible msgBase)
    {
        MsgGetRoomInfo msgGetRoomInfo = msgBase as MsgGetRoomInfo;

        for (int i = content.childCount - 1; i >= 0; --i)
        {
            GameObject go = content.GetChild(i).gameObject;
            Destroy(go);
        }

        if (msgGetRoomInfo.players == null)
        {
            return;
        }

        for (int i = 0; i < msgGetRoomInfo.players.Count; ++i)
        {
            GeneratePlayerItem(msgGetRoomInfo.players[i]);
        }
    }

    private void GeneratePlayerItem(PlayerInfo playerInfo)
    {
        GameObject go = Instantiate<GameObject>(playerObj);
        go.transform.SetParent(content);
        go.SetActive(true);
        go.transform.localScale = Vector3.one;

        Transform t = go.transform;
        Text idText = t.Find("IdText").GetComponent<Text>();
        Text campText = t.Find("CampText").GetComponent<Text>();

        idText.text = playerInfo.id;
        if (playerInfo.camp == 0)
        {
            campText.text = "T";
            campText.color = Color.red;
        }
        else
        {
            campText.text = "M";
            campText.color = Color.blue;
        }

        if (playerInfo.isHouseOwner)
        {
            campText.text += "!";
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
