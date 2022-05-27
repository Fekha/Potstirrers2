using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    public class Message
    {
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string FromName { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
