using proto.SystemMsg;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;

public static class NetManager
{
    public enum NetEvent
    {
        ConnectSuccess = 1,
        ConnectFailure = 2,
        Disconnect = 3,
    }

    public delegate void EventListener(string error);
    public delegate void MsgListener(IExtensible msgBase);

    private static Dictionary<NetEvent, EventListener> eventListeners = new Dictionary<NetEvent, EventListener>();
    private static Dictionary<string, MsgListener> msgListeners = new Dictionary<string, MsgListener>();

    private static List<IExtensible> msgList = new List<IExtensible>();
    private static int msgCount = 0;   // List.Length may cause thread conflict.
    private const int MAX_PROCESS_MESSAGE_COUNT = 10;

    private static Socket socket = null;
    private static ByteArray readBuf = null;
    private static Queue<ByteArray> writeQueue = null;
    private const int MIN_REMAIN_SIZE = 10;

    private static bool isConnecting = false;
    private static bool isDisconnecting = false;

    public static bool isUseHeartbeat = true;
    public static int pingInterval = 30;
    private static float lastPingTime = 0;
    private static float lastPongTime = 0;

    public static string GetLocalEndPointStr()
    {
        if (socket == null)
        {
            return "";
        }

        return socket.LocalEndPoint.ToString();
    }

    public static void AddEventListener(NetEvent netEvent, EventListener listener)
    {
        if (eventListeners.ContainsKey(netEvent))
        {
            eventListeners[netEvent] += listener;
        }
        else
        {
            eventListeners[netEvent] = listener;
        }
    }

    public static void AddMsgListener(string msgName, MsgListener listener)
    {
        if (msgListeners.ContainsKey(msgName))
        {
            msgListeners[msgName] += listener;
        }
        else
        {
            msgListeners[msgName] = listener;
        }
    }

    public static void RemoveEventListener(NetEvent netEvent, EventListener listener)
    {
        if (eventListeners.ContainsKey(netEvent))
        {
            eventListeners[netEvent] -= listener;

            if (eventListeners[netEvent] == null)
            {
                eventListeners.Remove(netEvent);
            }
        }
    }

    public static void RemoveMsgListener(string msgName, MsgListener listener)
    {
        if (msgListeners.ContainsKey(msgName))
        {
            msgListeners[msgName] -= listener;

            if (msgListeners[msgName] == null)
            {
                msgListeners.Remove(msgName);
            }
        }
    }

    private static void InvokeEvent(NetEvent netEvent, string error)
    {
        if (eventListeners.ContainsKey(netEvent))
        {
            eventListeners[netEvent](error);
        }
    }

    private static void InvokeMsg(string msgName, IExtensible msgBase)
    {
        if (msgListeners.ContainsKey(msgName))
        {
            msgListeners[msgName](msgBase);
        }
    }

    public static void Connect(string ip, int port)
    {
        if (socket != null && socket.Connected)
        {
            Debug.Log("Connect fail, already connected.");
            return;
        }

        if (isConnecting)
        {
            Debug.Log("Connect fail, is connecting.");
            return;
        }

        InitState();

        isConnecting = true;
        socket.BeginConnect(ip, port, ConnectCallback, socket);
    }

    private static void InitState()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.NoDelay = true;

        readBuf = new ByteArray();

        writeQueue = new Queue<ByteArray>();

        msgList = new List<IExtensible>();
        msgCount = 0;

        isConnecting = false;
        isDisconnecting = false;

        lastPingTime = Time.time;
        lastPongTime = Time.time;

