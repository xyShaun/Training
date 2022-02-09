using proto.RoomMsg;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomListPanel : BasePanel
{
    private Text idText = null;
    private Button createRoomBtn = null;
    private Button refreshListBtn = null;
    private Button backBtn = null;
    private Transform content = null;
    private GameObject roomObj = null;

    public override void OnInit()
    {
        prefabPath = "Prefab/" + "RoomListPanel";
        layer = PanelManager.Layer.Panel;
    }

    public override void OnShow(params object[] objects)
    {
        idText = prefabIns.transform.Find("InfoPanel/IdText").GetComponent<Text>();
        createRoomBtn = prefabIns.transform.Find("OperationPanel/CreateRoomBtn").GetComponent<Button>();
        refreshListBtn = prefabIns.transform.Find("OperationPanel/RefreshListBtn").GetComponent<Button>();
        backBtn = prefabIns.transform.Find("OperationPanel/BackBtn").GetComponent<Button>();
        content = prefabIns.transform.Find("ListPanel/Scroll View/Viewport/Content");
        roomObj = prefabIns.transform.Find("Room").gameObject;

        createRoomBtn.onClick.AddListener(OnCreateRoomBtnClick);
        refreshListBtn.onClick.AddListener(OnRefreshListBtnClick);
        backBtn.onClick.AddListener(OnBackBtnClick);

        roomObj.SetActive(false);

        idText.text = GameMain.id;

        NetManager.AddMsgListener("MsgGetRoomList", OnMsgGetRoomList);
        NetManager.AddMsgListener("MsgCreateRoom", OnMsgCreateRoom);
        NetManager.AddMsgListener("MsgEnterRoom", OnMsgEnterRoom);

        MsgGetRoomList msgGetRoomList = new MsgGetRoomList();
        NetManager.Send(msgGetRoomList);
    }

    private void OnBackBtnClick()
    {
        NetManager.Disconnect();

        PanelManager.Open<MainPanel>();
        Close();
    }

    private void OnRefreshListBtnClick()
    {
        MsgGetRoomList msgGetRoomList = new MsgGetRoomList();
        NetManager.Send(msgGetRoomList);
    }

    private void OnCreateRoomBtnClick()
    {
        MsgCreateRoom msgCreateRoom = new MsgCreateRoom();
        NetManager.Send(msgCreateRoom);
    }

    private void OnMsgGetRoomList(IExtensible msgBase)
    {
        MsgGetRoomList msgGetRoomList = msgBase as MsgGetRoomList;

        for (int i = content.childCount - 1; i >= 0; --i)
        {
            GameObject go = content.GetChild(i).gameObject;
            Destroy(go);
        }

        if (msgGetRoomList.rooms == null)
        {
            return;
        }

        for (int i = 0; i < msgGetRoomList.rooms.Count; ++i)
        {
            GenerateRoomItem(msgGetRoomList.rooms[i]);
        }
    }

    private void GenerateRoomItem(RoomInfo roomInfo)
    {
        GameObject go = Instantiate<GameObject>(roomObj);
        go.transform.SetParent(content);
        go.SetActive(true);
        go.transform.localScale = Vector3.one;

        Transform t = go.transform;
        Text idText = t.Find("IdText").GetComponent<Text>();
        Text countText = t.Find("CountText").GetComponent<Text>();
        Text statusText = t.Find("StatusText").GetComponent<Text>();
        Button joinBtn = t.Find("JoinBtn").GetComponent<Button>();

        idText.text = roomInfo.id.ToString();
        countText.text = roomInfo.count.ToString();
        if (roomInfo.status == 0)
        {
            statusText.text = "Preparing";
        }
        else
        {
            statusText.text = "In Battle";
        }

        joinBtn.name = idText.text;
        joinBtn.onClick.AddListener(delegate () { OnJoinBtnClick(joinBtn.name); });
    }

    private void OnJoinBtnClick(string roomId)
    {
        MsgEnterRoom msgEnterRoom = new MsgEnterRoom();
        msgEnterRoom.id = int.Parse(roomId);
        NetManager.Send(msgEnterRoom);
    }

    public override void OnClose()
    {
        NetManager.RemoveMsgListener("MsgGetRoomList", OnMsgGetRoomList);
        NetManager.RemoveMsgListener("MsgCreateRoom", OnMsgCreateRoom);
        NetManager.RemoveMsgListener("MsgEnterRoom", OnMsgEnterRoom);
    }

    private void OnMsgCreateRoom(IExtensible msgBase)
    {
        MsgCreateRoom msgCreateRoom = msgBase as MsgCreateRoom;
        if (msgCreateRoom.result == 0)
        {
            PanelManager.Open<TipPanel>("Create room successfully.");
            PanelManager.Open<RoomPanel>();
            Close();
        }
        else
        {
            PanelManager.Open<TipPanel>("Create room fail.");
        }
    }

    private void OnMsgEnterRoom(IExtensible msgBase)
    {
        MsgEnterRoom msgEnterRoom = msgBase as MsgEnterRoom;

        if (msgEnterRoom.result == 0)
        {
            PanelManager.Open<RoomPanel>();
            Close();
        }
        else
        {
            PanelManager.Open<TipPanel>("Enter room fail.");
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
