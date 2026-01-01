using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Timers;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    [Serializable]
    public class SitOnFurniture : RuleBase
    {
        override public string Name { get; } = "Sit on Furniture";
        override public string Description { get; } = "Triggers whenever you try to do /sit on a chair or similiar.";
        override public string Hint { get; }
        override public bool hasExtraButton { get; } = true;
        override public RuleCategory Category { get; } = RuleCategory.Misc;

        public bool KeepTriggering { get; set; } = false;

        [JsonIgnore] bool sittingOnChair = false;
        [JsonIgnore] Vector3 sittingOnChairPos = new();
        [JsonIgnore] readonly TimerPlus sittingOnChairTimer = new();
        [JsonIgnore] int safetyStop = 0;

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
            try
            {
                if (emoteId == 50) // /sit on Chair done
                {
                    sittingOnChair = true;
                    Trigger("You are sitting on Furniture!");
                    if (KeepTriggering)
                    {
                        int calc = 5000;
                        if (ShockOptions.Duration <= 10) calc += ShockOptions.Duration * 1000;
                        sittingOnChairTimer.Interval = calc;
                        sittingOnChairTimer.Start();
                        Task.Delay(30).Wait(); // wait for a miniscule amount to make 100% sure the position is correct.
                        sittingOnChairPos = Plugin.LocalPlayerCharacter.Position;
                    }
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
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }

        private void checkSittingOnChair(object? sender, ElapsedEventArgs? e)
        {
            try
            {
                safetyStop++;
                if (Plugin.LocalPlayerCharacter == null || !Plugin.LocalPlayerCharacter.IsValid())
                {
                    Logger.Log(3, $"{Name} | No Player Character.");
                    sittingOnChair = false;
                    sittingOnChairTimer.Stop();
                    safetyStop = 0;
                    return;
                }

                if (safetyStop > 10)
                {
                    Logger.Log(3, $"{Name} | Timer has exceeded safety limit - aborting Chair Check.");
                    sittingOnChair = false;
                    sittingOnChairTimer.Stop();
                    safetyStop = 0;
                    return;
                }

                if (sittingOnChair && Plugin.LocalPlayerCharacter.Position.Equals(sittingOnChairPos))
                {
                    Trigger("You are still sitting on Furniture!");
                    sittingOnChairTimer.Refresh();
                }
                else
                {
                    Logger.Log(3, $"{Name} | Moved from Position.");
                    sittingOnChair = false;
                    sittingOnChairTimer.Stop();
                    safetyStop = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(Name + " Check() failed.");
                Logger.Error(ex.Message);
                sittingOnChairTimer.Stop();
                safetyStop++;
            }
        }

        public override void DrawExtraButton()
        {

            bool keepTriggering = KeepTriggering;
            if (ImGui.Checkbox("Keep Triggering until standing up? (Max 10)", ref keepTriggering))
            {
                KeepTriggering = keepTriggering;
                Plugin.Configuration.saveCurrentPreset();
            }
        }

    }
}
