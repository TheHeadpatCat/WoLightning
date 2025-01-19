using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WoLightning.Util;
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

        private bool sittingOnChair = false;
        private Vector3 sittingOnChairPos = new();
        readonly private TimerPlus sittingOnChairTimer = new();

        public SitOnFurniture(Plugin plugin) : base(plugin) { }

        override public void Start()
        {
            Plugin.EmoteReaderHooks.OnSitEmote += Check;
            sittingOnChairTimer.Elapsed += checkSittingOnChair;
            IsRunning = true;
        }

        override public void Stop() 
        {
            Plugin.EmoteReaderHooks.OnSitEmote -= Check;
            sittingOnChairTimer.Elapsed -= checkSittingOnChair;
            IsRunning = false;
        }

        public void Check(ushort emoteId)
        {
            
            if (emoteId == 50) // /sit on Chair done
            {
                sittingOnChair = true;
                sittingOnChairPos = Plugin.ClientState.LocalPlayer.Position;
                Trigger("You are sitting on Furniture!");
                int calc = 5000;
                if (ShockOptions.Duration <= 10) calc += ShockOptions.Duration * 1000;
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
            
        }

        private void checkSittingOnChair(object? sender, ElapsedEventArgs? e)
        {
            if (sittingOnChair && Plugin.ClientState.LocalPlayer.Position.Equals(sittingOnChairPos))
            {
                Trigger("You are still sitting on Furniture!");
                sittingOnChairTimer.Refresh();
            }
            else
            {
                sittingOnChair = false;
                sittingOnChairTimer.Stop();
            }
        }
    }
}
