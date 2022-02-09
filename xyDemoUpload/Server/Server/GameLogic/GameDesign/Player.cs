using ProtoBuf;
using Server.Network.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.GameLogic.GameDesign
{
    class Player
    {
        public string id = "";
        public float x = 0;
        public float y = 0;
        public float z = 0;
        public float ex = 0;
        public float ey = 0;
        public float ez = 0;
        public int roomId = -1;
        public int camp = -1;
        public int hp = 100;

        public ClientState state = null;

        public Player(ClientState state)
        {
            this.state = state;
        }

        public void Send(IExtensible msgBase)
        {
            NetManager.Send(state, msgBase);
        }
    }
}
