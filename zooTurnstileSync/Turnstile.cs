using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using zooTurnstileSync;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace ZooTurnstileSync
{
    class Turnstile
    {

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

        private IntPtr h;
        private int devNo;
        private string devIP;
        private string port = "4370";
        //int syncCount = 0;
        //int reconMax = 30;   //3 Minutes = syncTime * reconMax
        // array of punched tickets to check if these are consumed
        private ticketPunched tp;

        private MainUI ui;
        private Logs log;
        private WEB_API web;
        
        private static readonly object busy = new object();
        private Timer RTLTimer;
        int rtLogTime = 3000;

        private string[] newTicketString = { "", "" };
        List<String> addedTickets = new List<String>();

        public Turnstile(String devIP, int devNo, MainUI UI, Logs logger, WEB_API WEB)
        {
            this.devIP = devIP;
            this.devNo = devNo;

            this.ui = UI;
            this.log = logger;
            this.web = WEB;

            this.tp = new ticketPunched();
        }


        private void SetRTLTimer()
        {
            // Create a timer with a two second interval.
            RTLTimer = new Timer(OnRTLTimerEvent, null, rtLogTime, rtLogTime);
            /* Hook up the Elapsed event for the timer. 
            RTLTimer.Elapsed += OnRTLTimerEvent;
            RTLTimer.AutoReset = true;
            RTLTimer.Enabled = true;*/
        }

        private void OnRTLTimerEvent(object sender)//, ElapsedEventArgs e)
        {
            try
            {
                ui.ChangeStatus(devNo, "RTLOG...", Color.Blue);
                CheckRTLog();
            }
            catch (Exception ex)
            {
                log.LogText("Turnstile[" + (devNo + 1) + "]: Exception inside OnRTLTimerEvent: " + ex.Message.ToString() + System.Environment.NewLine);
                Disconect();
            }
        }

        public void SyncDevice()
        {
            if (IntPtr.Zero == h && ui.Lbl[devNo].Text != "Connecting...")
            {
                ConnectTurnstile();
            }
            else
            {

                ui.ChangeStatus(devNo, "Syncing...", Color.YellowGreen);
                if (AddTicket(newTicketString))
                {
                    web.SyncBackAddedTickets(addedTickets);
                    log.LogText("Turnstile[" + (devNo + 1) + "]: New Tickets" + System.Environment.NewLine + newTicketString[0]);
                    newTicketString[0] = "";
                    newTicketString[1] = "";
                }
                //ui.ChangeStatus(devNo, "Connected", Color.Green);
            }
        }

        public void ConnectTurnstile()
        {
            string connectionStr = "";
            connectionStr = "protocol=TCP,ipaddress=";
            connectionStr += devIP;
            connectionStr += ",port=" + port + ",timeout=2000,passwd=";

            int ret = 0;        // Error ID number
            
            lock (busy)
            {
                try
                {
                    ui.ChangeStatus(devNo, "Connecting...", Color.Blue);
                    h = Connect(connectionStr);
                    if (h != IntPtr.Zero)
                    {
                        if(FlushDevice())
                            h = Connect(connectionStr);
                    }
                }
                catch (Exception ex)
                {
                    log.LogText("Turnstile[" + (devNo + 1) + "]: Exception inside ConnectTurnstile: " + ex.Message.ToString() + System.Environment.NewLine);
                    Disconect();
                }
            }
            if (h != IntPtr.Zero)
            {
                log.LogText("Turnstile[" + (devNo + 1) + "]: Connection Successful!");
                ui.ChangeStatus(devNo, "Connected", Color.Green);
                string[] ticketString = { "", "" };
                List<String> addedTickets = new List<String>();
                if (web.CheckActiveEntries(ticketString, addedTickets))
                {
                    if (AddTicket(ticketString))
                    {
                        String addedTicketsLog = "";
                        foreach (String ticketNo in addedTickets)
                        {
                            addedTicketsLog = addedTicketsLog + "Turnstile[" + (devNo + 1) + "]: Ticket added " + ticketNo + System.Environment.NewLine;
                        }
                        if("" != addedTicketsLog)
                            log.LogText(addedTicketsLog);
                    }
                }
                SetRTLTimer();
            }
            else
            {
                //SetRTLTimer();
                lock (busy)
                {
                    try
                    {
                        ret = PullLastError();
                    }
                    catch(Exception ex)
                    {
                        log.LogText("Turnstile[" + (devNo + 1) + "]: Exception inside ConnectTurnstile: " + ex.Message.ToString() + System.Environment.NewLine);
                        Disconect();
                    }
                }
                log.LogText("Turnstile[" + (devNo + 1) + "]: Connection Failed! The error id is: " + ret);
                Disconect();
            }
        }

        public bool FlushDevice()
        {
            string logMsg = "";
            zkemkeeper.CZKEM axCZKEM1 = new zkemkeeper.CZKEM();
            try
            {
                lock (busy)
                {
                    ui.ChangeStatus(devNo, "Flushing...", Color.Blue);
                    bool bIsConnected = axCZKEM1.Connect_Net(devIP, 4370);   // 4370 is port no of attendance machine
                    if (bIsConnected == true)
                    {
                        //log = "Turnstile[" + (device+1) + "]: Device Connected Successfully\n";
                        axCZKEM1.ClearDataEx(0, "user");
                        axCZKEM1.ClearDataEx(0, "userauthorize");
                        axCZKEM1.Disconnect();
                        logMsg = logMsg + "Turnstile[" + (devNo + 1) + "]: Device Clear Successfully\n";
                        log.LogText(logMsg);
                        return true;
                    }
                    else
                    {
                        logMsg = logMsg + "Turnstile[" + (devNo + 1) + "]: Device Not Cleared/Connected\n";
                        log.LogText(logMsg);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    axCZKEM1.Disconnect();
                }
                catch (Exception exx)
                {
                    log.LogText("Turnstile[" + (devNo + 1) + "]: Exception inside FlushDevice: " + exx.Message.ToString() + System.Environment.NewLine);
                    Disconect();
                }
                logMsg = logMsg + "Turnstile[" + (devNo + 1) + "]: Device Clear/Connect Error: " + ex.Message.ToString();
                log.LogText(logMsg);
                return false;
            }
        }


        public bool AddTicket(string[] tickets)
        {
            int ret = 0;
            string data = tickets[0];
            string accessData = tickets[1];
            string options = "";

            if (IntPtr.Zero != h)
            {
                // user here is devtablename
                lock (busy)
                {
                    try
                    {
                        ret = SetDeviceData(h, "user", data, options);
                    }
                    catch (Exception ex)
                    {
                        log.LogText("Turnstile[" + (devNo + 1) + "]: Exception inside AddTicket: " + ex.Message.ToString() + System.Environment.NewLine);
                        Disconect();
                    }
                }
                if (ret >= 0)
                {
                    //LogText("Card No "+cn+" add successfully",Color.Black);
                    //adding access
                    int secret = -1;
                    lock (busy)
                    {
                        try
                        {
                            secret = SetDeviceData(h, "userauthorize", accessData, options);
                        }
                        catch (Exception ex)
                        {
                            log.LogText("Turnstile[" + (devNo + 1) + "]: Exception inside AddTicket: " + ex.Message.ToString() + System.Environment.NewLine);
                            Disconect();
                        }
                    }
                    if (secret >= 0)
                    {
                        // LogText("Card No " + cn + " access granted", Color.Black);
                        return true;
                    }
                    else
                    {
                        //LogText("--> Card no " + cn + " access not granted error="+secret, Color.Red);
                        return false;
                    }
                }
                else
                {
                    //LogText("--> Card no " + cn + " not added", Color.Red);
                    return false;
                }
            }
            else
            {
                //LogText("device not conneted",Color.Red);
                return false;
            }
        }

        public string RemoveTicket(string ePin)
        {
            int ret = 0;
            string data = "Pin=" + ePin;
            string options = "";
            //this.log.LogText("ASDTurnstile[" + (devNo + 1) + "]: Ticket removed " + ePin + System.Environment.NewLine);
            string logStr = "";
            
            if (IntPtr.Zero != h)
            {
                lock (busy)
                {
                    try
                    {
                        ret = DeleteDeviceData(h, "user", data, options);
                    }
                    catch (Exception ex)
                    {
                        logStr = logStr + "Turnstile[" + (devNo + 1) + "]: Exception inside RemoveTicket: " + ex.Message.ToString() + System.Environment.NewLine;
                        Disconect();
                    }
                }
                if (ret >= 0)
                {
                    logStr = "Turnstile[" + (devNo + 1) + "]: Ticket removed " + ePin + System.Environment.NewLine;
                    int secret = -1;
                    lock (busy)
                    {
                        try
                        {
                            secret = DeleteDeviceData(h, "userauthorize", data, options);
                        }
                        catch (Exception ex)
                        {
                            logStr = logStr + "Turnstile[" + (devNo + 1) + "]: Exception inside RemoveTicket: " + ex.Message.ToString() + System.Environment.NewLine;
                            Disconect();
                        }
                    }
                    if (secret >= 0)
                    {
                        logStr = logStr + "Turnstile[" + (devNo + 1) + "]: Access removed " + ePin + System.Environment.NewLine;
                        //syncDelete(ePin);
                    }
                    else
                    {
                        logStr = logStr + "Turnstile[" + (devNo + 1) + "]: Access not removed " + ePin + ".\t-- > ERROR: " + secret + System.Environment.NewLine;
                    }
                }
                else
                    logStr = logStr + "Turnstile[" + (devNo + 1) + "]: Ticket not removed " + ePin + ".\t-- > ERROR: " + ret + System.Environment.NewLine;
            }
            return logStr;
        }

        public void SetNewEntries(string [] newTicks, List<String> addedTicket)
        {
            newTicketString[0] = newTicketString[0] + "" + newTicks[0];
            newTicketString[1] = newTicketString[1] + "" + newTicks[1];
            addedTickets.AddRange(addedTicket);

            /*
            string[] ticketString = { "", "" };
            List<String> addedTickets = new List<String>();
            if (web.CheckNewEntries(ticketString, addedTickets))
            {
                AddTicket(ticketString);
            }*/
        }

        [HandleProcessCorruptedStateExceptions]
        public void CheckRTLog()
        {
            //LogText("\"timerRTLog_Tick\" Called.");
            RTLTimer.Change(Timeout.Infinite, Timeout.Infinite);
            int ret = 0, buffersize = 10256;
            string str1 = "";
            string[] tmp1 = null;
            string[] tmp2 = null;
            byte[] buffer = new byte[10256];
            
            if (IntPtr.Zero != h)
            {
                if ("" != MainUI.punchedTickets[devNo])
                {
                    log.LogText(RemoveTicket(MainUI.punchedTickets[devNo]));
                    MainUI.punchedTickets[devNo] = "";
                }
                lock (busy)
                {
                    try
                    {
                        ret = GetRTLog(h, ref buffer[0], buffersize);
                    }
                    catch (Exception ex)
                    {
                        log.LogText("Turnstile[" + (devNo + 1) + "]: Exception inside CheckRTLog: " + ex.Message.ToString() + System.Environment.NewLine);
                        Disconect();
                        return;
                    }
                }
                if (ret >= 0)
                {
                    //if(devNo == 1)
                    //    MainUI.punchedTickets = Enumerable.Repeat("61", MainUI.punchedTickets.Length).ToArray();

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

                            //log.LogText("Device "+ devNo +" LOG: " + str);

                            string eTime = tmp2[0];
                            string ePin = tmp2[1];
                            string eCard = tmp2[2];
                            string eAuthorized = tmp2[4];

                            // eAuthorized 200 is DOOR OPENED
                            if (eAuthorized == "200" || eAuthorized == "102")
                            {
                                string logMsg = "Turnstile[" + (devNo + 1) + "]: Ticket consumed " + tp.ePin + System.Environment.NewLine;
                                logMsg = logMsg + RemoveTicket(tp.ePin);
                                /*for (int i = 0 ; i < MainUI.punchedTickets.Length ; i++)
                                {
                                    MainUI.punchedTickets[i] = tp.ePin;
                                }*/


                                MainUI.punchedTickets = Enumerable.Repeat(tp.ePin, MainUI.punchedTickets.Length).ToArray();
                                // remove entry api call here
                                log.LogText(logMsg);
                                web.SyncDelete(tp.ePin);
                            }
                            // eAuthorized 0 is Valid Card
                            else if (eAuthorized == "0" || eAuthorized == "1")
                            {
                                tp.eTime = eTime;
                                tp.ePin = ePin;
                                tp.eCard = eCard;
                                log.LogText("Turnstile[" + (devNo + 1) + "]: Ticket verified " + tp.ePin);
                            }
                        }
                    }
                    RTLTimer.Change(rtLogTime, rtLogTime);

                    ui.ChangeStatus(devNo, "Connected...", Color.Green);
                }
                else
                {
                    // device is disconnected in this state
                    log.LogText("Turnstile[" + (devNo + 1) + "]: ERROR: GetRTLog return false. Disconnecting.");
                    Disconect();
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public void Disconect()
        {
            ui.ChangeStatus(devNo, "Not Connected", Color.Black);
            if (IntPtr.Zero != h)
            {
                string logMsg = "";
                if (null != RTLTimer)
                    RTLTimer.Change(
                        Timeout.Infinite,
                        Timeout.Infinite); //Stop();

                try
                { 
                    Disconnect(h);
                }
                catch (Exception ex)
                {
                    log.LogText("Turnstile[" + (devNo + 1) + "]: Exception inside Disconect: " + ex.Message.ToString() + System.Environment.NewLine);
                }
                h = IntPtr.Zero;
                ui.ChangeStatus(devNo, "Disconnected", Color.Red);
                logMsg = logMsg + "Turnstile[" + (devNo + 1) + "]: Disconnected" + System.Environment.NewLine;

                if (logMsg != "")
                {
                    log.LogText(logMsg);
                }
            }
        }
    }
}


/*
 * 
 * 
        private bool AddTicketToController(int devNo, string tn, string cn)
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
                    //LogText("Card No "+cn+" add successfully",Color.Black);
                    //adding access

                    int secret = SetDeviceData(h[devNo], "userauthorize", accessData, options);
                    if (secret >= 0)
                    {
                        // LogText("Card No " + cn + " access granted", Color.Black);
                        return true;
                    }
                    else
                    {
                        //LogText("--> Card no " + cn + " access not granted error="+secret, Color.Red);
                        return false;
                    }
                }
                else
                {
                    //LogText("--> Card no " + cn + " not added", Color.Red);
                    return false;
                }
            }
            else
            {
                //LogText("device not conneted",Color.Red);
                return false;
            }
        }

        public zkemkeeper.CZKEM axCZKEM1 = new zkemkeeper.CZKEM();
        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                bool bIsConnected = axCZKEM1.Connect_Net("192.168.1.205", 4370);   // 4370 is port no of attendance machine
                if (bIsConnected == true)
                {
                    MessageBox.Show("Device Connected Successfully");
                    axCZKEM1.ClearDataEx(0, "user");
                    MessageBox.Show("Device Clear Successfully");
                }
                else
                {
                    MessageBox.Show("Device Not Connect");
                }
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }
private void deleteAllExisting2(int device)
        {
            //LogText("\"deleteAllExisting\" Called.");
            String[] allRecord = getAllExistingData(h[device]);
            string log = "";
            foreach (string pin in allRecord)
            {
                log = log + removeTicketFromController(device, "", pin, "");
            }
            LogText(log);
        }

        private string[] getAllExistingData(IntPtr h)
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
        
    */