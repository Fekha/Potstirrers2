using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    public class Leaderboard
    {
        public int Wins { get; set; }
        public int WinsToday { get; set; }
        public int WinsThisWeek { get; set; }
        public int LocalWins { get; set; }
        public string Username { get; set; }
    }
}
