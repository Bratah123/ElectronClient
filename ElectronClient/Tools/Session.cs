/*  MapleLib - A general-purpose MapleStory library
 * Copyright (C) 2009, 2010 Snow and haha01haha01
   
 * This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.*/

using System;
using System.Net.Sockets;
using MapleLib.MapleCryptoLib;
using System.Diagnostics;
using ElectronMS;
using System.Threading;
using System.Windows.Forms;

namespace MapleLib.PacketLib
{
	/// <summary>
	/// Class to a network session socket
	/// </summary>
	public class Session
	{

		/// <summary>
		/// The Session's socket
		/// </summary>
		private readonly Socket _socket;

		private SessionType _type;

		/// <summary>
		/// The Recieved packet crypto manager
		/// </summary>
		private MapleCrypto _RIV;

		/// <summary>
		/// The Sent packet crypto manager
		/// </summary>
		private MapleCrypto _SIV;

		/// <summary>
		/// Method to handle packets received
		/// </summary>
		public delegate void PacketReceivedHandler(byte[] packet);

		/// <summary>
		/// Packet received event
		/// </summary>
		public event PacketReceivedHandler OnPacketReceived;

		/// <summary>
		/// Method to handle client disconnected
		/// </summary>
		public delegate void ClientDisconnectedHandler(Session session);

		/// <summary>
		/// Client disconnected event
		/// </summary>
		public event ClientDisconnectedHandler OnClientDisconnected;

		public delegate void InitPacketReceived(short version, byte serverIdentifier);
		public event InitPacketReceived OnInitPacketReceived;

		/// <summary>
		/// The Recieved packet crypto manager
		/// </summary>
		public MapleCrypto RIV
		{
			get { return _RIV; }
			set { _RIV = value; }
		}

        public bool Connected = true;

		/// <summary>
		/// The Sent packet crypto manager
		/// </summary>
		public MapleCrypto SIV
		{
			get { return _SIV; }
			set { _SIV = value; }
		}

		/// <summary>
		/// The Session's socket
		/// </summary>
		public Socket Socket
		{
			get { return _socket; }
		}

		public SessionType Type
		{
			get { return _type; }
        }

        #region buffers
        private const int DEFAULT_SIZE = 16000;
        private byte[] mBuffer = new byte[DEFAULT_SIZE];
        private byte[] mSharedBuffer = new byte[DEFAULT_SIZE];
        private int mCursor = 0;
        #endregion

        /// <summary>
		/// Creates a new instance of a Session
		/// </summary>
		/// <param name="socket">Socket connection of the session</param>

		public Session(Socket socket, SessionType type)
		{
			_socket = socket;
			_type = type;
        }

        #region bufferstuff

        #endregion
        /// <summary>
		/// Waits for more data to arrive (encrypted)
		/// </summary>
		public void WaitForData()
		{
            BeginReceive();
		}

		public void WaitForDataNoEncryption()
		{
            if (!_socket.Connected)
            {
                ForceDisconnect();
                return;
            }
            byte[] InitBuffer = new byte[16];
            _socket.BeginReceive(InitBuffer, 0, 16, SocketFlags.None, new AsyncCallback(OnInitPacketRecv), InitBuffer);
		}

        private void BeginReceive()
        {
            if (!Connected || !_socket.Connected)
            {
                ForceDisconnect();
                return;
            }
            _socket.BeginReceive(mSharedBuffer, 0, DEFAULT_SIZE, SocketFlags.None, new AsyncCallback(EndReceive), _socket);
        }

        private void ForceDisconnect()
        {
            if (!Connected) return;
            if (OnClientDisconnected != null)
            {
                OnClientDisconnected(this);
            }
            Connected = false;
        }

        public void Append(byte[] pBuffer) { Append(pBuffer, 0, pBuffer.Length); }
        public void Append(byte[] pBuffer, int pStart, int pLength)
        {
            if (mBuffer.Length - mCursor < pLength)
            {
                int newSize = mBuffer.Length * 2;
                while (newSize < mCursor + pLength) newSize *= 2;
                Array.Resize<byte>(ref mBuffer, newSize);
            }
            Buffer.BlockCopy(pBuffer, pStart, mBuffer, mCursor, pLength);
            mCursor += pLength;
        }

