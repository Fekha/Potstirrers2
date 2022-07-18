using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    public class Profile
    {
        public string Username { get; set; }
        public Nullable<int> DailyWins { get; set; }
        public Nullable<int> WeeklyWins { get; set; }
        public Nullable<int> AllWins { get; set; }
        public Nullable<int> AllPVPWins { get; set; }
        public int Level { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public int Cooked { get; set; }
        public int Calories { get; set; }
        public int Stars { get; set; }
        public bool IsOnline { get; set; }
        public Nullable<System.DateTime> LastLogin { get; set; }
    }
}
