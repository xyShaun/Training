using proto.RoomMsg;
using ProtoBuf;
using Server.GameLogic.GameDesign;
using Server.Network.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.GameLogic.Handler
{
    public partial class MsgHandler
    {
        public static void OnMsgGetRoomList(ClientState cs, IExtensible msgBase)
        {
            MsgGetRoomList msgGetRoomList = msgBase as MsgGetRoomList;
            Player player = cs.player as Player;
            if (player == null)
            {
                return;
            }

            player.Send(RoomManager.ToMsg());
        }

        public static void OnMsgCreateRoom(ClientState cs, IExtensible msgBase)
        {
            MsgCreateRoom msgCreateRoom = msgBase as MsgCreateRoom;
            Player player = cs.player as Player;
            if (player == null)
            {
                return;
            }

            if (player.roomId >= 0)
            {
                msgCreateRoom.result = 1;
                player.Send(msgCreateRoom);

                return;
            }

            Room room = RoomManager.AddRoom();
            room.AddPlayer(player.id);

            msgCreateRoom.result = 0;
            player.Send(msgCreateRoom);
        }

        public static void OnMsgEnterRoom(ClientState cs, IExtensible msgBase)
        {
            MsgEnterRoom msgEnterRoom = msgBase as MsgEnterRoom;
            Player player = cs.player as Player;
            if (player == null)
            {
                return;
            }

            if (player.roomId >= 0)
            {
                msgEnterRoom.result = 1;
                player.Send(msgEnterRoom);

                return;
            }

            Room room = RoomManager.GetRoom(msgEnterRoom.id);
            if (room == null)
            {
                msgEnterRoom.result = 1;
                player.Send(msgEnterRoom);

                return;
            }

            if (!room.AddPlayer(player.id))
            {
                msgEnterRoom.result = 1;
                player.Send(msgEnterRoom);

                return;
            }

            msgEnterRoom.result = 0;
            player.Send(msgEnterRoom);
        }

        public static void OnMsgGetRoomInfo(ClientState cs, IExtensible msgBase)
        {
            MsgGetRoomInfo msgGetRoomInfo = msgBase as MsgGetRoomInfo;
            Player player = cs.player as Player;
            if (player == null)
            {
                return;
            }

            Room room = RoomManager.GetRoom(player.roomId);
            if (room == null)
            {
                player.Send(msgGetRoomInfo);
                return;
            }

            player.Send(room.ToMsg());
        }

        public static void OnMsgLeaveRoom(ClientState cs, IExtensible msgBase)
        {
            MsgLeaveRoom msgLeaveRoom = msgBase as MsgLeaveRoom;
            Player player = cs.player as Player;
            if (player == null)
            {
                return;
            }

            Room room = RoomManager.GetRoom(player.roomId);
            if (room == null)
            {
                msgLeaveRoom.result = 1;
                player.Send(msgLeaveRoom);
                return;
            }

            room.RemovePlayer(player.id);

            msgLeaveRoom.result = 0;
            player.Send(msgLeaveRoom);
        }

        public static void OnMsgStartBattle(ClientState cs, IExtensible msgBase)
        {
            MsgStartBattle msgStartBattle = msgBase as MsgStartBattle;

            Player player = cs.player as Player;
            if (player == null)
            {
                return;
            }

            Room room = RoomManager.GetRoom(player.roomId);
            if (room == null)
            {
                msgStartBattle.result = 1;
                player.Send(msgStartBattle);

                return;
            }

            if (!room.IsHouseOwner(player))
            {
                msgStartBattle.result = 1;
                player.Send(msgStartBattle);

                return;
            }

            if (!room.StartBattle())
            {
                msgStartBattle.result = 1;
                player.Send(msgStartBattle);

                return;
            }

            msgStartBattle.result = 0;
            player.Send(msgStartBattle);
        }
    }
}
