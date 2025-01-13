using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    [Serializable]
    public class SitOnFurniture : BaseRule
    {
        override public string Name { get; } = "Sit on Furniture";
        override public string Description { get; } = "Triggers whenever you try to do /sit on a chair or similiar.";
        override public string Hint { get; }
        override public RuleCategory Category { get; } = RuleCategory.General;
        
        public SitOnFurniture(Plugin plugin) : base(plugin) { }

        override public void Start()
        {
            Plugin.EmoteReaderHooks.OnSitEmote += Check;
        }

        override public void Stop() 
        {
            Plugin.EmoteReaderHooks.OnSitEmote -= Check;
        }

        public void Check(ushort emoteId)
        {
            /*
            if (emoteId == 50) // /sit on Chair done
            {
                sittingOnChair = true;
                sittingOnChairPos = Plugin.ClientState.LocalPlayer.Position;
                Plugin.ClientPishock.request(ActivePreset.SitOnFurniture, Plugin.LocalPlayer);
                int calc = 5000;
                if (ActivePreset.SitOnFurniture.Duration <= 10) calc += ActivePreset.SitOnFurniture.Duration * 1000;
                sittingOnChairTimer.Interval = calc;
                sittingOnChairTimer.Start();
            }

            if (emoteId == 52) // /sit with no furniture or /groundsit on furniture - check nearby chairs
            {
                // Todo - Implement
            }

            if (emoteId == 51)
            {
                sittingOnChair = false;
                sittingOnChairTimer.Stop();
            }
            */
        }
    }
}
