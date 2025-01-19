using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Util.Types
{
    public class SpecificPlayer
    {
        public List<Player> Players { get; set; }
        public bool AnyPlayer { get; set; } = false;
        public bool IsBlacklist { get; set; } = false;
        public SpecificPlayer(Player Player) { }
        public SpecificPlayer(bool AnyPlayer) { }

        public bool Compare(Player otherPlayer)
        {
            if (AnyPlayer) return true;

            bool found = false;
            foreach (Player p in Players)
            {
                if(p.Equals(otherPlayer)) found = true;
            }
            if(IsBlacklist) return !found;
            return found;

        }
    }
}
