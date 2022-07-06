using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Models
{
    public class GameState
    {
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }
        public bool IsPlayer1Turn { get; set; }
        public DateTime Player1Ping { get; set; }
        public DateTime Player2Ping { get; set; }
    }

    public class GameTurn
    {
        public int IngId { get; set; }
        public bool Higher { get; set; }
    }
    public class GameRoll
    {
        public int roll1 { get; set; }
        public int roll2 { get; set; }
    }
}
