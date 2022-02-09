using Server.GameLogic.GameDesign;
using Server.Network.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.GameLogic.Handler
{
    public partial class NetEventHandler
    {
        public static void OnDisconnect(ClientState cs)
        {
            Console.WriteLine("OnDisconnect");

            Player player = cs.player as Player;
            if (player != null)
            {
                int roomId = player.roomId;
                if (roomId >= 0)
                {
                    Room room = RoomManager.GetRoom(roomId);
                    room.RemovePlayer(player.id);
                }

                PlayerManager.RemovePlayer(player.id);
            }
        }

        public static void OnTimer()
        {
            CheckHeartbeat();
            RoomManager.Update();
        }

        private static void CheckHeartbeat()
        {
            if (!NetManager.isUseHeartbeat)
            {
                return;
            }

            long nowTimeStamp = NetManager.GetTimeStamp();
            List<ClientState> clientListToDisconnect = new List<ClientState>();

            foreach (ClientState cs in NetManager.clients.Values)
            {
                if (nowTimeStamp - cs.lastPingTime > NetManager.pingInterval * 4)
                {
                    clientListToDisconnect.Add(cs);
                }
            }

            foreach (ClientState cs in clientListToDisconnect)
            {
                Console.WriteLine("Heartbeat disconnect: " + cs.socket.RemoteEndPoint.ToString());
                NetManager.Disconnect(cs);
            }
        }
    }
}
