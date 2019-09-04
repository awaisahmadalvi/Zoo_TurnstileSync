using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.IO;

using System.Runtime.InteropServices;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace zooTurnstileSync
{
    public partial class multiple : Form
    {

        // API string: https://zims.punjab.gov.pk/apis/ticket/

        int[] devices = { };
        string[] ip;
        string port="4370";

        int newCheckTime = 15;
        int rtLogTime = 2;
        Label[] Lbl;
        IntPtr[] h= { IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero };

        public multiple()
        {
            InitializeComponent();
            logtext("Program Started @ " + DateTime.Now.ToString("MM/dd/yyyy hh:mm tt") );
            Lbl = new Label [6] { status1, status2, status3, status4, status5, status6 };
            timerStart.Start();

        }

        public void logtext(string logMessage)
        {
            
            DateTime time = new DateTime();
            time = DateTime.Now;

            logMessage = time.ToString("hh:mm:ss") + "    " + logMessage + "\r\n";

            // Log file named after date
            string logPath = String.Format("{0}_{1:yyyy-MM-dd}.txt", "log", DateTime.Now);;

            tbLogs.SelectionStart = 0;
            tbLogs.SelectionLength = 0;
            //tbLogs.SelectionColor = color;
            tbLogs.SelectedText = logMessage;

            using (var str = new StreamWriter(logPath, append: true))
            {
                str.WriteLine(logMessage);
                str.Flush();
            }
        }

        //4.2 call Disconnect function
        [DllImport("plcommpro.dll", EntryPoint = "Disconnect")]
        public static extern void Disconnect(IntPtr h);

        private void btnStart_Click(object sender, EventArgs e)
        {
            if(btnStart.Text != "Connect")
            {
                timerStart.Stop();
                btnStart.Text = "Connect";
                return;
            }
            foreach(Label l in Lbl)
            {
                l.ForeColor = Color.Black;
                l.Text = "Idle";
            }

            timerSync.Stop();
            timerRTLog.Stop();

            foreach(int d in devices)
            {
                Disconnect(h[d]);
                h[d] = IntPtr.Zero;
                logtext("Device[" + d + "] Disconnected.");
            }
            
            devices = getConnectableDev();

            //h = Enumerable.Repeat(IntPtr.Zero, devices.Length).ToArray();
            foreach (int device in devices)
            {
                connectDevice(device);
            }

            // timers
            timerSync.Interval = newCheckTime * 1000;
            timerSync.Start();

            timerRTLog.Interval = rtLogTime * 1000;
            timerRTLog.Start();
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
            Lbl[device].ForeColor = clr;
            Lbl[device].Text = status;
        }

        [DllImport("C:\\WINDOWS\\system32\\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);
        
        [DllImport("plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        void connectDevice(int device)
        {
            
            string connectionStr = "";
            connectionStr = "protocol=TCP,ipaddress=";
            connectionStr += ip[device];
            connectionStr+=",port=" + port + ",timeout=2000,passwd=";

            int ret = 0;        // Error ID number
            Cursor = Cursors.WaitCursor;

            //if (IntPtr.Zero == h[device])
            //{
                h[device] = Connect(connectionStr);
                //Cursor = Cursors.Default;
                if (h[device] != IntPtr.Zero)
                {
                    logtext("Device["+device+"]: Connection is Successfull" );
                    changeStatus(device, "Connected", Color.Green);

                    deleteAllExisting(device);
                    checkActiveEntries(device);
                    
                }
                else
                {
                    ret = PullLastError();
                    logtext("Device[" + device + "]: Connect device Failed! The error id is: "+ret);

                }
            //}
            Cursor = Cursors.Default;
        }

        //4.7 call GetDeviceData function
        
        void deleteAllExisting(int device)
        {
            String[] allRecord = getAllExistingData(h[device]);
            foreach (string pin in allRecord)
            {
                removeTicketFromController(device, "", pin, "");

            }
        }

        [DllImport("plcommpro.dll", EntryPoint = "GetDeviceData")]
        public static extern int GetDeviceData(IntPtr h, ref byte buffer, int buffersize, string tablename, string filename, string filter, string options);

        string strcount = "";

        private String[] getAllExistingData(IntPtr h)
        {
            int ret = 0;
            int BUFFERSIZE = 1 * 1024 * 1024;
            byte[] buffer = new byte[BUFFERSIZE];
            string options = "";
            if (IntPtr.Zero != h)
            {
                //MessageBox.Show("str="+str);
                //MessageBox.Show("devdatfilter=" + devdatfilter);
                ret = GetDeviceData(h, ref buffer[0], BUFFERSIZE, "user", "Pin", "", options);
            }
            else
            {
                MessageBox.Show("Connect device failed!");
                return new String[0];
            }
            //MessageBox.Show(str);

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
                MessageBox.Show("Get data failed.The error is " + ret);
                return new String[0];
            }

        }

        [DllImport("plcommpro.dll", EntryPoint = "SetDeviceData")]
        public static extern int SetDeviceData(IntPtr h, string tablename, string data, string options);

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

        void checkActiveEntries(int device)
        {
            String resp = httpExecution("get_active_record/", "");
            
            if (resp != "")
            {
                var jsonObj = JsonConvert.DeserializeObject<newTickets>(resp);

                if (jsonObj.status == "success")
                {
                    List<String> addedTickets = new List<String>();
                    
                        if (IntPtr.Zero != h[device])
                        {
                            foreach (ticket t in jsonObj.data)
                            {
                                //logtext(t.ticket_id + " " + t.qr_code + " has been read in recieved data", Color.Green);
                                if (addTicketToController(device, t.ticket_id, t.qr_code))
                                {
                                    addedTickets.Add(t.ticket_id);
                                    logtext("Ticket added to controller [" + device + "] : " + t.ticket_id);
                                }
                            }
                        }
                    
                }
            }
        }
        
        void CheckNewEntries()
        {

            String resp = httpExecution("get_sync_record/", "");
            if (resp != "")
            {
                var jsonObj = JsonConvert.DeserializeObject<newTickets>(resp);

                if (jsonObj.status == "success")
                {
                    List<String> addedTickets = new List<String>();
                    foreach (int devNo in devices)
                    {
                        if (IntPtr.Zero != h[devNo])
                        {
                            foreach (ticket t in jsonObj.data)
                            {
                                //logtext(t.ticket_id + " " + t.qr_code + " has been read in recieved data", Color.Green);
                                if (addTicketToController(devNo, t.ticket_id, t.qr_code))
                                {
                                    addedTickets.Add(t.ticket_id);
                                    logtext("Ticket added to controller [" + devNo + "] : " + t.ticket_id);
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
            syncback sb = new syncback();
            sb.ticket_id = addedTickets;
            var json = JsonConvert.SerializeObject(sb);

            String resp = httpExecution("update_sync_record/", json);
            
            if (resp != "")
            {


                var jsonObj = JsonConvert.DeserializeObject<newTickets>(resp);

                if (jsonObj.status == "success")
                {
                    logtext("added tickets synced back");
                }

            }
        }

        [DllImport("plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        private void timerRTLog_Tick(object sender, EventArgs e)
        {
            int ret = 0, buffersize = 256;
            string str = "";
            string[] tmp = null;
            byte[] buffer = new byte[256];
            foreach (int device in devices)
            {
                if (IntPtr.Zero != h[device])
                {
                    ret = GetRTLog(h[device], ref buffer[0], buffersize);
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
                            logtext("PIN=" + ePin + " Card=" + eCard + " verified on Device[" + device + "]");
                            foreach (int _device in devices)
                            {
                                removeTicketFromController(_device, eTime, ePin, eCard);
                            }
                            syncDelete(ePin);
                            // remove entry api call here
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
        }

        [DllImport("plcommpro.dll", EntryPoint = "DeleteDeviceData")]
        public static extern int DeleteDeviceData(IntPtr h, string tablename, string data, string options);

        void removeTicketFromController(int device, string eTime, string ePin, string eCard)
        {
            int ret = 0;
            string data = "Pin=" + ePin;
            string options = "";

            if (IntPtr.Zero != h[device])
            {
                ret = DeleteDeviceData(h[device], "user", data, options);
                if (ret >= 0)
                {
                    logtext("Ticket is removed from Controller[" + device + "]: " + ePin);
                    int secret = DeleteDeviceData(h[device], "userauthorize", data, options);
                    if (secret >= 0)
                    {
                        logtext("Ticket access is removed from Controller[" + device + "]: " + ePin);
                        //syncDelete(ePin);
                    }
                    else
                    {
                        logtext("Ticket access is not removed from Controller[" + device + "]: " + ePin  + ".\t-- > ERROR: " + secret);
                    }
                }

                else
                    logtext("Ticket is not removed from Controller[" + device + "]: " + ePin + ".\t --> ERROR: " + ret);
            }

        }
        private void syncDelete(string pin)
        {
            syncbackdelete sb = new syncbackdelete();
            sb.ticket_id = pin;
            var json = JsonConvert.SerializeObject(sb);

            //String resp = httpExecution(tbApi.Text + "update_qr_status?ticket_id=" + pin, "");
            String resp = httpExecution("update_qr_status", json);

            if (resp != "")
            {

                //logtext(resp,Color.Black);

                var jsonObj = JsonConvert.DeserializeObject<delTicketServerMsg>(resp);

                if (jsonObj.status == "success")
                {
                    logtext("Ticket removed from server: " + pin);
                }
                else
                {
                    logtext("--> ERROR: Ticket removed from server no status success: " + pin);
                }

            }
            else
            {
                logtext("--> ERROR: Ticket removed from server no resp: " + pin);
            }
        }
        String httpExecution(string url, string body)
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
                logtext("ERROR: " + hre.ToString());
            }
            catch (ArgumentNullException ane)
            {
                logtext("ERROR: " + ane.ToString());
            }
            catch (InvalidOperationException ioe)
            {
                logtext("ERROR: " + ioe.ToString());
            }
            catch (AggregateException ae)
            {
                logtext("ERROR: " + ae.ToString());
            }
            catch (Exception ex)
            {
                logtext("ERROR: " + ex.ToString());
            }


            if (response == null)
            {
                logtext("ERROR: Null response from API.");
                label12.ForeColor = Color.Red;
                label12.Text = "Offline";
                return "";

            }
            if (response.StatusCode == HttpStatusCode.OK)
            {
                label12.ForeColor = Color.Green;
                label12.Text = "Online";
            }
            else         //if (response.StatusCode != HttpStatusCode.OK)
            {
                logtext("API Error: " + response.ReasonPhrase.ToString());
                label12.ForeColor = Color.Red;
                label12.Text = "Offline";
                return "";
            }            
            //MessageBox.Show(response.ReasonPhrase.ToString());
            return response.Content.ReadAsStringAsync().Result;
        }

        private string getApiUrl()
        {
            return tbApi.Text.ToString();
        }

        private void timerSync_Tick(object sender, EventArgs e)
        {
            foreach(int d in devices)
            {
                if(IntPtr.Zero == h[d])
                {
                    connectDevice(d);
                }
            }
            // Check only if any device is connected
            if (anyConnected())
            {
                CheckNewEntries();
            }
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
                    btnStart.PerformClick();
                    timerStart.Stop();
                    break;
            }
        }
    }
}