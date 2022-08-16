using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Models
{
    public class Friend
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public bool RealFriend { get; set; }
        public int Level { get; set; }
    }
}
