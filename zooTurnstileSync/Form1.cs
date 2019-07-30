using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


using System.Runtime.InteropServices;

namespace zooTurnstileSync
{
    public partial class Form1 : Form
    {
        string ip;
        string port;
        string api;
        string timeToUpdate;

        IntPtr h = IntPtr.Zero;
        public Form1()
        {
            InitializeComponent();
            timerSync.Stop();
            rtLogTimer.Stop();
        }


        

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (btnConnect.Text.ToString() == "Connect")
            {
                connect();
            }
            else
            {
                disconnect();
            }
            
        }

        void logtext(string a,Color color)
        {
            inputStatus.SelectionStart = 0;
            inputStatus.SelectionLength = 0;

            DateTime time = new DateTime();
            time = DateTime.Now;

            a = time.ToString("hh:mm:ss") + "    " + a + "\r\n";

            inputStatus.SelectedText = a;
            inputStatus.SelectionColor = color;
        }


        //4.1  call connect function
        [DllImport("C:\\WINDOWS\\system32\\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);
        [DllImport("plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        void connect()
        {
            ip = inputIP.Text.ToString();
            port = inputPort.Text.ToString();
            api = inputApiUrl.Text.ToString();
            timeToUpdate = inputTime.Text.ToString();

            string connectionStr = "";
            connectionStr = "protocol=TCP,ipaddress="+ip+",port="+port+",timeout=2000,passwd=";

            int ret = 0;        // Error ID number
            Cursor = Cursors.WaitCursor;

            if (IntPtr.Zero == h)
            {
                h = Connect(connectionStr);
                Cursor = Cursors.Default;
                if (h != IntPtr.Zero)
                {
                    logtext("connection is successfull",Color.Black);
                    btnConnect.Text = "Disconnect";
                    lblConnectionStatus.Text = "Connected";
                    timerSync.Interval = Int32.Parse(timeToUpdate)*1000;

                    // timers
                    timerSync.Start();
                    rtLogTimer.Start();


                }
                else
                {
                    ret = PullLastError();
                    logtext("Connect device Failed! The error id is: " + ret,Color.Red);

                }

            }
        }


        [DllImport("plcommpro.dll", EntryPoint = "Disconnect")]
        public static extern void Disconnect(IntPtr h);


        void disconnect()
        {
            Cursor = Cursors.WaitCursor;
            if (IntPtr.Zero != h)
            {
                Disconnect(h);
                Cursor = Cursors.Default;
                h = IntPtr.Zero;

                //timers
                timerSync.Stop();
                rtLogTimer.Stop();

                logtext("disconnected Successfully",Color.Black);
                btnConnect.Text = "Connect";
                lblConnectionStatus.Text = "Disconnected";

            }
            return;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            checkApi();
        }

        void checkApi()
        {
            logtext("API check for new Entries Started",Color.Black);

            string[,] tickets = new string[,]
            {
                {"6", "543268"},
                {"7", "543212"},
                {"8", "543213"},
                {"9", "543214"}
            };

            for (int i = 0; i <= tickets.GetUpperBound(0); i++)
            {
                string ticketNo = tickets[i, 0];
                string cardNo = tickets[i, 1];

                addTicketToController(ticketNo, cardNo);
            }

        }


        //4.6 call SetDeviceData function

        [DllImport("plcommpro.dll", EntryPoint = "SetDeviceData")]
        public static extern int SetDeviceData(IntPtr h, string tablename, string data, string options);

        void addTicketToController(string tn,string cn)
        {
            int ret = 0;
            string data = "Pin="+tn+"\tCardNo="+cn+"\tPassword=1";
            string accessData = "Pin="+tn+"\tAuthorizeDoorId=3\tAuthorizeTimezoneId=1";
            string options = "";
            
            if (IntPtr.Zero != h)
            {   
                // user here is devtablename
                ret = SetDeviceData(h, "user", data, options);
                if (ret >= 0)
                {
                    logtext("Card No "+cn+" add successfully",Color.Black);
                    //adding access
                    
                    int secret = SetDeviceData(h, "userauthorize", accessData, options);
                    if (secret >= 0)
                    {
                        logtext("Card No " + cn + " access granted", Color.Black);
                    }
                    else
                    {
                        logtext("--> Card no " + cn + " access not granted error="+secret, Color.Red);
                    }
                    
                    // call sync added record api
                    return;
                }
                else
                    logtext("--> Card no "+cn+" not added",Color.Red);
            }
            else
            {
                logtext("device not conneted",Color.Red);
                return;
            }
            
        }


        [DllImport("plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        private void rtLogTimer_Tick(object sender, EventArgs e)
        {
            int ret = 0, i = 0, buffersize = 256;
            string str = "";
            string[] tmp = null;
            byte[] buffer = new byte[256];

            if (IntPtr.Zero != h)
            {

                ret = GetRTLog(h, ref buffer[0], buffersize);
                if (ret >= 0)
                {
                    str = Encoding.Default.GetString(buffer);
                    tmp = str.Split(',');

                    string eTime = tmp[0];
                    string ePin = tmp[1];
                    string eCard = tmp[2];
                    string eAuthorized = tmp[4];

                    // eAuthorized 0 is correct door opened

                    if (eAuthorized == "0")
                    {
                        logtext("PIN="+ePin+" Card="+eCard+" verified", Color.Green);
                        removeTicketFromController(eTime, ePin, eCard);

                        // remove entry api call here
                    }

                    
                    //MessageBox.Show(tmp[0]);
                    //this.lsvrtlog.Items.Add(tmp[0]);
                    //this.lsvrtlog.Items[i].SubItems.Add(tmp[1]);
                    //this.lsvrtlog.Items[i].SubItems.Add(tmp[2]);
                    //this.lsvrtlog.Items[i].SubItems.Add(tmp[3]);
                    //this.lsvrtlog.Items[i].SubItems.Add(tmp[4]);
                    //this.lsvrtlog.Items[i].SubItems.Add(tmp[5]);
                    //this.lsvrtlog.Items[i].SubItems.Add(tmp[6]);
                }
                
            }
            else
            {
                logtext("Connect device failed!",Color.Red);
                return;
            }
        }

        [DllImport("plcommpro.dll", EntryPoint = "DeleteDeviceData")]
        public static extern int DeleteDeviceData(IntPtr h, string tablename, string data, string options);

        void removeTicketFromController(string eTime,string ePin,string eCard)
        {
            int ret = 0;
            string data = "Pin="+ePin;
            string options = "";
            
            if (IntPtr.Zero != h)
            {
                ret = DeleteDeviceData(h, "user", data, options);
                if (ret >= 0)
                {
                    logtext("card no " + eCard + " is removed from device",Color.Black);
                    int secret = DeleteDeviceData(h, "userauthorize", data, options);
                    if (secret >= 0)
                    {
                        logtext("card no " + eCard + " access is removed", Color.Black);
                    }
                    else
                    {
                        logtext("card no " + eCard + " access is not removed. error "+secret, Color.Red);
                    }
                }
                    
                else
                    logtext("card no "+eCard+" is not removed from device. error "+ret,Color.Red);
            }
            
        }
    }
}
