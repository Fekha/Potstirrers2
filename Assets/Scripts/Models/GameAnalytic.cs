using System;
namespace Assets.Models
{
    public class GameAnalytic
    {
        public int GameId { get; set; }
        public DateTime GameStartTime { get; set; }
        public DateTime? GameEndTime { get; set; }
        public string Player1 { get; set; }
        public string Player2 { get; set; }
        public int? Player1CookedNum { get; set; }
        public int? Player2CookedNum { get; set; }
        public bool WineMenu { get; set; }
        public int? TotalTurns { get; set; }
        public bool Quit { get; set; }
        public int Wager { get; set; }
        public bool IsFriendGame { get; set; }
        public bool IsCpuGame { get; set; }
    }
}