        private void EndReceive(IAsyncResult ar)
        {
            if (!Connected) {
                return;
            }
            int recvLen = 0;
            try {
                recvLen = _socket.EndReceive(ar);
            } catch {
                ForceDisconnect();
                return;
            }
            if (recvLen <= 0) {
                ForceDisconnect();
                return;
            }
            Append(mSharedBuffer, 0, recvLen);

            while (true) {
                if (mCursor < 4) {
                    break;
                }
                ushort packetSize = MapleCrypto.getPacketLength(mBuffer, 0);
                if (mCursor < (packetSize + 4)) {
                    break;
                }
                byte[] packetBuffer = new byte[packetSize];
                Buffer.BlockCopy(mBuffer, 4, packetBuffer, 0, packetSize);
                RIV.Transform(packetBuffer);
                mCursor -= (packetSize + 4);
                if (mCursor > 0) {
                    Buffer.BlockCopy(mBuffer, packetSize + 4, mBuffer, 0, mCursor);
                }
                if (OnPacketReceived != null) {
                    if (!Connected) {
                        return;
                    }
                    OnPacketReceived(packetBuffer);
                }
            }
            BeginReceive();
        }

        private void OnInitPacketRecv(IAsyncResult ar)
        {
            if (!Connected) return;
            byte[] data = (byte[])ar.AsyncState;
            int len = _socket.EndReceive(ar);
            if (len < 15)
            {
                if (OnClientDisconnected != null)
                {
                    OnClientDisconnected(this);
                }
                Connected = false;
                return;
            }
            PacketReader reader = new PacketReader(data);
            reader.ReadShort();
            reader.ReadShort();
            reader.ReadMapleString();
            _SIV = new MapleCrypto(reader.ReadBytes(4), 227); // kms
            _RIV = new MapleCrypto(reader.ReadBytes(4), 227); // kms
            //byte serverType = reader.ReadByte();
            if (_type == SessionType.CLIENT_TO_SERVER)
            {
                OnInitPacketReceived(227, 1);
            }
            WaitForData();
        }

		/// <summary>
		/// Encrypts the packet then send it to the client.
		/// </summary>
		/// <param name="packet">The PacketWrtier object to be sent.</param>
/*		public void SendPacket(PacketWriter packet)
		{
            if (!Connected) return;
			SendPacket(packet.ToArray());
		}*/

		/// <summary>
		/// Encrypts the packet then send it to the client.
		/// </summary>
		/// <param name="input">The byte array to be sent.</param>
		public void SendPacket(byte[] input)
		{
            if (!Connected || !_socket.Connected) {
                return;
            }
			byte[] cryptData = input;
			byte[] sendData = new byte[cryptData.Length + 4];
			byte[] header = _type == SessionType.SERVER_TO_CLIENT ? _SIV.getHeaderToClient(cryptData.Length) : _SIV.getHeaderToServer(cryptData.Length);

            //SIV.Encrypt(cryptData);
            SIV.Transform(cryptData);

			System.Buffer.BlockCopy(header, 0, sendData, 0, 4);
			System.Buffer.BlockCopy(cryptData, 0, sendData, 4, cryptData.Length);

            SendRawPacket(sendData);
		}

 //       private volatile LockFreeQueue<ByteArraySegment> mSendSegments = new LockFreeQueue<ByteArraySegment>();
 //       private int mSending = 0;

		/// <summary>
		/// Sends a raw buffer to the client.
		/// </summary>
		/// <param name="buffer">The buffer to be sent.</param>
		public void SendRawPacket(byte[] buffer)
		{
            if (!Connected) {
                return;
            }
            
 //           mSendSegments.Enqueue(new ByteArraySegment(buffer));
            
 //           if (Interlocked.CompareExchange(ref mSending, 1, 0) == 0)
                BeginInSend(buffer);
		}

/*        private void BeginInSend()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += (s, a) => EndInSend(a);
            ByteArraySegment segment = mSendSegments.Next;
            args.SetBuffer(segment.Buffer, segment.Start, segment.Length);
            if (!_socket.SendAsync(args)) {
                EndInSend(args);
            }
        }*/

        private void BeginInSend(byte[] data)
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.SetBuffer(data, 0, data.Length);
                    _socket.SendAsync(args);
      //                  EndInSend(args);
                }

 /*       private void EndInSend(SocketAsyncEventArgs pArguments) {
            if (!Connected) {
                return;
            }
            if (pArguments.BytesTransferred <= 0) {
                Connected = false;
                return;
            }
            if (mSendSegments.Next.Advance(pArguments.BytesTransferred)) {
                mSendSegments.Dequeue();
            }
            if (mSendSegments.Next != null) {
                BeginInSend();
            } else {
                mSending = 0;
            }
        }*/
	}
}