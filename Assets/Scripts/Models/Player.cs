using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Models
{
    public class Player
    {
        public int UserId = 40;
        public string Username = "Guest";
        public string Password = "";
        public PlayerTypes playerType = PlayerTypes.HUMAN;
        public int Wins = 0;
        public int LocalWins = 0;
        public int SelectedDie = 0;
        public int SelectedDie2 = 0;
        public int SelectedMeat = 0;
        public int SelectedVeggie = 0;
        public int SelectedFruit = 0;
        public int Stars = 0;
        public int Cooked = 0;
        public int Xp = 0;
        public int Level = 1;
        public bool IsGuest = true;
        public bool WineMenu = false;
        public bool UseD8s = false;
        public bool DisableDoubles = false;
        public bool PlayAsPurple = false;
    }
}
