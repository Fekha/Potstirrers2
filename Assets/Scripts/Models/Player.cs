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
        public bool IsCPU = false;
        public int Wins = 0;
        public int LocalWins = 0;
        public List<int> SelectedDice = new List<int>();
        public List<int> SelectedIngs = new List<int>();
        public List<string> SelectedTitles = new List<string>();
        public int Calories = 0;
        public int Stars = 0;
        public int Cooked = 0;
        public int Xp = 0;
        public int Level = 1;
        public bool IsGuest = true;
        public bool HasNewMessage = false;
        public bool HasNewChest = false;
        public bool WineMenu = false;
        public bool UseD8s = false;
        public bool DisableDoubles = false;
        public bool PlayAsPurple = false;
        public float MusicVolume = .1f;
        public float TurnVolume = .25f;
        public float VoiceVolume = .5f;
        public float EffectsVolume = .5f;
        public float MasterVolume = .5f;
    }
}
