using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Network.Framework
{
    public class ClientState
    {
        public Socket socket = null;
        public ByteArray readBuf = new ByteArray();
        public long lastPingTime = 0;

        public object player = null;
    }
}
