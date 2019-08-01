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


using System.Runtime.InteropServices;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace zooTurnstileSync
{
    public class ticket
    {
        public string ticket_id { get; set; }
        public string qr_code { get; set; }
    }

    public class syncback
    {
        public List<string> ticket_id { get; set; }
    }
    public class syncbackdelete
    {
        public string ticket_id { get; set; }
    }
    public class newTickets
    {
        public string status { get; set; }
        public string message { get; set; }
        public List<ticket> data { get; set; }
            
    
    }
    public class delTicketServerMsg
    {
        public string ticket_id { get; set; }
        public string status { get; set; }
        public string message { get; set; }


    }
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

            inputStatus.SelectionColor = color;
            inputStatus.SelectedText = a;
            
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
                    logtext("connection is successfull",Color.Green);
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

            apiCheckForNewEntries();
            
            // dummy data
            /*string[,] tickets = new string[,]
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
            */

        }


        //4.6 call SetDeviceData function

        [DllImport("plcommpro.dll", EntryPoint = "SetDeviceData")]
        public static extern int SetDeviceData(IntPtr h, string tablename, string data, string options);

        bool addTicketToController(string tn,string cn)
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
                    //logtext("Card No "+cn+" add successfully",Color.Black);
                    //adding access
                    
                    int secret = SetDeviceData(h, "userauthorize", accessData, options);
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
                return false ;
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
                        syncDelete(ePin);
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



        //************************************* API Functions ****************************************
        void apiCheckForNewEntries()
        {

            String resp = httpExecution("http://zims.punjab.gov.pk/api/ticket/get_sync_record/","");
            if (resp != "")
            {


                var jsonObj = JsonConvert.DeserializeObject<newTickets>(resp);

                if (jsonObj.status == "success")
                {
                    List<String> addedTickets = new List<String>();

                    foreach (ticket t in jsonObj.data)
                    {
                        //logtext(t.ticket_id + " " + t.qr_code + " has been read in recieved data", Color.Green);
                        if (addTicketToController(t.ticket_id, t.qr_code))
                        {
                            addedTickets.Add(t.ticket_id);
                            logtext("Ticket added to controller: "+  t.ticket_id,Color.Green);
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

            String resp = httpExecution("http://zims.punjab.gov.pk/api/ticket/update_sync_record/", json);
            if (resp != "")
            {


                var jsonObj = JsonConvert.DeserializeObject<newTickets>(resp);

                if (jsonObj.status == "success")
                {
                    logtext("added tickets synced back", Color.Green);
                }

            }
        }

        private void syncDelete(string pin)
        {
            /*syncbackdelete sb = new syncbackdelete();
            sb.ticket_id = pin;
            var json = JsonConvert.SerializeObject(sb);*/
            
           
            String resp = httpExecution("http://zims.punjab.gov.pk/api/ticket/update_qr_status?ticket_id=" + pin, "");
            
            if (resp != "")
            {

                //logtext(resp,Color.Black);

                var jsonObj = JsonConvert.DeserializeObject<delTicketServerMsg>(resp);

                if (jsonObj.status == "success")
                {
                    logtext("ticket removed from server", Color.Green);
                } 
                else
                {
                    logtext("--> error: ticket removed from server no status success", Color.Red);
                }

            }
            else
            {
                logtext("--> error: ticket removed from server no resp", Color.Red);
            }
        }

        String httpExecution(string url,string body)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);
            client.DefaultRequestHeaders.Add("TOKEN", "12345");
            client.DefaultRequestHeaders.Add("KEY", "012ea63f-7046-45c3-a0f9-cec86e05d104");
            client.DefaultRequestHeaders.Add("TITLE", "ZIMS-Application");
            client.DefaultRequestHeaders
                  .Accept
                  .Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
            
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(body,Encoding.UTF8,"application/json");//CONTENT-TYPE header
            
            HttpResponseMessage response = client.SendAsync(request).Result;

            //MessageBox.Show(response.ReasonPhrase.ToString());

            return response.Content.ReadAsStringAsync().Result;
        }
    }
}
