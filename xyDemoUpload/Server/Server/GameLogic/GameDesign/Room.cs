using proto.BattleMsg;
using proto.RoomMsg;
using ProtoBuf;
using Server.Network.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.GameLogic.GameDesign
{
    class Room
    {
        public int id = -1;
        public int maxPlayerCount = 6;
        public Dictionary<string, bool> playerIds = new Dictionary<string, bool>();
        public string houseOwnerId = "";

        public enum Status
        {
            PREPARING = 0,
            INBATTLE = 1,
        }

        public Status status = Status.PREPARING;

        private static float[,,] birthPoints = new float[2, 3, 6]
        {
            {
                {-33.7f, 0.0f, -31.6f, 0.0f, -1.9f, 0.0f },
                {-22.3f, 0.0f, -34.3f, 0.0f, -10.7f, 0.0f },
                {-13.4f, 0.0f, -32.9f, 0.0f, -8.7f, 0.0f },
            },
            {
                {-22.5f, 0.0f, 1.4f, 0.0f, -179.9f, 0.0f },
                {-35.3f, 0.0f, 0.6f, 0.0f, -156.8f, 0.0f },
                {-13.6f, 0.0f, 0.7f, 0.0f, 115.1f, 0.0f },
            },
        };

        private long lastJudgeTime = 0;

        public void Update()
        {
            if (status != Status.INBATTLE)
            {
                return;
            }

            if (NetManager.GetTimeStamp() - lastJudgeTime < 3)
            {
                return;
            }

            lastJudgeTime = NetManager.GetTimeStamp();

            int winCamp = JudgeWinner();
            if (winCamp == -1)
            {
                return;
            }

            status = Status.PREPARING;

            MsgBattleResult msgBattleResult = new MsgBattleResult();
            msgBattleResult.winCamp = winCamp;
            Broadcast(msgBattleResult);
        }

        public bool CanStartBattle()
        {
            if (status != Status.PREPARING)
            {
                return false;
            }

            int count0 = 0;
            int count1 = 0;
            foreach (string id in playerIds.Keys)
            {
                Player player = PlayerManager.GetPlayer(id);
                if (player.camp == 0)
                {
                    ++count0;
                }
                else
                {
                    ++count1;
                }
            }

            if (count0 < 1 || count1 < 1)
            {
                return false;
            }

            return true;
        }

        public CharacterInfo PlayerToCharacterInfo(Player player)
        {
            CharacterInfo characterInfo = new CharacterInfo();
            characterInfo.camp = player.camp;
            characterInfo.id = player.id;
            characterInfo.hp = player.hp;
            characterInfo.x = player.x;
            characterInfo.y = player.y;
            characterInfo.z = player.z;
            characterInfo.ex = player.ex;
            characterInfo.ey = player.ey;
            characterInfo.ez = player.ez;

            return characterInfo;
        }

        public bool StartBattle()
        {
            if (!CanStartBattle())
            {
                return false;
            }

            status = Status.INBATTLE;

            ResetPlayers();

            MsgEnterBattle msgEnterBattle = new MsgEnterBattle();
            msgEnterBattle.mapId = 0;

            int i = 0;
            foreach (string id in playerIds.Keys)
            {
                Player player = PlayerManager.GetPlayer(id);
                msgEnterBattle.characters.Add(PlayerToCharacterInfo(player));
                ++i;
            }

            Broadcast(msgEnterBattle);

            return true;
        }

        public bool AddPlayer(string id)
        {
            Player player = PlayerManager.GetPlayer(id);
            if (player == null)
            {
                Console.WriteLine("Room.AddPlayer fail, player is null.");
                return false;
            }

            if (playerIds.Count >= maxPlayerCount)
            {
                Console.WriteLine("Room.AddPlayer fail, room is full.");
                return false;
            }

            if (status != Status.PREPARING)
            {
                Console.WriteLine("Room.AddPlayer fail, room is not preparing.");
                return false;
            }

            if (playerIds.ContainsKey(id))
            {
                Console.WriteLine("Room.AddPlayaer fail, already in the room.");
                return false;
            }

            playerIds[id] = true;
            player.camp = ChooseCamp();
            player.roomId = this.id;

            if (houseOwnerId == "")
            {
                houseOwnerId = player.id;
            }

            Broadcast(ToMsg());

            return true;
        }

        public bool RemovePlayer(string id)
        {
            Player player = PlayerManager.GetPlayer(id);
            if (player == null)
            {
                Console.WriteLine("Room.RemovePlayer fail, player is null.");
                return false;
            }

            if (!playerIds.ContainsKey(id))
            {
                Console.WriteLine("Room.RemovePlayer fail, already not in the room.");
                return false;
            }

            playerIds.Remove(id);
            player.camp = -1;
            player.roomId = -1;

            if (IsHouseOwner(player))
            {
                houseOwnerId = ChangeHouseOwner();
            }

            if (status == Status.INBATTLE)
            {
                MsgLeaveBattle msgLeaveBattle = new MsgLeaveBattle();
                msgLeaveBattle.id = player.id;
                Broadcast(msgLeaveBattle);
            }

            if (playerIds.Count == 0)
            {
                RoomManager.RemoveRoom(this.id);
            }

            Broadcast(ToMsg());

            return true;
        }

        public IExtensible ToMsg()
        {
            MsgGetRoomInfo msgGetRoomInfo = new MsgGetRoomInfo();
            //int count = playerIds.Count;
            //msgGetRoomInfo.players = new List<PlayerInfo>();

            int i = 0;
            foreach (string id in playerIds.Keys)
            {
                Player player = PlayerManager.GetPlayer(id);
                PlayerInfo playerInfo = new PlayerInfo();
                playerInfo.id = player.id;
                playerInfo.camp = player.camp;
                playerInfo.isHouseOwner = IsHouseOwner(player);

                msgGetRoomInfo.players.Add(playerInfo);
                ++i;
            }

            return msgGetRoomInfo;
        }

        private void SetBirthPos(Player player, int index)
        {
            int camp = player.camp;

            player.x = birthPoints[camp, index, 0];
            player.y = birthPoints[camp, index, 1];
            player.z = birthPoints[camp, index, 2];
            player.ex = birthPoints[camp, index, 3];
            player.ey = birthPoints[camp, index, 4];
            player.ez = birthPoints[camp, index, 5];
        }

        private void ResetPlayers()
        {
            int count0 = 0;
            int count1 = 0;
            foreach (string id in playerIds.Keys)
            {
                Player player = PlayerManager.GetPlayer(id);
                player.hp = 100;

                if (player.camp == 0)
                {
                    SetBirthPos(player, count0);
                    ++count0;
                }
                else
                {
                    SetBirthPos(player, count1);
                    ++count1;
                }
            }
        }

        public void Broadcast(IExtensible msgBase)
        {
            foreach (string id in playerIds.Keys)
            {
                Player player = PlayerManager.GetPlayer(id);
                player.Send(msgBase);
            }
        }

        private string ChangeHouseOwner()
        {
            foreach (string id in playerIds.Keys)
            {
                return id;
            }

            return "";
        }

        public int JudgeWinner()
        {
            int count0 = 0;
            int count1 = 0;
            foreach (string id in playerIds.Keys)
            {
                Player player = PlayerManager.GetPlayer(id);
                if (!IsDie(player))
                {
                    if (player.camp == 0)
                    {
                        ++count0;
                    }
                    else if (player.camp == 1)
                    {
                        ++count1;
                    }
                }
            }

            if (count0 == 0)
            {
                return 1;
            }
            else if (count1 == 0)
            {
                return 0;
            }

            return -1;
        }

        public bool IsDie(Player player)
        {
            return player.hp <= 0;
        }

        public bool IsHouseOwner(Player player)
        {
            return player.id == houseOwnerId;
        }

        private int ChooseCamp()
        {
            int count0 = 0;
            int count1 = 0;
            foreach (string id in playerIds.Keys)
            {
                Player player = PlayerManager.GetPlayer(id);
                if (player.camp == 0)
                {
                    ++count0;
                }
                else if (player.camp == 1)
                {
                    ++count1;
                }
            }

            if (count0 <= count1)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
    }
}
