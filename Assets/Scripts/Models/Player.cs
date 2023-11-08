using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Models
{
    public class Player
    {
        public int UserId { get; set; } = 40;
        public string Username { get; set; } = "Guest";
        public string Password { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsCPU { get; set; } = false;
        public int Wins { get; set; } = 0;
        public int LocalWins { get; set; } = 0;
        public List<int> Friends { get; set; } = new List<int>();
        public List<int> SelectedDice { get; set; } = new List<int>();
        public List<int> SelectedIngs { get; set; } = new List<int>();
        public List<string> SelectedTitles { get; set; } = new List<string>();
        public int Calories { get; set; } = 0;
        public int Stars { get; set; } = 0;
        public int Cooked { get; set; } = 0;
        public int Xp { get; set; } = 0;
        public int Level { get; set; } = 1;
        public bool IsGuest { get; set; } = true;
        public bool HasNewMessage { get; set; } = false;
        public bool HasNewChest { get; set; } = false;
        public bool WineMenu { get; set; } = false;
        public bool UseD8s { get; set; } = false;
        public bool DisableDoubles { get; set; } = false;
        public bool PlayAsPurple { get; set; } = false;
        public float MusicVolume { get; set; } = .1f;
        public float TurnVolume { get; set; } = .25f;
        public float VoiceVolume { get; set; } = .5f;
        public float EffectsVolume { get; set; } = .5f;
        public float MasterVolume { get; set; } = .5f;
        public int SeasonScore { get; set; } = 0;
        public bool IsOnline { get; set; } = false;
        public DateTime? LastLogin { get; set; } = null;
        public DateTime? CreatedDate { get; set; } = null;
    }
}
