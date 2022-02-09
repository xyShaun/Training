using proto.SyncMsg;
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
        public static void OnMsgSyncCharacter(ClientState cs, IExtensible msgBase)
        {
            MsgSyncCharacter msgSyncCharacter = msgBase as MsgSyncCharacter;
            Player player = cs.player as Player;
            if (player == null)
            {
                return;
            }

            Room room = RoomManager.GetRoom(player.roomId);
            if (room == null)
            {
                return;
            }

            if (room.status != Room.Status.INBATTLE)
            {
                return;
            }

            player.x = msgSyncCharacter.x;
            player.y = msgSyncCharacter.y;
            player.z = msgSyncCharacter.z;
            player.ex = msgSyncCharacter.ex;
            player.ey = msgSyncCharacter.ey;
            player.ez = msgSyncCharacter.ez;

            msgSyncCharacter.id = player.id;
            room.Broadcast(msgSyncCharacter);
        }

        public static void OnMsgFire(ClientState cs, IExtensible msgBase)
        {
            MsgFire msgFire = msgBase as MsgFire;
            Player player = cs.player as Player;
            if (player == null)
            {
                return;
            }

            Room room = RoomManager.GetRoom(player.roomId);
            if (room == null)
            {
                return;
            }

            if (room.status != Room.Status.INBATTLE)
            {
                return;
            }

            msgFire.id = player.id;
            room.Broadcast(msgFire);
        }

        public static void OnMsgHit(ClientState cs, IExtensible msgBase)
        {
            MsgHit msgHit = msgBase as MsgHit;
            Player player = cs.player as Player;
            if (player == null)
            {
                return;
            }

            Player targetPlayer = PlayerManager.GetPlayer(msgHit.targetId);
            if (targetPlayer == null)
            {
                return;
            }

            Room room = RoomManager.GetRoom(player.roomId);
            if (room == null)
            {
                return;
            }

            if (room.status != Room.Status.INBATTLE)
            {
                return;
            }

            if (player.id != msgHit.id)
            {
                return;
            }

            int damage = 10;
            targetPlayer.hp -= damage;

            msgHit.id = player.id;
            msgHit.hp = targetPlayer.hp;
            msgHit.damage = damage;
            room.Broadcast(msgHit);
        }
    }
}
