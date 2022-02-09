using proto.LoginMsg;
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
        public static void OnMsgLogin(ClientState cs, IExtensible msgBase)
        {
            MsgLogin msgLogin = msgBase as MsgLogin;

            if (cs.player != null)
            {
                msgLogin.isLoginSuccess = false;
                NetManager.Send(cs, msgLogin);

                return;
            }

            Player player = new Player(cs);
            player.id = msgLogin.id;
            PlayerManager.AddPlayer(msgLogin.id, player);
            cs.player = player;

            msgLogin.isLoginSuccess = true;
            player.Send(msgLogin);
        }
    }
}
