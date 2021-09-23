using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using MapleLib.MapleCryptoLib;
using MapleLib.PacketLib;
using System.Windows.Forms;

namespace ElectronMS
{
    public sealed class InterceptedLinkedClient
    {
        Session inSession;
        Session outSession;

        bool gotEnc = false;
        ushort Port;
        bool connected = true;
        bool block = false;
        int charID = -1;
        MapleMode Mode;

        public InterceptedLinkedClient(Session inside, string toIP, ushort toPort)
        {
            this.Mode = Program.Mode;
            this.Port = toPort;
            Debug.WriteLine("New linkclient to " + toIP);
            inSession = inside;
            inside.OnPacketReceived += new Session.PacketReceivedHandler(inside_OnPacketReceived);
            inside.OnClientDisconnected += new Session.ClientDisconnectedHandler(inside_OnClientDisconnected);
            ConnectOut(toIP, toPort);

            Debug.WriteLine("Connecting out to port " + toPort);
        }

        void inside_OnClientDisconnected(Session session)
        {
            if(outSession != null)
            outSession.Socket.Shutdown(SocketShutdown.Both);
            connected = false;
        }

        void ConnectOut(string ip, int port)
        {
            try
            {
                Socket outSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                outSocket.BeginConnect(ip, port, new AsyncCallback(OnOutConnectCallback), outSocket);
            }
            catch { outSession_OnClientDisconnected(null); }
        }

        private void OnOutConnectCallback(IAsyncResult ar)
        {
            Socket sock = (Socket)ar.AsyncState;
            try
            {
                sock.EndConnect(ar);
            }
            catch (Exception ex)
            {
                connected = false;
                inSession.Socket.Shutdown(SocketShutdown.Both);
                MessageBox.Show(ex.ToString());
                return;
            }

            if (outSession != null)
            {
                outSession.Socket.Close();
                outSession.Connected = false;
            }
            Session session = new Session(sock, SessionType.CLIENT_TO_SERVER);
            outSession = session;
            outSession.OnInitPacketReceived += new Session.InitPacketReceived(outSession_OnInitPacketReceived);
            outSession.OnPacketReceived += new Session.PacketReceivedHandler(outSession_OnPacketReceived);
            outSession.OnClientDisconnected += new Session.ClientDisconnectedHandler(outSession_OnClientDisconnected);
            session.WaitForDataNoEncryption();
        }

        private volatile Mutex mutex = new Mutex();

