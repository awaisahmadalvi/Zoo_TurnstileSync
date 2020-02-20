using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using ZooTurnstileSync;

namespace zooTurnstileSync
{
    class WEB_API
    {

        private MainUI ui;
        private Logs log;

        public WEB_API(Logs logger, MainUI UI)
        {
            this.ui = UI;
            this.log = logger;
        }

        private string GetApiUrl()
        {
            return ui.getURL();
        }
        public bool CheckActiveEntries(string[] ticketString, List<String> addedTickets)
        {
            //log.LogText("\"checkActiveEntries\" Called.");
            String resp = HttpExecution("get_active_record/", "");
            if (resp != "")
            {
                try
                {
                    var jsonObj = JsonConvert.DeserializeObject<newTickets>(resp);

                    if (jsonObj.status == "success")
                    {
                        foreach (ticket t in jsonObj.data)
                        {
                            ticketString = AddTicketToString(ticketString, t.ticket_id, t.qr_code);
                            addedTickets.Add(t.ticket_id);
                            /*
                            //log.LogText(t.ticket_id + " " + t.qr_code + " has been read in recieved data", Color.Green);
                            if (addTicketToController(device, t.ticket_id, t.qr_code))
                            {
                                addedTickets.Add(t.ticket_id);
                                log.LogText("Turnstile[" + device + "]: Ticket added " + t.ticket_id);
                            }
                            */
                        }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    log.LogText("API Exception: Inside CheckActiveEntries, " + ex.Message.ToString() + System.Environment.NewLine);
                }
            }
            return false;
        }

        public bool CheckNewEntries(string[] ticketString, List<String> addedTickets)
        {
            //log.LogText("\"CheckNewEntries\" Called.");
            String resp = HttpExecution("get_sync_record/", "");
            if (resp != "")
            {
                try
                {
                    var jsonObj = JsonConvert.DeserializeObject<newTickets>(resp);

                    if (jsonObj.status == "success")
                    {
                        foreach (ticket t in jsonObj.data)
                        {
                            ticketString = AddTicketToString(ticketString, t.ticket_id, t.qr_code);
                            addedTickets.Add(t.ticket_id);
                            /*
                            //log.LogText(t.ticket_id + " " + t.qr_code + " has been read in recieved data", Color.Green);
                            if (addTicketToController(device, t.ticket_id, t.qr_code))
                            {
                                addedTickets.Add(t.ticket_id);
                                log.LogText("Turnstile[" + (device+1) + "]: Ticket added " + t.ticket_id);
                            }*/
                        }
                        if (addedTickets.Count > 0)
                        {
                            return true;
                        }
                        return false;
                        /*
                        foreach (int device in devices)
                        {
                            if (IntPtr.Zero != h[device])
                            {
                                if (AddTicketStringToController(device, ticketString))
                                {
                                    String logMsg = "";
                                    foreach (String ticketNo in addedTickets)
                                    {
                                        logMsg = logMsg + "Turnstile[" + (device + 1) + "]: Ticket added " + ticketNo + System.Environment.NewLine;
                                    }
                                    if (logMsg != "")
                                    {
                                        log.LogText(logMsg);
                                    }
                                }
                            }
                        }
                        if (addedTickets.Any())
                        {
                            SyncBackAddedTickets(addedTickets);
                        }*/
                    }
                }
                catch (JsonSerializationException ex)
                {
                    log.LogText("API Exception: Inside CheckNewEntries, " + ex.Message.ToString() + System.Environment.NewLine);
                }
            }
            return false;
        }
        
        public void SyncBackAddedTickets(List<string> addedTickets)
        {
            //log.LogText("\"SyncBackAddedTickets\" Called.");
            syncback sb = new syncback();
            sb.ticket_id = addedTickets;
            String resp = null;
            try
            {
                var json = JsonConvert.SerializeObject(sb);
                resp = HttpExecution("update_sync_record/", json);
            }
            catch (Exception ex)
            {
                log.LogText("API Exception: Inside SyncBackAddedTickets, " + ex.Message.ToString() + System.Environment.NewLine);
            }
            if (resp != "")
            {
                try
                {
                    var jsonObj = JsonConvert.DeserializeObject<newTickets>(resp);

                    if (jsonObj.status == "success")
                    {
                        log.LogText("API: Added tickets synced back");
                    }
                }
                catch(Exception ex)
                {
                    log.LogText("API Exception: Inside SyncBackAddedTickets, " + ex.Message.ToString() + System.Environment.NewLine);
                }
            }
        }

        public void SyncDelete(string pin)
        {
            syncbackdelete sb = new syncbackdelete();
            sb.ticket_id = pin;
            String resp = null;
            try
            {
                var json = JsonConvert.SerializeObject(sb);
                resp = HttpExecution("update_qr_status", json);
            }
            catch (Exception ex)
            {
                log.LogText("API Exception: Inside SyncDelete, " + ex.Message.ToString() + System.Environment.NewLine);
            }
            if (resp != "")
            {
                try
                {

                    var jsonObj = JsonConvert.DeserializeObject<delTicketServerMsg>(resp);

                    if (jsonObj.status == "success")
                    {
                        log.LogText("API: Ticket removed from server: " + pin);
                    }
                    else
                    {
                        log.LogText("API ERROR: Ticket not removed from server, no status success: " + pin);
                    }
                }
                catch (Exception ex)
                {
                    log.LogText("API Exception: " + ex.Message.ToString() + System.Environment.NewLine);
                }
            }
            else
            {
                log.LogText("API ERROR: Ticket not removed from server, no response: " + pin);
            }
        }

        private String HttpExecution(string uri, string body)
        {
            string url = GetApiUrl() + uri;
            //log.LogText(url);
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
                log.LogText("API ERROR: " + hre.ToString());
            }
            catch (ArgumentNullException ane)
            {
                log.LogText("API ERROR: " + ane.ToString());
            }
            catch (InvalidOperationException ioe)
            {
                log.LogText("API ERROR: " + ioe.ToString());
            }
            catch (AggregateException ae)
            {
                log.LogText("API ERROR: " + ae.ToString());
            }
            catch (Exception ex)
            {
                log.LogText("API ERROR: " + ex.ToString());
            }

            if (response == null)
            {
                log.LogText("API ERROR: Null response from API.");
                ui.LblNetStatusChangeSafe("Error - " + uri, Color.Red);
                return "";

            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                //log.LogText("API OK");
                ui.LblNetStatusChangeSafe("Online - "+ uri, Color.Green);
            }
            else         //if (response.StatusCode != HttpStatusCode.OK)
            {
                log.LogText("API ERROR: " + response.ReasonPhrase.ToString());
                ui.LblNetStatusChangeSafe("Error - " + uri, Color.Red);
                return "";
            }
            //MessageBox.Show(response.ReasonPhrase.ToString());
            return response.Content.ReadAsStringAsync().Result;
        }

        private string[] AddTicketToString(string[] ticketString, string tn, string cn)
        {
            ticketString[0] = ticketString[0] + "Pin=" + tn + "\tCardNo=" + cn + "\r\n";//"\tPassword=1" + "\r\n";
            //ticketString[1] = ticketString[1] + "Pin=" + tn + "\tAuthorizeDoorId=3\tAuthorizeTimezoneId=1" + "\r\n";
            ticketString[1] = ticketString[1] + "Pin=" + tn + "\tAuthorizeDoorId=15\tAuthorizeTimezoneId=1" + "\r\n";
            return ticketString;
        }

    }
}
