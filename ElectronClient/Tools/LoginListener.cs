using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapleLib;
using MapleLib.PacketLib;
using System.Net;
using System.Net.Sockets;

namespace ElectronMS
{
    public class Listener
    {
        /// <summary>
        /// The listener socket
        /// </summary>
        private readonly Socket _listener;
        private ushort Port;

        /// <summary>
        /// Method called when a client is connected
        /// </summary>
        public delegate void ClientConnectedHandler(Session session, ushort port);

        /// <summary>
        /// Client connected event
        /// </summary>
        public event ClientConnectedHandler OnClientConnected;
        /// <summary>
        /// A List contains all the sessions connected to the listener.
        /// </summary>
        public bool Running { get { return _listener.IsBound; } }
        /// <summary>
        /// Creates a new instance of Acceptor
        /// </summary>
        public Listener()
        {
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Starts listening and accepting connections
        /// </summary>
        /// <param name="port">Port to listen to</param>
        public void Listen(ushort port)
        {
            Port = port;
            _listener.Bind(new IPEndPoint(IPAddress.Any, port));
            _listener.Listen(15);
            _listener.BeginAccept(new AsyncCallback(OnClientConnect), null);
        }

        /// <summary>
        /// Client connected handler
        /// </summary>
        /// <param name="iarl">The IAsyncResult</param>
        private void OnClientConnect(IAsyncResult async)
        {
                Socket socket = _listener.EndAccept(async);
                Session session = new Session(socket, SessionType.SERVER_TO_CLIENT);
                if (OnClientConnected != null)
                    OnClientConnected(session, Port);

                session.WaitForData();

                _listener.BeginAccept(new AsyncCallback(OnClientConnect), null);
           
        }
        /// <summary>
        /// Releases a session.
        /// </summary>
        /// <param name="session">The Session to kick.</param>
        public void Release(Session session)
        {
            session.Socket.Close();
        }
        /// <summary>
        /// Stops listening.
        /// </summary>
        public void Close()
        {
            _listener.Close();
        }
    }
}
