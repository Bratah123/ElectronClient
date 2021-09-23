using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace ElectronMS
{
    public sealed class LinkClient
    {
        private Socket inSocket;
        private Socket outSocket;
        private const int MAXBUFFER = 16000;

        byte[] OutBuffer = new byte[MAXBUFFER];
        byte[] InBuffer = new byte[MAXBUFFER];
        public bool Connected = true;

        private string mToIP = Constant.IP;

        public LinkClient(Socket sock, ushort toPort, string toIP)
        {
            try
            {
                inSocket = sock;
                mToIP = toIP;
                outSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                outSocket.BeginConnect(toIP, toPort, new AsyncCallback(OnOutconnect), outSocket);
                Debug.WriteLine("Client attempted to connect to " + toIP);
            }
            catch { }
        }

        private void OnOutconnect(IAsyncResult ar)
        {
            try
            {
                outSocket.EndConnect(ar);
            }
            catch
            {
                inSocket.Shutdown(SocketShutdown.Both);
                return;
            }
            if (!outSocket.Connected)
            {
                Debug.WriteLine("Failed to connect lulz.");
                inSocket.Shutdown(SocketShutdown.Both);
                return;
            }
            else
                Debug.WriteLine("Link is operational");
            try
            {
                outSocket.BeginReceive(OutBuffer, 0, MAXBUFFER, SocketFlags.None, new AsyncCallback(OnOutPacket), outSocket);
                inSocket.BeginReceive(InBuffer, 0, MAXBUFFER, SocketFlags.None, new AsyncCallback(OnInPacket), inSocket);
            }
            catch { }
        }

        private void SendToIn(byte[] data)
        {
            if (!Connected)
            {
                return;
            }
             BeginInSend(data);
        }

        private void SendToOut(byte[] data)
        {
            if (!Connected)
            {
                return;
            }
            BeginOutSend(data);
        }

        private void BeginOutSend(byte[] data)
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.SetBuffer(data, 0, data.Length);
            outSocket.SendAsync(args);
        }

        private void BeginInSend(byte[] data)
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.SetBuffer(data, 0, data.Length);
            inSocket.SendAsync(args);
        }

        private volatile Mutex mutex2 = new Mutex();

        private void OnOutPacket(IAsyncResult ar)
        {
            if (!Connected)
            {
                return;
            }
            mutex2.WaitOne();
            try
            {
                int len = outSocket.EndReceive(ar);
                if (len <= 0 || !Connected)
                {
                    Connected = false;
                    outSocket.Shutdown(SocketShutdown.Both);
                    return;
                }
                byte[] toSend = new byte[len];
                Buffer.BlockCopy(OutBuffer, 0, toSend, 0, len);
                SendToIn(toSend);
                outSocket.BeginReceive(OutBuffer, 0, MAXBUFFER, SocketFlags.None, new AsyncCallback(OnOutPacket), outSocket);
            }
            finally
            {
                mutex2.ReleaseMutex();
            }
        }

        private volatile Mutex mutex = new Mutex();

        private void OnInPacket(IAsyncResult ar)
        {
            mutex.WaitOne();
            try
            {
                int len = inSocket.EndReceive(ar);
                if (len <= 0 || !Connected)
                {
                    Connected = false;
                    inSocket.Shutdown(SocketShutdown.Both);
                    return;
                }
                byte[] toSend = new byte[len];
                Buffer.BlockCopy(InBuffer, 0, toSend, 0, len);
                SendToOut(toSend);
                inSocket.BeginReceive(InBuffer, 0, MAXBUFFER, SocketFlags.None, new AsyncCallback(OnInPacket), inSocket);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }
}
