using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Http;
using System.IO;

using System.Runtime.InteropServices;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace zooTurnstileSync
{
    public partial class multiple : Form
    {
        private delegate void SafeCallLog(string logMessage);
        private delegate void SafeNetStatus(int status);
        private delegate void SafeLblStatus(int device, string status, Color clr);
        // API string: https://zims.punjab.gov.pk/apis/ticket/
        // API string: http://localhost/zims/apis/ticket/

        //[DllImport("C:\\WINDOWS\\system32\\plcommpro.dll", EntryPoint = "Connect")]
        [DllImport("plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);

        [DllImport("plcommpro.dll", EntryPoint = "Disconnect")]
        public static extern void Disconnect(IntPtr h);

        [DllImport("plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        [DllImport("plcommpro.dll", EntryPoint = "GetDeviceData")]
        public static extern int GetDeviceData(IntPtr h, ref byte buffer, int buffersize, string tablename, string filename, string filter, string options);

        [DllImport("plcommpro.dll", EntryPoint = "SetDeviceData")]
        public static extern int SetDeviceData(IntPtr h, string tablename, string data, string options);
        
        [DllImport("plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        [DllImport("plcommpro.dll", EntryPoint = "DeleteDeviceData")]
        public static extern int DeleteDeviceData(IntPtr h, string tablename, string data, string options);
        
        int[] devices = { };
        string[] ip;
        string port="4370";

        int newCheckTime = 15;
        int rtLogTime = 500;
        int reconCount = 0;
        Label[] Lbl;
        IntPtr[] h= { IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero };

        // array of punched tickets to check if these are consumed
        ticketPunched[] tp;

        public multiple()
        {
            InitializeComponent();
            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
            {
                this.Text = "Zoo Turnstile v" + System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            else
            {
                this.Text = "Zoo Turnstile";
            }
            logtext("Program Started @ " + DateTime.Now.ToString("MM/dd/yyyy hh:mm tt") );
            Lbl = new Label [6] { status1, status2, status3, status4, status5, status6 };
            timerStart.Start();

            tp = new ticketPunched[h.Length];
            for (int i = 0; i < h.Length; i++)
            {
                tp[i] = new ticketPunched();
            }
        }

        public void logtext(string logMessage)
        {
            
            DateTime time = new DateTime();
            time = DateTime.Now;

            logMessage = time.ToString("hh:mm:ss tt") + "    " + logMessage + "\r\n";

            // Log file named after date
            string logPath = String.Format("{0}_{1:yyyy-MM-dd}.txt", "log", DateTime.Now);

            WriteTextSafe(logMessage);

            using (var str = new StreamWriter(logPath, append: true))
            {
                str.WriteLine(logMessage);
                str.Flush();
            }
        }

        private void WriteTextSafe(string logMessage)
        {
            if (tbLogs.InvokeRequired)
            {
                var d = new SafeCallLog(WriteTextSafe);
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

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (btnStart.Text == "Disconnect")
            {
                disconnectAll();
                timerRTLog.Stop();
                timerSync.Stop();
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
                l.Text = "Idle";
            }

            timerSync.Stop();
            timerRTLog.Stop();

            btnStart.Enabled = false;
            backgrounsdWorker1.RunWorkerAsync();

        }
        private void disconnectAll()
        {
            string log = "";
            foreach (int device in devices)
            {
                Disconnect(h[device]);
                h[device] = IntPtr.Zero;
                if (Lbl[device].Text == "Connected")
                {
                    changeStatus(device, "Disconnected", Color.Red);
                }
                log = log + "Turnstile[" + (device + 1) + "]: Disconnected" + System.Environment.NewLine;
            }
            if (log != "")
            {
                logtext(log);
            }
        }

        private int[] getConnectableDev()
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

        bool anyConnected()
        {
            foreach (Label l in Lbl)
            {
                if (l.Text == "Connected")
                    return true;
            }
            return false;
        }

        void changeStatus(int device,string status, Color clr)
        {
            if (tbLogs.InvokeRequired)
            {
                var d = new SafeLblStatus(changeStatus);
                tbLogs.Invoke(d, new object[] { device, status, clr });
            }
            else
            {
                Lbl[device].ForeColor = clr;
                Lbl[device].Text = status;
            }
        }

        void connectDevice(int device)
        {   
            string connectionStr = "";
            connectionStr = "protocol=TCP,ipaddress=";
            connectionStr += ip[device];
            connectionStr+=",port=" + port + ",timeout=2000,passwd=";

            int ret = 0;        // Error ID number

            //if (IntPtr.Zero == h[device])
            //{
            
            h[device] = Connect(connectionStr);
            if (h[device] != IntPtr.Zero)
            {
                logtext("Turnstile[" + (device+1) + "]: Connection Successful!");
                changeStatus(device, "Connected", Color.Green);
                deleteAllExisting(device);
                checkActiveEntries(device);
            }
            else
            {
                ret = PullLastError();
                logtext("Turnstile[" + (device+1) + "]: Connection Failed! The error id is: " + ret);
            }
        }
        
        void deleteAllExisting(int device)
        {
            //logtext("\"deleteAllExisting\" Called.");
            String[] allRecord = getAllExistingData(h[device]);
            string log = "";
            foreach (string pin in allRecord)
            {
                log = log + removeTicketFromController(device, "", pin, "");
            }
            logtext(log);
        }

        private String[] getAllExistingData(IntPtr h)
        {
            int ret = 0;
            int BUFFERSIZE = 1 * 1024 * 1024;
            byte[] buffer = new byte[BUFFERSIZE];
            string options = "";
            string strcount = "";

            if (IntPtr.Zero != h)
            {
                //MessageBox.Show("str="+str);
                //MessageBox.Show("devdatfilter=" + devdatfilter);
                ret = GetDeviceData(h, ref buffer[0], BUFFERSIZE, "user", "Pin", "", options);
            }
            else
            {
                return new String[0];
            }

            if (ret >= 0)
            {
                string retData = Encoding.Default.GetString(buffer);
                strcount = Encoding.Default.GetString(buffer);
                //MessageBox.Show("Get " + retData + " records");
                retData = retData.Replace("Pin\r\n", "");
                retData = retData.Replace("\r\n", ",");
                string[] extPins = retData.Split(',');
                return extPins.Take(extPins.Count() - 1).ToArray();
            }
            else
            {
                MessageBox.Show("Get data failed. The error is " + ret);
                return new String[0];
            }
        }

        private string[] addTicketToString(string[] ticketString, string tn, string cn)
        {
            ticketString[0] = ticketString[0] + "Pin=" + tn + "\tCardNo=" + cn + "\r\n";//"\tPassword=1" + "\r\n";
            ticketString[1] = ticketString[1] + "Pin=" + tn + "\tAuthorizeDoorId=3\tAuthorizeTimezoneId=1" + "\r\n";
            return ticketString;
        }

        bool addTicketToController(int devNo, string tn, string cn)
        {
            int ret = 0;
            string data = "Pin=" + tn + "\tCardNo=" + cn + "\tPassword=1";
            string accessData = "Pin=" + tn + "\tAuthorizeDoorId=3\tAuthorizeTimezoneId=1";
            string options = "";

            if (IntPtr.Zero != h[devNo])
            {
                // user here is devtablename
                ret = SetDeviceData(h[devNo], "user", data, options);
                if (ret >= 0)
                {
                    //logtext("Card No "+cn+" add successfully",Color.Black);
                    //adding access

                    int secret = SetDeviceData(h[devNo], "userauthorize", accessData, options);
                    if (secret >= 0)
                    {
                        // logtext("Card No " + cn + " access granted", Color.Black);
                        return true;
                    }
                    else
                    {
                        //logtext("--> Card no " + cn + " access not granted error="+secret, Color.Red);
                        return false;
                    }
                }
                else
                {
                    //logtext("--> Card no " + cn + " not added", Color.Red);
                    return false;
                }
            }
            else
            {
                //logtext("device not conneted",Color.Red);
                return false;
            }
        }

        bool addTicketStringToController(int devNo, string[] tickets)
        {
            int ret = 0;
            string data = tickets[0];
            string accessData = tickets[1];
            string options = "";

            if (IntPtr.Zero != h[devNo])
            {
                // user here is devtablename
                ret = SetDeviceData(h[devNo], "user", data, options);
                if (ret >= 0)
                {
                    //logtext("Card No "+cn+" add successfully",Color.Black);
                    //adding access

                    int secret = SetDeviceData(h[devNo], "userauthorize", accessData, options);
                    if (secret >= 0)
                    {
                        // logtext("Card No " + cn + " access granted", Color.Black);
                        return true;
                    }
                    else
                    {
                        //logtext("--> Card no " + cn + " access not granted error="+secret, Color.Red);
                        return false;
                    }
                }
                else
                {
                    //logtext("--> Card no " + cn + " not added", Color.Red);
                    return false;
                }
            }
            else
            {
                //logtext("device not conneted",Color.Red);
                return false;
            }
        }

        void checkActiveEntries(int device)
        {
            //logtext("\"checkActiveEntries\" Called.");
            String resp = httpExecution("get_active_record/", "");
            if (resp != "")
            {
                var jsonObj = JsonConvert.DeserializeObject<newTickets>(resp);

                if (jsonObj.status == "success")
                {
                    List<String> addedTickets = new List<String>();
                    if (IntPtr.Zero != h[device])
                    {
                        string[] ticketString = {"", ""};
                        foreach (ticket t in jsonObj.data)
                        {
                            ticketString = addTicketToString(ticketString, t.ticket_id, t.qr_code);
                            addedTickets.Add(t.ticket_id);
                            /*
                            //logtext(t.ticket_id + " " + t.qr_code + " has been read in recieved data", Color.Green);
                            if (addTicketToController(device, t.ticket_id, t.qr_code))
                            {
                                addedTickets.Add(t.ticket_id);
                                logtext("Turnstile[" + device + "]: Ticket added " + t.ticket_id);
                            }
                            */
                        }
                        if(addTicketStringToController(device, ticketString))
                        {
                            String addedTicketsLog = "";
                            foreach (String ticketNo in addedTickets)
                            {
                                addedTicketsLog = addedTicketsLog + "Turnstile[" + (device+1) + "]: Ticket added " + ticketNo + System.Environment.NewLine;
                            }
                            logtext(addedTicketsLog);
                        }
                        
                    }
                }
            }
        }

        void CheckNewEntries()
        {
            //logtext("\"CheckNewEntries\" Called.");
            String resp = httpExecution("get_sync_record/", "");
            if (resp != "")
            {
                var jsonObj = JsonConvert.DeserializeObject<newTickets>(resp);

                if (jsonObj.status == "success")
                {
                    List<String> addedTickets = new List<String>();
                    string[] ticketString = { "", "" };
                    foreach (ticket t in jsonObj.data)
                    {
                        ticketString = addTicketToString(ticketString, t.ticket_id, t.qr_code);
                        addedTickets.Add(t.ticket_id);
                        /*
                        //logtext(t.ticket_id + " " + t.qr_code + " has been read in recieved data", Color.Green);
                        if (addTicketToController(device, t.ticket_id, t.qr_code))
                        {
                            addedTickets.Add(t.ticket_id);
                            logtext("Turnstile[" + (device+1) + "]: Ticket added " + t.ticket_id);
                        }*/
                    }
                    foreach (int device in devices)
                    {
                        if (IntPtr.Zero != h[device])
                        {
                            if (addTicketStringToController(device, ticketString))
                            {
                                String log = "";
                                foreach (String ticketNo in addedTickets)
                                {
                                    log = log + "Turnstile[" + (device + 1) + "]: Ticket added " + ticketNo + System.Environment.NewLine;
                                }
                                if (log != "")
                                {
                                    logtext(log);
                                }
                            }
                        }
                    }
                    if (addedTickets.Any())
                    {
                        SyncBackAddedTickets(addedTickets);
                    }
                }
            }
        }

        private void SyncBackAddedTickets(List<string> addedTickets)
        {
            //logtext("\"SyncBackAddedTickets\" Called.");
            syncback sb = new syncback();
            sb.ticket_id = addedTickets;
            var json = JsonConvert.SerializeObject(sb);
            String resp = null;
            resp = httpExecution("update_sync_record/", json);
            if (resp != "")
            {
                var jsonObj = JsonConvert.DeserializeObject<newTickets>(resp);

                if (jsonObj.status == "success")
                {
                    logtext("API: Added tickets synced back");
                }
            }
        }

        string removeTicketFromController(int device, string eTime, string ePin, string eCard)
        {
            int ret = 0;
            string data = "Pin=" + ePin;
            string options = "";
            string log = "";

            if (IntPtr.Zero != h[device])
            {
                ret = DeleteDeviceData(h[device], "user", data, options);
                if (ret >= 0)
                {
                    log = "Turnstile[" + (device + 1) + "]: Ticket removed " + ePin + System.Environment.NewLine;
                    int secret = DeleteDeviceData(h[device], "userauthorize", data, options);
                    if (secret >= 0)
                    {
                        log = log + "Turnstile[" + (device + 1) + "]: Access removed " + ePin + System.Environment.NewLine;
                        //syncDelete(ePin);
                    }
                    else
                    {
                        log = log + "Turnstile[" + (device + 1) + "]: Access not removed " + ePin + ".\t-- > ERROR: " + secret + System.Environment.NewLine;
                    }
                }

                else
                    log = log + "Turnstile[" + (device + 1) + "]: Ticket not removed " + ePin + ".\t-- > ERROR: " + ret + System.Environment.NewLine;
            }
            return log;
        }

        private void syncDelete(string pin)
        {
            syncbackdelete sb = new syncbackdelete();
            sb.ticket_id = pin;
            var json = JsonConvert.SerializeObject(sb);
            String resp = null;
            //String resp = httpExecution(tbApi.Text + "update_qr_status?ticket_id=" + pin, "");
            resp = httpExecution("update_qr_status", json);
            if (resp != "")
            {

                //logtext(resp,Color.Black);

                var jsonObj = JsonConvert.DeserializeObject<delTicketServerMsg>(resp);

                if (jsonObj.status == "success")
                {
                    logtext("API: Ticket removed from server: " + pin);
                }
                else
                {
                    logtext("API ERROR: Ticket not removed from server, no status success: " + pin);
                }
            }
            else
            {
                logtext("API ERROR: Ticket not removed from server, no response: " + pin);
            }
        }

        private String httpExecution(string url, string body)
        {
            
            url = getApiUrl() + url;
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);
            client.DefaultRequestHeaders.Add("TOKEN", "12345");
            client.DefaultRequestHeaders.Add("KEY", "012ea63f-7046-45c3-a0f9-cec86e05d104");
            client.DefaultRequestHeaders.Add("TITLE", "ZIMS-Application");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT x.y; rv:10.0) Gecko/20100101 Firefox/10.0");
            client.DefaultRequestHeaders
                  .Accept
                  .Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");//CONTENT-TYPE header
            
            HttpResponseMessage response = null;
            try
            {
                response = client.SendAsync(request).Result;
            }
            catch (HttpRequestException hre)
            {
                logtext("API ERROR: " + hre.ToString());
            }
            catch (ArgumentNullException ane)
            {
                logtext("API ERROR: " + ane.ToString());
            }
            catch (InvalidOperationException ioe)
            {
                logtext("API ERROR: " + ioe.ToString());
            }
            catch (AggregateException ae)
            {
                logtext("API ERROR: " + ae.ToString());
            }
            catch (Exception ex)
            {
                logtext("API ERROR: " + ex.ToString());
            }
                
            if (response == null)
            {
                logtext("API ERROR: Null response from API.");
                lblNetStatusChangeSafe(0);
                return "";

            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                //logtext("API OK");
                lblNetStatusChangeSafe(1);
            }
            else         //if (response.StatusCode != HttpStatusCode.OK)
            {
                logtext("API ERROR: " + response.ReasonPhrase.ToString());
                lblNetStatusChangeSafe(0);
                return "";
            }            
            //MessageBox.Show(response.ReasonPhrase.ToString());
            return response.Content.ReadAsStringAsync().Result;
        }

        private void lblNetStatusChangeSafe(int status)
        {
            if (lblNetStatus.InvokeRequired)
            {
                var d = new SafeNetStatus(lblNetStatusChangeSafe);
                lblNetStatus.Invoke(d, new object[] { status });
            }
            else
            {
                switch (status)
                {
                    case 0:
                        lblNetStatus.ForeColor = Color.Red;
                        lblNetStatus.Text = "Offline";
                        break;
                    case 1:
                        lblNetStatus.ForeColor = Color.Green;
                        lblNetStatus.Text = "Online";
                        break;
                }
            }
        }

        private string getApiUrl()
        {
            return tbApi.Text.ToString();
        }

        private void timerRTLog_Tick(object sender, EventArgs e)
        {
            //logtext("\"timerRTLog_Tick\" Called.");
            timerRTLog.Stop();
            int ret = 0, buffersize = 10256;
            string str1 = "";
            string[] tmp1 = null;
            string[] tmp2 = null;
            byte[] buffer = new byte[10256];
            foreach (int device in devices)
            {
                if (IntPtr.Zero != h[device])
                {
                    ret = GetRTLog(h[device], ref buffer[0], buffersize);
                    if (ret >= 0)
                    {
                        str1 = Encoding.Default.GetString(buffer);
                        str1 = str1.Replace("\0", "");
                        str1 = str1.Replace("\r\n", ";");
                        tmp1 = str1.Split(';');
                        foreach (string str in tmp1)
                        {
                            if (str != "")
                            {
                                //str1 = Encoding.Default.GetString(buffer);
                                //str = Encoding.Default.GetString(buffer);
                                tmp2 = str.Split(',');

                                //logtext("Device "+ device +" LOG: " + str);

                                string eTime = tmp2[0];
                                string ePin = tmp2[1];
                                string eCard = tmp2[2];
                                string eAuthorized = tmp2[4];

                                // eAuthorized 200 is DOOR OPENED
                                if (eAuthorized == "200" || eAuthorized == "102")
                                {
                                    string log = "Turnstile[" + (device + 1) + "]: Ticket consumed " + tp[device].ePin + System.Environment.NewLine;
                                    foreach (int _device in devices)
                                    {
                                        log = log + removeTicketFromController(_device, tp[device].eTime, tp[device].ePin, tp[device].eCard);
                                    }
                                    // remove entry api call here
                                    logtext(log);
                                    syncDelete(tp[device].ePin);
                                }
                                // eAuthorized 0 is Valid Card
                                else if (eAuthorized == "0" || eAuthorized == "1")
                                {
                                    tp[device].eTime = eTime;
                                    tp[device].ePin = ePin;
                                    tp[device].eCard = eCard;
                                    logtext("Turnstile[" + (device + 1) + "]: Ticket verified " + tp[device].ePin);
                                }
                            }
                        }
                    }
                    else
                    {
                        // device is disconnected in this state
                        changeStatus(device, "Disconnected", Color.Red);
                        h[device] = IntPtr.Zero;
                        //connectDevice(device);
                    }
                }
                /*
                else
                {
                    logtext("Connect failed! Device[" + device + "]");
                }*/
            }
            timerRTLog.Start();
        }

        private void timerSync_Tick(object sender, EventArgs e)
        {
            timerSync.Stop();
            Cursor = Cursors.WaitCursor;
            reconCount++;
            Task.Factory.StartNew(() =>         //This will run using a Thread-Pool thread which will not cause the UI to be unresponsive.
            {
                //logtext("\"timerSync_Tick\" Called.");
                if (reconCount >= 4)
                {
                    reconCount = 0;
                    foreach (int d in devices)
                    {
                        if (IntPtr.Zero == h[d])
                        {
                            connectDevice(d);
                        }
                    }
                }
                // Check only if any device is connected
                if (anyConnected())
                {
                    CheckNewEntries();
                }
            }).ContinueWith(t =>                  //This will run on the UI thread
            {
                Cursor = Cursors.Default;
                timerSync.Start();
            }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnRanToCompletion, //Only run this if the first action did not throw an exception
            TaskScheduler.FromCurrentSynchronizationContext()); //Use the UI thread to run this action
        }

        private void timerStart_Tick(object sender, EventArgs e)
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

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            disconnectAll();
            devices = getConnectableDev();
            //h = Enumerable.Repeat(IntPtr.Zero, devices.Length).ToArray();
            foreach (int device in devices)
            {
                connectDevice(device);
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            Cursor = Cursors.Default;
            btnStart.Text = "Disconnect";
            btnStart.Enabled = true;
            // timers
            timerSync.Interval = newCheckTime * 1000;
            timerSync.Start();

            timerRTLog.Interval = rtLogTime;// * 1000;
            timerRTLog.Start();
        }
    }
}