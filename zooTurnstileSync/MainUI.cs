using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using zooTurnstileSync;
using System.Linq;
using System.Threading.Tasks;

namespace ZooTurnstileSync
{
    public partial class MainUI : Form
    {
        public delegate void SafeLog(string logMessage);
        public delegate void SafeNetStatus(string status, Color color);
        public delegate void SafeLblStatus(int device, string status, Color clr);

        string liveURL = "https://zims.punjab.gov.pk/apis/ticket/";
        string localURL = "http://localhost/zims/apis/ticket/";

        int[] devices = { };
        const int noOfDevices = 6;
        
        string[] ip;

        int syncTime = 1 * 60 * 1000;    //1 Minute 10000; //
        int initSyncTime = 5 * 60 * 1000;     // First Sync After 5 Minutes 10000; //
        public static string[] punchedTickets;

        public Label[] Lbl;
        Turnstile[] ts;
        Logs log;
        WEB_API web;

        public MainUI()
        {
            InitializeComponent();
            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
            {
                this.Text = "Zoo Turnstile v" + System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
                tbApi.Text = localURL;
            }
            else
            {
                syncTime = 10000;
                initSyncTime = 10000;
                tbApi.Text = liveURL;

                this.Text = "Zoo Turnstile";
            }
            log = new Logs(this);
            web = new WEB_API(log, this);

            log.LogText("Program Started @ " + DateTime.Now.ToString("MM/dd/yyyy hh:mm tt"));
            Lbl = new Label [noOfDevices] { status1, status2, status3, status4, status5, status6 };
            timerStart.Start();

            ts = new Turnstile[noOfDevices];
        }
        
        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (btnStart.Text == "Disconnect")
            {
                DisconnectAll();
                //timerRTLog.Stop();
                //timerSync.Stop();
                btnStart.Text = "Connect";
                return;
            }
            else if (btnStart.Text != "Connect")
            {
                timerStart.Stop();
                btnStart.Text = "Connect";
                return;
            }

            Cursor = Cursors.WaitCursor;
            foreach (Label l in Lbl)
            {
                l.ForeColor = Color.Black;
                l.Text = "Not Connected";
            }

            //timerSync.Stop();
            //timerRTLog.Stop();

            btnStart.Enabled = false;
            backgroundWorker1.RunWorkerAsync();

        }
        private void DisconnectAll()
        {
            string logMsg = "";
            timerSync.Stop();
            logMsg = "MainUI: Disconnecting all devices." + System.Environment.NewLine;
            foreach (int device in devices)
            {
                ts[device].Disconect();
                //Disconnect(h[device]);
                //h[device] = IntPtr.Zero;
                // KILL All threads
                /*if (Lbl[device].Text == "Connected")
                {
                    ChangeStatus(device, "Disconnected", Color.Red);
                }
                logMsg = logMsg + "Turnstile[" + (device + 1) + "]: Disconnected" + System.Environment.NewLine;*/
            }
            if (logMsg != "")
            {
                log.LogText(logMsg);
            }
        }

        public string getURL()
        {
            return tbApi.Text.ToString();
        }

        private int[] GetConnectableDev()
        {
            // get all ips
            ip = new string[]{
                    inputIp1.Text.ToString(),
                    inputIp2.Text.ToString(),
                    inputIp3.Text.ToString(),
                    inputIp4.Text.ToString(),
                    inputIp5.Text.ToString(),
                    inputIp6.Text.ToString()
                };

            List<int> tempActiveDevices = new List<int>();
            for (int i = 0; i < ip.Length; i++)
            {
                if (ip[i] != "")
                {
                    tempActiveDevices.Add(i);
                }
            }
            return tempActiveDevices.ToArray();
        }

        private bool AnyConnected()
        {
            foreach (Label l in Lbl)
            {
                if (l.Text == "Connected")
                    return true;
            }
            return false;
        }

        public void WriteTextSafe(string logMessage)
        {
            if (tbLogs.InvokeRequired)
            {
                var d = new SafeLog(WriteTextSafe);
                tbLogs.Invoke(d, new object[] { logMessage });
            }
            else
            {
                tbLogs.SelectionStart = 0;
                tbLogs.SelectionLength = 0;
                //tbLogs.SelectionColor = color;
                tbLogs.SelectedText = logMessage;
            }
        }
        public void ChangeStatus(int device,string status, Color clr)
        {
            if (tbLogs.InvokeRequired)
            {
                var d = new SafeLblStatus(ChangeStatus);
                tbLogs.Invoke(d, new object[] { device, status, clr });
            }
            else
            {
                Lbl[device].ForeColor = clr;
                Lbl[device].Text = status;
            }
        }

