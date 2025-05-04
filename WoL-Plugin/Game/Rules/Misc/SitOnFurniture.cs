using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using System.Text.Json.Serialization;
using System.Timers;
using WoLightning.Util;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    [Serializable]
    public class SitOnFurniture : RuleBase
    {
        override public string Name { get; } = "Sit on Furniture";
        override public string Description { get; } = "Triggers whenever you try to do /sit on a chair or similiar.";
        override public string Hint { get; }
        override public RuleCategory Category { get; } = RuleCategory.Misc;

        private bool sittingOnChair = false;
        private Vector3 sittingOnChairPos = new();
        readonly private TimerPlus sittingOnChairTimer = new();
        private int safetyStop = 0;

        [JsonConstructor]
        public SitOnFurniture() { }
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
            sittingOnChairTimer.Stop();
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
            safetyStop++;

            Plugin.Log(3, "Chair Check " + Plugin.ClientState.LocalPlayer);
            if (safetyStop > 10)
            {
                Plugin.Log(3, "Timer has exceeded safety limit - aborting Chair Check.");
                sittingOnChair = false;
                sittingOnChairTimer.Stop();
                safetyStop = 0;
                return;
            }

            if (Plugin.ClientState.LocalPlayer == null)
            {
                Plugin.Log(3, "No Player");
                sittingOnChair = false;
                sittingOnChairTimer.Stop();
                safetyStop = 0;
                return;
            }

            if (sittingOnChair && Plugin.ClientState.LocalPlayer.Position.Equals(sittingOnChairPos))
            {
                Trigger("You are still sitting on Furniture!");
                sittingOnChairTimer.Refresh();
                safetyStop++;
            }
            else
            {
                sittingOnChair = false;
                sittingOnChairTimer.Stop();
                safetyStop = 0;
            }
        }
    }
}