        if (!msgListeners.ContainsKey("MsgPong"))
        {
            AddMsgListener("MsgPong", OnMsgPong);
        }
    }

    private static void OnMsgPong(IExtensible msgBase)
    {
        lastPongTime = Time.time;
    }

    private static void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            Socket socketObj = ar.AsyncState as Socket;
            socketObj.EndConnect(ar);
            Debug.Log("Socket connect success.");

            isConnecting = false;
            InvokeEvent(NetEvent.ConnectSuccess, "");

            socketObj.BeginReceive(readBuf.byteData, readBuf.writeIndex, readBuf.WriteableLength, SocketFlags.None, ReceiveCallback, socketObj);
        }
        catch (SocketException ex)
        {
            Debug.Log("Socket connect fail: " + ex.ToString());

            isConnecting = false;
            InvokeEvent(NetEvent.ConnectFailure, ex.ToString());
        }
        //finally
        //{
        //    isConnecting = false;
        //}
    }

    public static void Disconnect()
    {
        if (socket == null || !socket.Connected)
        {
            return;
        }

        if (isConnecting)
        {
            return;
        }

        if (writeQueue.Count > 0)
        {
            isDisconnecting = true;
        }
        else
        {
            socket.Close();
            InvokeEvent(NetEvent.Disconnect, "");
        }
    }

    public static void Send(IExtensible msgBase)
    {
        Debug.Log("NetManager.Send");

        if (socket == null || !socket.Connected)
        {
            return;
        }

        if (isConnecting)
        {
            return;
        }

        if (isDisconnecting)
        {
            return;
        }

        Debug.Log("NetManager.Send not return");

        byte[] nameBytes = MsgManager.EncodeProtocolName(msgBase);
        byte[] bodyBytes = MsgManager.EncodeProtocolBody(msgBase);
        int length = nameBytes.Length + bodyBytes.Length;

        Debug.Log("Encode done");

        byte[] sendBytes = new byte[2 + length];
        sendBytes[0] = (byte)(length % 256);
        sendBytes[1] = (byte)(length / 256);
        Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
        Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);

        Debug.Log("Copy done");

        ByteArray byteArray = new ByteArray(sendBytes);
        int count = 0;
        lock (writeQueue)
        {
            writeQueue.Enqueue(byteArray);
            count = writeQueue.Count;
        }

        Debug.LogFormat("Enqueue done, Queue.Count: {0}", count);

        if (count == 1)
        {
            Debug.Log("NetManager.BeginSend");
            socket.BeginSend(sendBytes, 0, sendBytes.Length, SocketFlags.None, SendCallback, socket);
        }
    }

    public static void SendCallback(IAsyncResult ar)
    {
        Debug.Log("NetManager.SendCallback");

        Socket socketObj = ar.AsyncState as Socket;
        if (socketObj == null || !socketObj.Connected)
        {
            return;
        }

        int count = socketObj.EndSend(ar);

        ByteArray byteArray;
        lock (writeQueue)
        {
            if (writeQueue.Count >= 1)
            {
                byteArray = writeQueue.First();
            }
            else
            {
                byteArray = null;
            }
        }

        byteArray.readIndex += count;
        if (byteArray.ReadableLength == 0)
        {
            lock (writeQueue)
            {
                writeQueue.Dequeue();
                if (writeQueue.Count >= 1)
                {
                    byteArray = writeQueue.First();
                }
                else
                {
                    byteArray = null;
                }
            }
        }

        if (byteArray != null)
        {
            socketObj.BeginSend(byteArray.byteData, byteArray.readIndex, byteArray.ReadableLength, SocketFlags.None, SendCallback, socketObj);
        }
        else if (isDisconnecting)
        {
            socketObj.Close();
        }
    }

    public static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            Socket socketObj = ar.AsyncState as Socket;
            int count = socketObj.EndReceive(ar);

            // received FIN signal
            if (count == 0)
            {
                Disconnect();
                return;
            }

            readBuf.writeIndex += count;

            OnReceiveData();

            readBuf.CheckAndMoveByteData();
            if (readBuf.WriteableLength < MIN_REMAIN_SIZE)
            {
                readBuf.Resize(readBuf.ReadableLength * 2);
            }

            socketObj.BeginReceive(readBuf.byteData, readBuf.writeIndex, readBuf.WriteableLength, SocketFlags.None, ReceiveCallback, socketObj);
        }
        catch (SocketException ex)
        {
            Debug.Log("Socket receive fail: " + ex.ToString());
        }
    }

    public static void OnReceiveData()
    {
        if (readBuf.ReadableLength <= 2)
        {
            return;
        }

        int readIdx = readBuf.readIndex;
        byte[] bytes = readBuf.byteData;
        Int16 bodyLength = (Int16)((bytes[readIdx + 1] << 8) | bytes[readIdx]);
        if (readBuf.ReadableLength < bodyLength)
        {
            return;
        }

        readBuf.readIndex += 2;

        int nameCount = 0;
        string protocolName = MsgManager.DecodeProtocolName(readBuf.byteData, readBuf.readIndex, out nameCount);
        if (protocolName == "")
        {
            Debug.Log("OnReceiveData MsgManager.DecodeProtocolName fail.");
            return;
        }

        readBuf.readIndex += nameCount;

        int bodyCount = bodyLength - nameCount;
        IExtensible msgBase = MsgManager.DecodeProtocolBody(protocolName, readBuf.byteData, readBuf.readIndex, bodyCount);

        readBuf.readIndex += bodyCount;
        readBuf.CheckAndMoveByteData();

        lock (msgList)
        {
            msgList.Add(msgBase);
        }
        ++msgCount;

        if (readBuf.ReadableLength > 2)
        {
            OnReceiveData();
        }
    }

    public static void Update()
    {
        MsgUpdate();
        PingUpdate();
    }

    public static void MsgUpdate()
    {
        if (msgCount == 0)
        {
            return;
        }

        for (int i = 0; i < MAX_PROCESS_MESSAGE_COUNT; ++i)
        {
            IExtensible msgBase = null;
            lock (msgList)
            {
                if (msgList.Count > 0)
                {
                    msgBase = msgList[0];
                    msgList.RemoveAt(0);
                    --msgCount;
                }
            }

            if (msgBase != null)
            {
                InvokeMsg(MsgManager.GetProtocolName(msgBase).Split('.').Last(), msgBase);
            }
            else
            {
                break;
            }
        }
    }

    private static void PingUpdate()
    {
        if (!isUseHeartbeat)
        {
            return;
        }

        if (Time.time - lastPingTime > pingInterval)
        {
            MsgPing msgPing = new MsgPing();
            Send(msgPing);
            lastPingTime = Time.time;
        }

        if (Time.time - lastPongTime > pingInterval * 4)
        {
            Disconnect();
        }
    }
}
