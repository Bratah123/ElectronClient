using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Resources;
using System.Collections.Specialized;
using System.Reflection;
using Microsoft.Win32;
using MapleLib.PacketLib;
using MapleLib.MapleCryptoLib;
using MapleLib;



namespace ElectronMS
{
    public enum MapleMode
    {
        MSEA,
        EMS,
        GMS,
        KMS
    }

    public partial class frmMain : Form
    {
        public string installpath = "";
        private string TempFolder = "";

        Process Maple;
        private NotifyIcon trayIcon;
        public ContextMenu trayMenu;
        public MapleMode Mode;
        public Dictionary<ushort, LinkServer> Servers = new Dictionary<ushort, LinkServer>();
        public Dictionary<ushort, Listener> Listeners = new Dictionary<ushort, Listener>();

        public frmMain()
        {
          
            InitializeComponent();
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Bisque;
            this.TransparencyKey = Color.Bisque;
            TempFolder = Path.GetTempPath();
            this.Mode = Program.Mode;
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Start ElectronMS", OnStartButton);
            trayMenu.MenuItems.Add("Vote Page", OnSiteButton);
           // trayMenu.MenuItems.Add("Show", OnShow);
           // trayMenu.MenuItems.Add("Unstuck account", OnUnstuck);
            trayMenu.MenuItems.Add("Exit", OnExit);
            trayIcon = new NotifyIcon();
            trayIcon.Text = "ElectronMS" + ((Program.DevMode) ? " - DEV" : "");
            trayIcon.Icon = new Icon(this.Icon, 40, 40);

            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
            if(Mode != MapleMode.GMS)
            ShowInTaskbar = false; // Remove from taskbar.
            new Thread(StartLoading).Start();
        }

        private void OnSiteButton(object sender, EventArgs e)
        {
            Process.Start(Program.regURL);
        }

        private void OnShow(object sender, EventArgs e)
        {
            this.Show();
            this.Visible = true;
        }

        private void OnUnstuck(object sender, EventArgs e)
        {
            string URL = "http://ip-address/unstuck.php";
            WebClient webClient = new WebClient();

            NameValueCollection formData = new NameValueCollection();
            formData["username"] = textBox1.Text;

            byte[] responseBytes = webClient.UploadValues(URL, "POST", formData);
            string responsefromserver = Encoding.UTF8.GetString(responseBytes);
            Console.WriteLine(responsefromserver);
            webClient.Dispose();
            MessageBox.Show("Unstucked Account!");
        }

        private void OnStartButton(object sender, EventArgs e)
        {
            LaunchMaple();
        }

        private void OnExit(object sender, EventArgs e)
        {
            try
            {
                if (Maple != null)
                    Maple.Kill();
            }
            catch { }
            frmMain_FormClosed(null, null);
            Environment.Exit(0);
        }

        void StartLoading()
        {
           routeIP();
         StartTunnels();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
 
        }

        public void LaunchMaple()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            if (!File.Exists(currentDirectory + "/Maplestory.exe"))
            {
                MessageBox.Show("메이플스토리가 설치 된 경로에 접속기를 넣어주세요.");
                Application.Exit();
            }
            Maple = new Process();
            Maple.StartInfo.FileName = Path.Combine(currentDirectory, "Maplestory.exe");
            if (Mode == MapleMode.KMS)
                Maple.StartInfo.Arguments = "GameLaunching";
                     Maple.Start();

           
        }
          
    
        public void StartTunnels()
        {
            try
            {
                ushort lowPort = Program.lowPort;
                ushort highPort = Program.highPort;
                string toIP = Program.toIP;
                bool AllLink = true;
                if (AllLink)
                {
                    LinkServer loginServer = new LinkServer(8484, toIP);
                    ushort count = (ushort)(highPort - lowPort);
                    for (ushort i = 0; i <= count; i++)
                    {
                        LinkServer server = new LinkServer((ushort)(lowPort + i), toIP);
                    }
                }
                else
                {
                    Listener lListener = new Listener();
                    Debug.WriteLine("Listening on 8484");
                    lListener.OnClientConnected += new Listener.ClientConnectedHandler(listener_OnClientConnected);
                    lListener.Listen(8484);

                    ushort count = (ushort)(highPort - lowPort);
                    for (ushort i = 0; i <= count; i++)
                    {
                        Listener listener = new Listener();
                        listener.OnClientConnected += new Listener.ClientConnectedHandler(listener_OnClientConnected);
                        listener.Listen((ushort)(lowPort + i));
                        Debug.WriteLine("Listening on " + (lowPort + i).ToString());
                        Listeners.Add((ushort)(lowPort + i), listener);
                    }
                }
            }
            catch { }
        }

        void listener_OnClientConnected(Session session, ushort port)
        {
            Debug.WriteLine("Accepted connection on " + port);
            InterceptedLinkedClient lClient = new InterceptedLinkedClient(session, Program.toIP, port);
        }

        
        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            FixALL();
        }

        public void FixALL()
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            derouteIP();
        }

        private void routeIP()
        {
            Process p = new Process();
            p.StartInfo.FileName = "netsh.exe";
            p.StartInfo.Arguments = "int ip add addr 1 address=175.207.0. mask=255.255.255.0 st=ac";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
         }

        private void derouteIP()
        {
            Process p = new Process();
            p.StartInfo.FileName = "netsh.exe";
            p.StartInfo.Arguments = "int ip delete addr 1 175.207.0.";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start(Program.regURL);
        }



        private void btnLogin_Click_1(object sender, EventArgs e)
        {
            if (textBox1.Text.Length < 4 || textBox2.Text.Length < 4)
            {
                MessageBox.Show("Password and username has to be longer than 4.");
                return;
            }
            btnLogin.Enabled = false;
            
            /*string message;
            try
            {
                Hawt35.Tools.AccountCheck.CheckAccount(Program.accountCheck, textBox1.Text, textBox2.Text, out message);
            }
            catch
            {
                MessageBox.Show("계정인증서버가 오프라인 상태입니다.");
                textBox2.Text = "";
                btnLogin.Enabled = true;
                return;
            }
            if (message.Length != 15)
            {
                textBox2.Text = "";
                btnLogin.Enabled = true;
                return;
            }*/
            Program.username = textBox1.Text;
            Program.password = textBox2.Text;
            LaunchMaple();

            this.Hide();
            ShowInTaskbar = false;
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            btnLogin.Enabled = false;
          
        }


        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
                btnLogin_Click_1(null, null);
        }
    }
}
