using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Models
{
    public class Turn
    {
        public int TurnNumber { get; set; }
        public int GameId { get; set; }
        public Nullable<int> Ingredient1RoutePos { get; set; }
        public Nullable<int> Ingredient2RoutePos { get; set; }
        public Nullable<int> Ingredient3RoutePos { get; set; }
        public Nullable<int> Ingredient4RoutePos { get; set; }
        public Nullable<int> Ingredient5RoutePos { get; set; }
        public Nullable<int> Ingredient6RoutePos { get; set; }
        public Nullable<bool> Ingredient1IsCooked { get; set; }
        public Nullable<bool> Ingredient2IsCooked { get; set; }
        public Nullable<bool> Ingredient3IsCooked { get; set; }
        public Nullable<bool> Ingredient4IsCooked { get; set; }
        public Nullable<bool> Ingredient5IsCooked { get; set; }
        public Nullable<bool> Ingredient6IsCooked { get; set; }
        public Nullable<int> Roll1 { get; set; }
        public Nullable<int> Roll2 { get; set; }
        public Nullable<System.DateTime> TurnStart { get; set; }
        public Nullable<System.DateTime> TurnEnd { get; set; }
        public Nullable<bool> Player1Turn { get; set; }
    }
}