        public void LblNetStatusChangeSafe(string status, Color color)
        {
            if (lblNetStatus.InvokeRequired)
            {
                var d = new SafeNetStatus(LblNetStatusChangeSafe);
                lblNetStatus.Invoke(d, new object[] { status, color });
            }
            else
            {
                lblNetStatus.ForeColor = color;
                lblNetStatus.Text = status;
            }
        }

        private void TimerStart_Tick(object sender, EventArgs e)
        {
            switch (btnStart.Text)
            {
                case "Connect":
                    btnStart.Text = "in 5...";
                    break;
                case "in 5...":
                    btnStart.Text = "in 4...";
                    break;
                case "in 4...":
                    btnStart.Text = "in 3...";
                    break;
                case "in 3...":
                    btnStart.Text = "in 2...";
                    break;
                case "in 2...":
                    btnStart.Text = "in 1...";
                    break;
                case "in 1...":
                    btnStart.Text = "Connect";
                    timerStart.Stop();
                    btnStart.PerformClick();
                    break;
            }
        }



        private void BackgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            DisconnectAll();
            devices = GetConnectableDev();
            punchedTickets = new string[noOfDevices];
            punchedTickets = Enumerable.Repeat("", noOfDevices).ToArray();
            //h = Enumerable.Repeat(IntPtr.Zero, devices.Length).ToArray();
            Task[] ConThread = new Task[noOfDevices];
            foreach (int device in devices)
            {
                //ts[device] = new Turnstile(ip[device], device, this, log, web);
                //ts[device].Connect()
                ConThread[device] = Task.Factory.StartNew(() =>
                {
                    ts[device] = new Turnstile(ip[device], device, this, log, web);
                    ts[device].ConnectTurnstile();
                });
                //ThreadPool.QueueUserWorkItem(ConnectThread, new object[] { device });
            }
        }
        /*private void ConnectThread(int device)
        {
            ts[device] = new Turnstile(ip[device], device, this, log, web);
            ts[device].ConnectTurnstile();
        }*/

        private void BackgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            Cursor = Cursors.Default;
            btnStart.Text = "Disconnect";
            btnStart.Enabled = true;

            // timers
            timerSync.Interval = initSyncTime;
            timerSync.Start();

            //timerRTLog.Interval = rtLogTime;      // * 1000;
            //timerRTLog.Start();
        }

        private void timerSync_Tick(object sender, EventArgs e)
        {
            timerSync.Stop();
            string[] ticketString = { "", "" };
            List<String> addedTickets = new List<String>();
            if (web.CheckNewEntries(ticketString, addedTickets))
            {
                Task[] SyncThread = new Task[noOfDevices];
                foreach (int device in devices)
                {
                    ts[device].SetNewEntries(ticketString, addedTickets);
                    SyncThread[device] = Task.Factory.StartNew(() => ts[device].SyncDevice());
                    
                    //ThreadPool.QueueUserWorkItem(ts[device].SyncDevice, null);
                }
            }
            timerSync.Interval = syncTime;
            timerSync.Start();
        }
    }
}

/*
private void TimerRTLog_Tick(object sender, EventArgs e)
{
    timerRTLog.Stop();
    foreach (int device in devices)
    {
    }
    timerRTLog.Start();
}
private void TimerSync_Tick(object sender, EventArgs e)
{
    timerSync.Stop();
    Cursor = Cursors.WaitCursor;
    syncCount++;
    Task.Factory.StartNew(() =>         //This will run using a Thread-Pool thread which will not cause the UI to be unresponsive.
    {
        //logtext("\"timerSync_Tick\" Called.");
        if (syncCount >= reconMax)
        {
            syncCount = 0;
            foreach (int device in devices)
            {
                if (IntPtr.Zero == h[device])
                {
                    ThreadPool.QueueUserWorkItem(ConnectThread, new object[] { device });
                }
            }
            foreach (int d in devices)
            {
                if (IntPtr.Zero == h[d])
                {

                    connectDevice(d);
                }
            }
        } else if (AnyConnected())
        {
            //CheckNewEntries();
        }
    }).ContinueWith(t =>                  //This will run on the UI thread
    {
        Cursor = Cursors.Default;
        timerSync.Start();
    }, CancellationToken.None,
    TaskContinuationOptions.OnlyOnRanToCompletion, //Only run this if the first action did not throw an exception
    TaskScheduler.FromCurrentSynchronizationContext()); //Use the UI thread to run this action
}
*/
