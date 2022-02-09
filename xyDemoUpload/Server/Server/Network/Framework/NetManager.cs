using ProtoBuf;
using Server.GameLogic.Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Server.Network.Framework
{
    class NetManager
    {
        public static Socket listenSocket = null;
        public static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();
        private static List<Socket> checkReadList = new List<Socket>();

        public static bool isUseHeartbeat = true;
        public static long pingInterval = 30;

        private const int MIN_REMAIN_SIZE = 10;

        public static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }

        public static void StartListen(int listenPort)
        {
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress ipAddr = IPAddress.Parse("0.0.0.0");
            IPEndPoint ipEp = new IPEndPoint(ipAddr, listenPort);
            listenSocket.Bind(ipEp);

            listenSocket.Listen(0);
            Console.WriteLine("Server start up successfully.");

            while (true)
            {
                ResetCheckReadList();
                Socket.Select(checkReadList, null, null, 1000);

                for (int i = 0; i < checkReadList.Count; ++i)
                {
                    Socket s = checkReadList[i];
                    if (s == listenSocket)
                    {
                        ReadListenSocket(s);
                    }
                    else
                    {
                        ReadClientSocket(s);
                    }
                }

                Timeout();
            }
        }

        private static void ReadListenSocket(Socket listenSocket)
        {
            try
            {
                Socket clientSocket = listenSocket.Accept();
                Console.WriteLine("Accept " + clientSocket.RemoteEndPoint.ToString());

                ClientState state = new ClientState();
                state.socket = clientSocket;
                state.lastPingTime = GetTimeStamp();
                clients.Add(clientSocket, state);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Accept fail: " + ex.ToString());
            }
        }

        private static void Timeout()
        {
            MethodInfo mi = typeof(NetEventHandler).GetMethod("OnTimer");
            object[] oa = { };
            mi.Invoke(null, oa);
        }

        private static void ReadClientSocket(Socket clientSocket)
        {
            ClientState state = clients[clientSocket];
            ByteArray readBuf = state.readBuf;

            int count = 0;
            try
            {
                count = clientSocket.Receive(readBuf.byteData, readBuf.writeIndex, readBuf.WriteableLength, SocketFlags.None);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Receive socket exception: " + ex.ToString());
                Disconnect(state);

                return;
            }

            if (count == 0)
            {
                Console.WriteLine("Socket close: " + clientSocket.RemoteEndPoint.ToString());
                Disconnect(state);

                return;
            }

            readBuf.writeIndex += count;

            OnReceiveData(state);

            readBuf.CheckAndMoveByteData();
            if (readBuf.WriteableLength < MIN_REMAIN_SIZE)
            {
                readBuf.Resize(readBuf.ReadableLength * 2);
            }
        }

        private static void OnReceiveData(ClientState state)
        {
            ByteArray readBuf = state.readBuf;
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
                Console.WriteLine("OnReceiveData MsgManager.DecodeProtocolName fail.");
                Disconnect(state);

                return;
            }

            readBuf.readIndex += nameCount;

            int bodyCount = bodyLength - nameCount;
            IExtensible msgBase = MsgManager.DecodeProtocolBody(protocolName, readBuf.byteData, readBuf.readIndex, bodyCount);

            readBuf.readIndex += bodyCount;
            readBuf.CheckAndMoveByteData();

            string methodName = "On" + protocolName.Split('.').Last();
            MethodInfo mi = typeof(MsgHandler).GetMethod(methodName);
            object[] oa = { state, msgBase };
            Console.WriteLine("Receive: " + protocolName);
            if (mi != null)
            {
                mi.Invoke(null, oa);
            }
            else
            {
                Console.WriteLine("OnReceiveData invoke fail: " + protocolName);
            }

            if (readBuf.ReadableLength > 2)
            {
                OnReceiveData(state);
            }
        }

        public static void Disconnect(ClientState state)
        {
            MethodInfo mi = typeof(NetEventHandler).GetMethod("OnDisconnect");
            object[] oa = { state };
            mi.Invoke(null, oa);

            state.socket.Close();
            clients.Remove(state.socket);
        }

        private static void ResetCheckReadList()
        {
            checkReadList.Clear();
            checkReadList.Add(listenSocket);

            foreach (Socket s in clients.Keys)
            {
                checkReadList.Add(s);
            }
        }

        public static void Send(ClientState cs, IExtensible msgBase)
        {
            if (cs == null)
            {
                return;
            }

            if (!cs.socket.Connected)
            {
                return;
            }

            byte[] nameBytes = MsgManager.EncodeProtocolName(msgBase);
            byte[] bodyBytes = MsgManager.EncodeProtocolBody(msgBase);
            int length = nameBytes.Length + bodyBytes.Length;

            byte[] sendBytes = new byte[2 + length];
            sendBytes[0] = (byte)(length % 256);
            sendBytes[1] = (byte)(length / 256);
            Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
            Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);

            try
            {
                cs.socket.BeginSend(sendBytes, 0, sendBytes.Length, SocketFlags.None, null, null);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("BeginSend fail: " + ex.ToString());
            }
        }
    }
}
