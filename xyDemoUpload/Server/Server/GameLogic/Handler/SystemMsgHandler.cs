using proto.SystemMsg;
using ProtoBuf;
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
        public static void OnMsgPing(ClientState cs, IExtensible msgBase)
        {
            Console.WriteLine("OnMsgPing");
            cs.lastPingTime = NetManager.GetTimeStamp();

            MsgPong msgPong = new MsgPong();
            NetManager.Send(cs, msgPong);
        }
    }
}
