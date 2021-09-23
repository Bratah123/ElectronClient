using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Windows.Forms;

namespace ElectronMS
{
    public sealed class LinkServer
    {
        public Socket sListener;
        public string LinkIP = Constant.IP;
        public ushort Port;
        public LinkServer(ushort port, string toIP)
        {
            LinkIP = toIP;
            Port = port;
            try
            {
                sListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sListener.Bind(new IPEndPoint(IPAddress.Any, port));
                sListener.Listen(10);
                sListener.BeginAccept(new AsyncCallback(OnConnect), sListener);
            }
            catch { }
        }

        private void OnConnect(IAsyncResult ar)
        {
            Socket client = sListener.EndAccept(ar);
            Debug.WriteLine("Created normal linkedclient");
            LinkClient lClient = new LinkClient(client, Port, LinkIP);
            sListener.BeginAccept(new AsyncCallback(OnConnect), sListener);
        }
    }
}
