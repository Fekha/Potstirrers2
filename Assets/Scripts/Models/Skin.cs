using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    public class Skin
    {
        public int SkinId = 0;
        public int SkinType = 0;
        public int Rarity = 0;
        public bool IsUnlocked = false;
        public bool IsSelected = false;
        public int UnlockedQty = 0;
        public string SkinName = "";
        public string SkinDesc = "";
    }
}