        void inside_OnPacketReceived(byte[] packet)
        {
            if (!connected || block)
            {
                return;
            }
            mutex.WaitOne();
            try
            {
                /*short opcode = BitConverter.ToInt16(packet, 0);
                switch (Mode)
                {
                    case MapleMode.GMS:
                        switch (opcode)
                        {
                            case 0x27:
                                charID = BitConverter.ToInt32(packet, 2);
                                break;
                            case 0x38:
                                SendLoginData();
                                break;
                        }
                        break;
                    default:
                        Debug.WriteLine("Write handlers for EMS please.");
                        break;
                }*/
                outSession.SendPacket(packet);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        void outSession_OnClientDisconnected(Session session)
        {
            if (block){ // simply changing channels, shouldn't happen though
                return;
            }
            inSession.Socket.Shutdown(SocketShutdown.Both);
            Debug.WriteLine("out disconnected (" + Port + ")");
            connected = false;
        }

        private volatile Mutex mutex2 = new Mutex();

        void outSession_OnPacketReceived(byte[] packet)
        {
            if (!gotEnc || !connected)
            {
                return;
            }
            mutex2.WaitOne();
            try
            {
                short opcode = BitConverter.ToInt16(packet, 0);
                Debug.WriteLine("Got a packet from server: " + opcode);
                /*switch (Mode)
                {
                    case MapleMode.GMS:
                        if (opcode == 0x10)
                        {
                            block = true;
                            //short newPort = BitConverter.ToInt16(packet, 7);
                            //ConnectOut(Program.toIP, newPort);
                            return;
                        }
                        if (opcode == 0x0B)
                            block = true;
                        break;

                    default:
                        break;
                }*/
                inSession.SendPacket(packet);
            }
            finally
            {
                mutex2.ReleaseMutex();
            }
        }

        void outSession_OnInitPacketReceived(short version, byte serverIdentifier)
        {
            Debug.WriteLine("Init packet: v" + version + "ident: " + serverIdentifier);
            /*if (block)
            {
                connected = true;
                ChannelCompleteLogin();
                return;
            }*/
            if (this.Port == 8484 || this.Port == 9900)
            {
                SendHandShake(230, 8);
            //    MessageBox.Show("쉐킷1");
            }
            else
            {
                SendHandShake2(230, 8);
            //    MessageBox.Show("쉐킷2");
            }
        }

        void ChannelCompleteLogin()
        {
            PacketWriter writer = new PacketWriter();
            if (Program.Mode == MapleMode.MSEA)
            {
                writer.WriteShort(0x27); 
            }
            else
            {
                writer.WriteShort(0x27);
            }
            writer.WriteInt(charID);

            outSession.SendPacket(writer.ToArray());
            block = false;
            Debug.WriteLine("change channel complete.");
        }

        private void SendHandShake(short version, byte serverident)
        {
            PacketWriter writer = new PacketWriter();
            writer.WriteShort(0x0E);
            writer.WriteShort(95);
            writer.WriteMapleString("1");
            byte[] riv = new byte[4];
            byte[] siv = new byte[4];
            Random lulz = new Random();
            lulz.NextBytes(riv);
            lulz.NextBytes(siv);
            inSession.RIV = new MapleCrypto(riv, version);
            inSession.SIV = new MapleCrypto(siv, version);
            writer.WriteBytes(riv);
            writer.WriteBytes(siv);
            writer.WriteByte(serverident);
            // writer.WriteByte(0);
            gotEnc = true;
            inSession.SendRawPacket(writer.ToArray());
        }

        private void SendHandShake2(short version, byte serverident)
        {
            PacketWriter writer = new PacketWriter();
            writer.WriteShort(588);
            writer.WriteShort(291);
            writer.WriteMapleString("65766"); // 227.2
            byte[] riv = new byte[4];
            byte[] siv = new byte[4];
            Random lulz = new Random();
            lulz.NextBytes(riv);
            lulz.NextBytes(siv);
            inSession.RIV = new MapleCrypto(riv, version);
            inSession.SIV = new MapleCrypto(siv, version);
            writer.WriteBytes(riv);
            writer.WriteBytes(siv);
            writer.WriteByte(serverident);
            writer.WriteBytes(HexEncoding.GetBytes("47 00 00 10 40 00 B5 18 0B 00 F3 29 4B 00 66 D0 12 00 F4 FA 5D 00 52 02 00 00 B7 FD 5D 00 01 16 00 00 62 15 5E 00 FE F9 01 00 94 0F 60 00 4E 16 00 00 95 26 60 00 81 02 00 00 82 29 60 00 0E 00 00 00 73 2A 60 00 39 12 00 00 EC 3E 60 00 9F 02 00 00 76 42 60 00 58 0B 00 00 FD 4F 60 00 7A 00 00 00 50 59 60 00 5B 00 00 00 A9 5A 60 00 00 C5 31 00 C8 20 92 00 95 00 00 00 43 22 92 00 02 01 00 00 F8 27 92 00 20 03 00 00 55 2E 92 00 2E 18 00 00 EB 48 92 00 7E 14 00 00 77 5F 92 00 5A 4E 00 00 3A B0 92 00 EB 18 00 00 63 C9 92 00 F4 08 00 00 9D D2 92 00 38 44 00 00 B0 19 93 00 2A 8A 00 00 1D A4 93 00 05 00 00 00 AF A4 93 00 83 2C 00 00 5E D1 93 00 E4 00 00 00 6D D2 93 00 3E 5F 00 00 5D 32 94 00 24 D1 02 00 FF 03 97 00 D2 00 00 00 26 05 97 00 72 A7 00 00 D0 B0 97 00 EC 03 00 00 8D B5 97 00 50 00 00 00 66 B7 97 00 3B 8A 18 00 5C 42 B0 00 32 3A 00 00 4E 7D B0 00 7F 6B 00 00 B1 E9 B0 00 F8 7E 01 00 5A 69 B2 00 1C 00 00 00 A9 6B B2 00 26 00 00 00 C3 6C B2 00 28 00 00 00 93 6F B2 00 26 00 00 00 1C 71 B2 00 1D 00 00 00 BA 72 B2 00 09 82 06 00 35 F5 B8 00 4D 04 00 00 21 FA B8 00 1D CC 13 00 CD C6 CC 00 59 02 00 00 97 C9 CC 00 CC 21 00 00 13 ED CC 00 00 00 40 00 13 ED 0C 01 69 18 3A 00 CD 0B 47 01 17 0A 00 00 5E 1C 47 01 50 05 00 00 A7 29 47 01 47 29 00 00 9C 5D 47 01 BF 4E 04 00 49 AD 4B 01 27 28 01 00 FD D7 4C 01 73 EC 00 00 A7 C4 4D 01 B6 3C 01 00 0F 08 4F 01 64 B1 04 00 BF B9 53 01 64 00 00 00 6C BA 53 01 16 04 00 00 F4 BE 53 01 0F 00 00 00 6F BF 53 01 72 00 00 00 2A C0 53 01 7D 01 00 00 34 C2 53 01 43 00 00 00 4E C3 53 01 EF 09 00 00 08 CE 53 01 BF 00 00 00 96 CF 53 01 97 02 00 00 76 D4 53 01 0F 03 00 00 A5 D8 53 01 1B 00 00 00 F9 D8 53 01 44 00 00 00 3C DB 53 01 C4 5C 27 00 A8 DA 85 01 20 DE 10 00"));
            gotEnc = true;
            inSession.SendRawPacket(writer.ToArray());
        }

        void SendLoginData()
        {
            PacketWriter writer = new PacketWriter();
            writer.WriteShort(0x01);
            writer.WriteMapleString(Program.username);
            writer.WriteMapleString(Program.password);
            writer.WriteMapleString(Environment.OSVersion.VersionString);
            outSession.SendPacket(writer.ToArray());
        }
    }
}
