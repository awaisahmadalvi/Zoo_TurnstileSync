using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZooTurnstileSync
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
    public class ticketPunched
    {
        public string eTime { get; set; }
        public string ePin { get; set; }
        public string eCard { get; set; }
    }
    class Tickets_Struct
    {
    }
}
