using proto.RoomMsg;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.GameLogic.GameDesign
{
    class RoomManager
    {
        public static Dictionary<int, Room> rooms = new Dictionary<int, Room>();

        private static int maxId = 1;

        public static Room GetRoom(int id)
        {
            if (rooms.ContainsKey(id))
            {
                return rooms[id];
            }

            return null;
        }

        public static Room AddRoom()
        {
            Room room = new Room();
            room.id = maxId;
            ++maxId;
            rooms.Add(room.id, room);

            return room;
        }

        public static bool RemoveRoom(int id)
        {
            rooms.Remove(id);

            return true;
        }

        public static void Update()
        {
            foreach (Room room in rooms.Values)
            {
                room.Update();
            }
        }

        public static IExtensible ToMsg()
        {
            MsgGetRoomList msgGetRoomList = new MsgGetRoomList();

            int i = 0;
            foreach (Room room in rooms.Values)
            {
                RoomInfo roomInfo = new RoomInfo();
                roomInfo.id = room.id;
                roomInfo.count = room.playerIds.Count;
                roomInfo.status = (int)room.status;

                msgGetRoomList.rooms.Add(roomInfo);
                ++i;
            }

            return msgGetRoomList;
        }
    }
}
